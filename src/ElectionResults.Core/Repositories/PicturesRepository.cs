using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElectionResults.Core.Repositories
{
    public class PicturesRepository : IPicturesRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public PicturesRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Result<List<ArticlePicture>>> AddPictures(List<ArticlePicture> pictures)
        {
            _dbContext.AddRange(pictures);
            await _dbContext.SaveChangesAsync();
            return Result.Success(pictures);
        }

        public async Task<Result> RemovePictures(int modelNewsId)
        {
            var picturesForId = await _dbContext.ArticlePictures.Where(p => p.ArticleId == modelNewsId).ToListAsync();
            if (picturesForId.Any())
            {
                _dbContext.ArticlePictures.RemoveRange(picturesForId);
                await _dbContext.SaveChangesAsync();
            }
            return Result.Success();
        }
    }
}