using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Entities;

namespace ElectionResults.Core.Repositories
{
    public interface ITerritoryRepository
    {
        Task<Result<List<County>>> GetCounties();
        Task<Result<List<Locality>>> GetLocalities(int? countyId);
        Task<Result<List<Country>>> GetCountries();
    }
}