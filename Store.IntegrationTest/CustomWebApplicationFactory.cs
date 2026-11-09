using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using StackExchange.Redis;
using Store.API;
using Store.Application.Contracts;
using Store.Domain.Entities;
using Store.Domain.Entities.Identity;
using Store.Infrastructure.Data;
using Store.Infrastructure.Identity;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



    namespace Store.IntegrationTest
    {
        public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
        {
            private SqliteConnection? _connection;

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddUserSecrets<ApiMarker>();

              config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "ThisIsATestKeyThatIsLongEnoughForHmacSha256!!",
                    ["Jwt:ValidIssuer"] = "test-issuer",
                    ["Jwt:ValidAudience"] = "test-audience",
                    ["Jwt:DurationInDays"] = "1"
                });
            });

            return base.CreateHost(builder);
        }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.ConfigureServices(services =>
                {
                    RemoveDbContext<DbIdentityContext>(services);
                    RemoveDbContext<StoreDbContext>(services);

                    services.RemoveAll<IConnectionMultiplexer>();
                    services.RemoveAll<IBasketRepository>();
                    services.AddSingleton<IBasketRepository, FakeBasketRepository>();

                    _connection = new SqliteConnection("DataSource=:memory:");
                    _connection.Open();

                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = "PRAGMA foreign_keys = OFF;";
                        command.ExecuteNonQuery();
                    }

                    services.AddSingleton(_connection);

                    services.AddDbContext<DbIdentityContext>((sp, options) =>
                        options.UseSqlite(sp.GetRequiredService<SqliteConnection>()));

                    services.AddDbContext<StoreDbContext>((sp, options) =>
                        options.UseSqlite(sp.GetRequiredService<SqliteConnection>()));

                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var scopedServices = scope.ServiceProvider;

                    var appDb = scopedServices.GetRequiredService<DbIdentityContext>();
                    var storeDb = scopedServices.GetRequiredService<StoreDbContext>();

                    CreateTables(appDb);
                    CreateTables(storeDb);

                    SeedAsync(scopedServices).GetAwaiter().GetResult();
                });
            }

            private static void RemoveDbContext<T>(IServiceCollection services) where T : DbContext
            {
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<T>) ||
                    d.ServiceType == typeof(T)).ToList();

                foreach (var descriptor in descriptors)
                    services.Remove(descriptor);
            }

            private static void CreateTables(DbContext context)
            {
                var script = context.Database.GenerateCreateScript();

                foreach (var statement in script.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var sql = statement.Trim();
                    if (string.IsNullOrWhiteSpace(sql)) continue;

                    try
                    {
                        context.Database.ExecuteSqlRaw(sql);
                    }
                    catch (SqliteException ex) when (ex.Message.Contains("already exists"))
                    {
                    }
                }
            }

            private static async Task SeedAsync(IServiceProvider sp)
            {
                var userManager = sp.GetRequiredService<UserManager<AppUser>>();
                var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
                var storeDb = sp.GetRequiredService<StoreDbContext>();

                foreach (var role in new[] { "Admin", "Client" })
                {
                    if (!await roleManager.RoleExistsAsync(role))
                        await roleManager.CreateAsync(new IdentityRole(role));
                }

                const string adminEmail = "admin@store.com";
                if (await userManager.FindByEmailAsync(adminEmail) == null)
                {
                    var admin = new AppUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        DisplayName = "Admin",
                        EmailConfirmed = true,
                        LockoutEnabled = false
                        
                    };

                    var result = await userManager.CreateAsync(admin, "Admin#123");
                    if (!result.Succeeded)
                        throw new Exception("Admin seed failed: " +
                            string.Join(", ", result.Errors.Select(e => e.Description)));

                    await userManager.AddToRoleAsync(admin, "Admin");
                }

                const string clientEmail = "client@store.com";
                if (await userManager.FindByEmailAsync(clientEmail) == null)
                {
                    var client = new AppUser
                    {
                        UserName = clientEmail,
                        Email = clientEmail,
                        DisplayName = "Client",
                        EmailConfirmed = true,
                        LockoutEnabled = false
                    };

                    var result = await userManager.CreateAsync(client, "Password123!");
                    if (!result.Succeeded)
                        throw new Exception("Client seed failed: " +
                            string.Join(", ", result.Errors.Select(e => e.Description)));

                    await userManager.AddToRoleAsync(client, "Client");
                }

                if (!await storeDb.Categories.AnyAsync())
                {
                    storeDb.Categories.Add(new Category
                    {
                        Name = "Electronics",
                        Description = "Modern electronic devices",
                        CreatedAt = DateTime.Now.AddDays(-1),
                        LastModifiesAt = DateTime.Now
                    });
                    await storeDb.SaveChangesAsync();
                }
                var category = await storeDb.Categories.FirstAsync();

                if (!await storeDb.Products.AnyAsync())
                {
                    storeDb.Products.Add(new Product
                    {
                        Name = "Dell Laptop",
                        Description = "High performance laptop",
                        Price = 1500.00m,
                        StockQuantity = 5,
                        CategoryId = category.Id,
                        PictureUrl = "images/laptop.jpg",
                        CreatedAt = DateTime.Now.AddDays(-1),
                        LastModifiesAt = DateTime.Now,
                    });
                    await storeDb.SaveChangesAsync();
                }
                var product = await storeDb.Products.FirstAsync();

                if (!await storeDb.DeliveryMethods.AnyAsync())
                {
                    storeDb.DeliveryMethods.Add(new DeliveryMethod
                    {
                        ShortName = "DHL",
                        DeliveryTime = "2-3 Days",
                        Description = "Fast delivery",
                        Cost = 10.00m,
                        Id =1
                                               
                    });
                    await storeDb.SaveChangesAsync();
                }
                var deliveryMethod = await storeDb.DeliveryMethods.FirstAsync();

                if (!await storeDb.Orders.AnyAsync())
                {
                    var shippingAddress = new ShippingAddress
                    {
                        FirstName = "John",
                        LastName = "Doe",
                        Street = "123 Main St",
                        City = "New York",
                        State = "NY",
                        ZipCode = "10001"
                        
                    };

                    var itemOrdered = new ProductItemOrdered(product.Id, product.Name, product.PictureUrl!);
                    var orderItem = new OrderItems(itemOrdered, product.Price, 1);

                    var order = new Domain.Entities.Order(
                        new List<OrderItems> { orderItem },
                        "test@test.com",
                        shippingAddress,
                        deliveryMethod,
                        product.Price,
                        "pi_123456789"
                        
                        );

                    storeDb.Orders.Add(order);
                    await storeDb.SaveChangesAsync();
                }

                var basketRepo = sp.GetRequiredService<IBasketRepository>();
                await basketRepo.UpdateBasketAsync(new CustomerBasket("basket_123")
                {
                    ClientSecret = "secret_123",
                    PaymentIntentId = "pi_123456789",
                    DeliveryMethodId = deliveryMethod.Id,
                    ShippingPrice = deliveryMethod.Cost

                });
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (disposing)
                {
                    _connection?.Dispose();
                }
            }
        }

        public class FakeBasketRepository : IBasketRepository
        {
            private readonly ConcurrentDictionary<string, CustomerBasket> _baskets = new();

            public Task<CustomerBasket?> GetBasketAsync(string basketId)
            {
                _baskets.TryGetValue(basketId, out var basket);
                return Task.FromResult(basket);
            }

            public Task<CustomerBasket?> UpdateBasketAsync(CustomerBasket basket)
            {
                _baskets[basket.Id] = basket;
                return Task.FromResult<CustomerBasket?>(basket);
            }

            public Task<bool> DeleteBasketAsync(string basketId)
            {
                return Task.FromResult(_baskets.TryRemove(basketId, out _));
            }
        }
    }
