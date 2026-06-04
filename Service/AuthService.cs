using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Crypto.Entities;
using Crypto.Interface;
using Crypto.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Crypto.Service
{
    public class AuthService : IAuthService
    {
        
        private readonly IConfiguration _config;

        private readonly JwtSettings _jwtSettings;

        public AuthService(IConfiguration config, IOptions<JwtSettings> jwtOptions)
        {
            _config = config;
            _jwtSettings = jwtOptions.Value;
        }

        public string HashGeneration(string Password)
        {
            string hash = BCrypt.Net.BCrypt.HashPassword(Password);
            return hash;
        }

        public bool PasswordVerify(string inputPassword, string PasswordHash)
        {
            return BCrypt.Net.BCrypt.Verify(inputPassword, PasswordHash);
        }


        public string GenerateJwtToken(Usuario usuario)
        {
            var secretKey = _jwtSettings.SecretKey;
            var issuer = _jwtSettings.Issuer;
            var audience = _jwtSettings.Audience;
            var expirationInMinutes = _jwtSettings.ExpirationInMinutes;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Email, usuario.Email),
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationInMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}