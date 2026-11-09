using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Store.API.Errors;
using Store.API.Helpers;
using Store.Application;
using Store.Application.Contracts;
using Store.Application.Dtos.Identity;
using Store.Domain.Entities.Identity;
using System.Security.Claims;
using System.Text;

namespace Store.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : BaseApiController
    {
        private readonly IEmailservices _emailService;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ITokenService _tokenservice;
        private readonly IConfiguration _config;
        public AccountController(IConfiguration configuration, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ITokenService tokenservice,IEmailservices emailservices)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenservice = tokenservice;
            _emailService = emailservices;
            _config = configuration;
        }
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto Dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var existingUser = await _userManager.FindByEmailAsync(Dto.Email);
            if(existingUser != null)
            {
                return BadRequest(new ApiResponse(400,"Email  Already in use."));
            }
            var user = new AppUser
            {
                DisplayName = Dto.DisplayName,
                Email = Dto.Email,
                UserName = Dto.Email,
                PhoneNumber = Dto.PhoneNumber
            };
            var result = await _userManager.CreateAsync(user, Dto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new ApiResponse(400, errors));
            }
            await _userManager.AddToRoleAsync(user, "Client");
            return Ok(new UserDto
            {
                DisplayName = user.DisplayName,
                Email = user.Email,
                Token = await _tokenservice.CreateTokenAsync(user,_userManager)
            });

        }

        [Authorize(Roles ="Admin")]
        [HttpPost("create-admin")]
        public async Task<ActionResult<UserDto>> CreateAdmin(RegisterDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var existingemail = await _userManager.FindByEmailAsync(dto.Email);
            if(existingemail != null )
                return BadRequest(new ApiResponse(400, "Email  Already in use."));
            
            var admin = new AppUser
            {
                DisplayName = dto.DisplayName,
                Email = dto.Email,
                UserName = dto.Email.Split('@')[0],
                PhoneNumber = dto.PhoneNumber
            };
            var result = await _userManager.CreateAsync(admin, dto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new ApiResponse(400, errors));
            }
            await _userManager.AddToRoleAsync(admin, "Admin");
            return Ok(new UserDto
            {
                DisplayName= admin.DisplayName,
                Email= admin.Email,
                Token = await _tokenservice.CreateTokenAsync(admin,_userManager)
            });
        }
        [EnableRateLimiting("loginPolicy")]
        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto Dto)
        {
            var user = await _userManager.FindByEmailAsync(Dto.Email);
            if(user == null)
            {
                return Unauthorized(new ApiResponse(401));
            }
            if(user.IsPermanentlyBanned == true)
            {
                return BadRequest("Your Email is Permanently Banned, Check your Email To reactivate it");

            }
            if (await _userManager.IsLockedOutAsync(user))
            {
                return BadRequest(new { message = "Account locked out for 15 minutes due to multiple failed login attempts." });
            }
            var result = await _signInManager.CheckPasswordSignInAsync(user, Dto.Password, true);

            if (result.IsLockedOut)
            {
                user.LockoutCount++;
                await _userManager.UpdateAsync(user);

                if (user.LockoutCount >= 2)
                {
                    user.IsPermanentlyBanned = true;
                    await _userManager.UpdateAsync(user);   
                    var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(resetToken));
                    Console.WriteLine($"RAW TOKEN: {resetToken}");
                    var reactivateLink =
                        $"https://localhost:7209/api/Account/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={encodedToken}";

                    var email = EmailTemplates.GetAccountBannedEmailTemplate(user.DisplayName, reactivateLink);
                    await _emailService.SendEmailAsync(user.Email!, "Security Alert: Account Permanently Banned", email);

                    return BadRequest(new { message = "Account permanently banned due to repeated suspicious activity. Check your email for reactivation steps." });
                }

                await _userManager.UpdateAsync(user);
                return BadRequest(new { message = "Account locked out for 15 minutes due to multiple failed login attempts." });
            }
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, Dto.Remeberme);
                if(user.LockoutCount >0)
                {
                    user.LockoutCount = 0;
                    await _userManager.UpdateAsync(user);
                }
            }
            if(!result.Succeeded)
            {
                return Unauthorized(new ApiResponse(401));
            }
            return Ok(new UserDto
            {
                DisplayName = user.DisplayName,
                Email = user.Email!,
                Token = await _tokenservice.CreateTokenAsync(user,_userManager)
            });
        }
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromQuery] string email, [FromQuery] string token, [FromBody] ResetPasswordDto dto)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
                return BadRequest(new { message = "Invalid Reset Link" });

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return BadRequest(new { message = "Invalid Request" });


            string decodedToken;
            try
            {
                decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            }
            catch
            {
                return BadRequest(new { message = "Invalid Reset Link" });
            }

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, dto.NewPassword);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            user.IsPermanentlyBanned = false;
            user.LockoutCount = 0;
            await _userManager.UpdateAsync(user);
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            return Ok(new { message = "Password has been successfully reset. You can now login with your new password." });
        }
        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Ok(new { message = "If the email exists, a password reset link has been sent." });

            if (user.IsPermanentlyBanned)
            {
                return BadRequest(new { message = "Your account is permanently banned. Please check your email for Reset steps." });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var resetlink =
                $"https://localhost:7209/api/Account/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={encodedToken}";

            var email = EmailTemplates.GetForgotPasswordEmailTemplate(user.DisplayName, resetlink);
            await _emailService.SendEmailAsync(user.Email!, "Reset Password Request", email);

            return Ok(new { message = "If the email exists, a password reset link has been sent." });
        }
        [Authorize]
        [HttpGet("currentuser")]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email!);
            return Ok(new UserDto
            {
                DisplayName = user!.DisplayName,
                Email = user.Email!,
                Token = await _tokenservice.CreateTokenAsync(user, _userManager)
            });
        }
    }
}
