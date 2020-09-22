using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Entities;

namespace ElectionResults.Core.Repositories
{
    public interface IArticleRepository
    {
        Task<Result<List<Article>>> GetAllArticles();

        Task<List<Article>> GetArticlesByBallotId(int ballotId);

        Task<Result> AddArticle(Article article);

        Task<Result> Delete(Article model);

        Task<Result<Article>> GetById(int id);
    }
}