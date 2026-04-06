using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ResumeTailorApp.Data;
using ResumeTailorApp.Models;

namespace ResumeTailorApp.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<AppUser> _passwordHasher = new();

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool ok, string error)> RegisterAsync(RegisterViewModel model)
        {
            var email = model.Email.Trim().ToLowerInvariant();

            var exists = await _context.AppUsers.AnyAsync(x => x.Email == email);
            if (exists)
                return (false, "An account with this email already exists.");

            var user = new AppUser
            {
                FullName = model.FullName.Trim(),
                Email = email
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

            _context.AppUsers.Add(user);
            await _context.SaveChangesAsync();

            return (true, string.Empty);
        }

        public async Task<AppUser?> LoginAsync(LoginViewModel model)
        {
            var email = model.Email.Trim().ToLowerInvariant();
            var user = await _context.AppUsers.FirstOrDefaultAsync(x => x.Email == email);

            if (user == null)
                return null;

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
            return result == PasswordVerificationResult.Failed ? null : user;
        }
    }
}