using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using StackExchange.Redis;
using Store.API.Errors;
using Store.API.Helpers;
using Store.API.Middlewares;
using Store.Application;
using Store.Application.Contracts;
using Store.Domain.Entities.Identity;
using Store.Infrastructure;
using Store.Infrastructure.Data;
using Store.Infrastructure.Identity;
using Store.Infrastructure.Repositories;
using static Store.Application.Contracts.IOrderServices;
namespace Store.API
{
    public class ApiMarker() { }
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
             .MinimumLevel.Error() 
             .WriteTo.File("logs/error-log-.txt",
               rollingInterval: RollingInterval.Day,
               outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
            try
            {
                Log.Information("Starting web application");
                var builder = WebApplication.CreateBuilder(args);
                builder.Host.UseSerilog();
                builder.Services.AddControllers();
                // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
                builder.Services.Configure<ApiBehaviorOptions>(options =>
                {
                    options.InvalidModelStateResponseFactory = actionContext =>
                    {
                        var errors = actionContext.ModelState
                        .Where(e => e.Value!.Errors.Count > 0)
                        .SelectMany(x => x.Value!.Errors)
                        .Select(x => x.ErrorMessage).ToArray();
                        var errorResponse = new ApiValidationErrorResponse
                        {
                            Errors = errors
                        };
                        return new BadRequestObjectResult(errorResponse);
                    };

                });


                var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

                builder.Services.AddSingleton<IConnectionMultiplexer>(c =>
                {
                    var configuration = ConfigurationOptions.Parse(redisConnectionString!, true);
                    return ConnectionMultiplexer.Connect(configuration);
                }); builder.Services.AddScoped<IBasketRepository, BasketRepository>();

                builder.Services.AddEndpointsApiExplorer();


                builder.Services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Store.API", Version = "v1" });

                    var securityScheme = new OpenApiSecurityScheme
                    {
                        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer",
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    };

                    c.AddSecurityDefinition("Bearer", securityScheme);

                    var securityRequirement = new OpenApiSecurityRequirement
                     {
                     {
                        securityScheme, new[] { "Bearer" }
                     }
                     };

                    c.AddSecurityRequirement(securityRequirement);
                });


                builder.Services.AddDbContext<StoreDbContext>(options =>
                {
                    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
                });

                builder.Services.AddDbContext<DbIdentityContext>(options =>
                  options.UseSqlServer(builder.Configuration.GetConnectionString("IdentityConnection"),
                   b => b.MigrationsAssembly("Store.Infrastructure")));


                builder.Services.AddAutoMapper(cfg =>

                    cfg.AddMaps(typeof(Store.Application.Mapping.ProductProfile).Assembly)
                );


                builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
                {
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireDigit = true;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequiredLength = 8;
                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                }).AddEntityFrameworkStores<DbIdentityContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders();


                builder.Services.AddScoped<ITokenService, TokenServices>();


                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                }).AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["JwT:ValidIssuer"],
                        ValidAudience = builder.Configuration["JwT:ValidAudience"],
                        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["JwT:Key"]!))
                    };
                });


                builder.Services.AddScoped<IOrderServices, OrderServices>();
                builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
                builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
                builder.Services.AddTransient<ProductUrlResolver>();
                builder.Services.AddScoped<IPaymentService, PaymentService>();
                builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("StripeSettings"));
                builder.Services.AddScoped<IEmailservices, EmailServices>();

                builder.Services.AddRateLimiter(options =>
                {


                    options.AddFixedWindowLimiter("loginPolicy", policy =>
                    {
                        policy.PermitLimit = 5;
                        policy.Window = TimeSpan.FromMinutes(1);
                        policy.QueueLimit = 0;
                    });
                });

                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("Allow-Frontend", policy =>
                    {
                        policy.WithOrigins("https://frontend-test", "https://localhost:7209")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                    });


                });


                var app = builder.Build();

                app.UseDeveloperExceptionPage();
                app.UseMiddleware<ExceptionMiddlewares>();

                app.Use(async (context, next) =>
                {
                    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                    context.Response.Headers.Append("X-Frame-Options", "DENY");
                    context.Response.Headers.Append("X_XSS-Protection", "1; mode=block");
                    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
                    await next();
                });

                    app.UseSwagger();
                    app.UseSwaggerUI();


                app.UseStaticFiles();

                app.UseCors("Allow-Frontend");

                app.UseHttpsRedirection();

                app.UseRateLimiter();

                app.UseRouting();

                app.UseAuthentication();
                app.UseAuthorization();

                app.MapControllers();


                    using (var scope = app.Services.CreateScope())
                    {
                        var service = scope.ServiceProvider;
                        try
                        {
                            var rolemanager = service.GetRequiredService<RoleManager<IdentityRole>>();
                            var usermanager = service.GetRequiredService<UserManager<AppUser>>();
                            string[] roles = { "Admin", "Client" };
                            foreach (var role in roles)
                            {
                                if (!await rolemanager.RoleExistsAsync(role))
                                    await rolemanager.CreateAsync(new IdentityRole(role));
                                var adminEmail = "admin@store.com";
                                var adminUser = await usermanager.FindByEmailAsync(adminEmail);
                                if (adminUser == null)
                                {
                                    var newAdmin = new AppUser
                                    {
                                        Email = adminEmail,
                                        DisplayName = "Admin",
                                        EmailConfirmed = true,
                                        UserName = adminEmail
                                    };
                                    var result = await usermanager.CreateAsync(newAdmin, "Admin#123");
                                    if (result.Succeeded)
                                        await usermanager.AddToRoleAsync(newAdmin, "Admin");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            var log = service.GetRequiredService<ILogger<Program>>();
                            log.LogError(ex, "Error while Getting your Role");
                        }
                    }



                    using (var scope = app.Services.CreateScope())
                    {
                        var services = scope.ServiceProvider;
                        try
                        {
                            var context = services.GetRequiredService<StoreDbContext>();
                        try { await context.Database.MigrateAsync(); }
                        catch (Exception ex) { Console.WriteLine("MIGRATE ERROR: " + ex.Message); }
                        context.Database.SetCommandTimeout(60);
                        await StoreContextSeed.SeedAsync(context);   // يتنفذ حتى لو الـ migrate فشل
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("=== SEED ERROR ===");
                            Console.WriteLine(ex.ToString());
                        }
                    }
                
                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
