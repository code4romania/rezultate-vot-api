using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Entities;

namespace ElectionResults.Core.Repositories
{
    public interface IElectionsRepository
    {
        Task<Result<List<ElectionBallot>>> GetElectionsForNewsFeed();

        Task<Result<List<Election>>> GetAllElections(bool includeBallots = false);
    }
}