using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Entities;

namespace ElectionResults.Core.Repositories
{
    public interface IPicturesRepository
    {
        Task<Result<List<ArticlePicture>>> AddPictures(List<ArticlePicture> pictures);
        Task<Result> RemovePictures(int modelNewsId);
    }
}