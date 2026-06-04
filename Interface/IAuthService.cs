using Crypto.Entities;

namespace Crypto.Interface
{
    interface IAuthService
    {
        public string HashGeneration(string Password);

        public bool PasswordVerify(string inputPassword, string PasswordHash);

        public string GenerateJwtToken(Usuario usuario);
    }
}