using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElectionResults.Core.Repositories
{
    public class ArticleRepository : IArticleRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public ArticleRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Result<List<Article>>> GetAllArticles()
        {
            var feeds = await _dbContext.Articles
                .Include(n => n.Election)
                .Include(n => n.Ballot)
                .Include(n => n.Author)
                .OrderByDescending(n => n.Timestamp)
                .ToListAsync();
            return Result.Success(feeds);
        }

        public async Task<List<Article>> GetArticlesByBallotId(int ballotId)
        {
            var news = await _dbContext.Articles.Where(n => n.BallotId == ballotId).ToListAsync();
            return news;
        }

        public async Task<Result> AddArticle(Article article)
        {
            _dbContext.Articles.Update(article);
            await _dbContext.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> Delete(Article model)
        {
            var existingNews = await _dbContext.Articles.FirstOrDefaultAsync(n => n.Id == model.Id);
            if (existingNews != null)
            {
                _dbContext.Articles.Remove(existingNews);
                await _dbContext.SaveChangesAsync();
                return Result.Success();
            }
            return Result.Failure("News not found");
        }

        public async Task<Result<Article>> GetById(int id)
        {
            var newsFeed = await _dbContext.Articles
                .AsNoTracking()
                .Include(n => n.Author)
                .Include(n => n.Election)
                .Include(n => n.Pictures)
                .Include(n => n.Ballot)
                .FirstOrDefaultAsync(n => n.Id == id);
            return Result.Success(newsFeed);
        }
    }
}
