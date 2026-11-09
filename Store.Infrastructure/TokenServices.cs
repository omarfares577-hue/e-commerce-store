using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Store.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Store.Application.Contracts;
namespace Store.Infrastructure
{
    public class TokenServices : ITokenService
    {
        private readonly IConfiguration _config;
        public TokenServices(IConfiguration config) { _config = config; }
        public async Task<string> CreateTokenAsync(AppUser user,UserManager<AppUser> usermanager)
        {
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim (ClaimTypes.GivenName,user.DisplayName),
                new Claim (ClaimTypes.Email,user.Email!)
            };
            var userRoles = await usermanager.GetRolesAsync(user);
            foreach (var role in userRoles)
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            var authKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Key"]!));
            var token = new JwtSecurityToken(
                 issuer: _config["JWT:ValidIssuer"],
                 audience: _config["JWT:ValidAudience"],
                 expires: DateTime.Now.AddDays(double.Parse(_config["JWT:DurationInDays"]!)),
                 claims: authClaims,
                 signingCredentials: new SigningCredentials(authKey, SecurityAlgorithms.HmacSha256Signature)
             );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
