using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;

namespace ElectionResults.Core.Repositories
{
    public interface IAdminRepository
    {
        Result<IdentityUser> GetByUsernameAndPassword(string email, string password);
    }
}