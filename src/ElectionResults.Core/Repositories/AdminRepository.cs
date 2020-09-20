using System.Linq;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Extensions;
using Microsoft.AspNetCore.Identity;

namespace ElectionResults.Core.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public AdminRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Result<IdentityUser> GetByUsernameAndPassword(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                return Result.Failure<IdentityUser>("The email and password must not be empty");

            var user = _dbContext.Users.SingleOrDefault(x => x.Email == email && x.PasswordHash == password.Sha256());
            
            if (user == null)
                return Result.Failure<IdentityUser>("The user doesn't exist");

            return Result.Success(user);
        }
    }
}
