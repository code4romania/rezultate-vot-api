using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using ElectionResults.API.ViewModels;
using ElectionResults.Core.Configuration;
using ElectionResults.Core.Entities;
using ElectionResults.Core.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;

namespace ElectionResults.API.Controllers
{
    public class HomeController : Controller
    {
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IArticleRepository _articleRepository;
        private readonly IElectionsRepository _electionsRepository;
        private readonly IAuthorsRepository _authorsRepository;
        private readonly IPicturesRepository _picturesRepository;
        private readonly IOptions<AWSS3Settings> _awsS3Settings;

        public HomeController(
            IWebHostEnvironment hostEnvironment,
            IArticleRepository articleRepository,
            IElectionsRepository electionsRepository,
            IAuthorsRepository authorsRepository,
            IPicturesRepository picturesRepository,
            IOptions<AWSS3Settings> aws3Settings)
        {
            webHostEnvironment = hostEnvironment;
            _articleRepository = articleRepository;
            _electionsRepository = electionsRepository;
            _authorsRepository = authorsRepository;
            _picturesRepository = picturesRepository;
            _awsS3Settings = aws3Settings;
        }

        public async Task<IActionResult> Index()
        {
            var feeds = await _articleRepository.GetAllArticles();
            ViewBag.Environment = webHostEnvironment.EnvironmentName;
            return View(feeds.Value);
        }

        public async Task<IActionResult> New()
        {
            var newsFeedViewModel = await BuildEditNewsFeedViewModel();
            return View(newsFeedViewModel);
        }

        private async Task<NewsViewModel> BuildEditNewsFeedViewModel()
        {
            var newsFeedViewModel = new NewsViewModel();
            var elections = await _electionsRepository.GetElectionsForNewsFeed();
            newsFeedViewModel.Elections = new List<SelectListItem>();
            foreach (var election in elections.Value)
            {
                var electionGroup = new SelectListGroup { Name = election.ElectionName };
                foreach (var ballot in election.Ballots)
                {
                    newsFeedViewModel.Elections.Add(new SelectListItem(ballot.Name, ballot.BallotId.ToString())
                    {
                        Group = electionGroup
                    });
                }
            }
            newsFeedViewModel.Date = DateTime.Now;

            newsFeedViewModel.SelectedElectionId = null;
            var authors = await _authorsRepository.GetAuthors();
            newsFeedViewModel.Authors = authors.Select(a => new SelectListItem(a.Name, a.Id.ToString())).ToList();
            return newsFeedViewModel;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _articleRepository.Delete(new Article { Id = id });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var newsFeedViewModel = await BuildEditNewsFeedViewModel();
                var result = await _articleRepository.GetById(id);
                var news = result.Value;
                newsFeedViewModel.SelectedAuthorId = news.AuthorId;
                newsFeedViewModel.SelectedElectionId = news.ElectionId;
                newsFeedViewModel.Body = news.Body;
                newsFeedViewModel.NewsId = news.Id;
                newsFeedViewModel.Date = news.Timestamp;
                newsFeedViewModel.Title = news.Title;
                newsFeedViewModel.Link = news.Link;
                newsFeedViewModel.Embed = news.Embed;
                newsFeedViewModel.UploadedPictures = news.Pictures;
                return View("New", newsFeedViewModel);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> New(NewsViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var elections = await _electionsRepository.GetElectionsForNewsFeed();
                    var ballots = elections.Value.SelectMany(e => e.Ballots).ToList();
                    var selectedBallot = ballots
                        .Where(b => b.BallotId == model.SelectedElectionId)
                        .FirstOrDefault();
                    if (selectedBallot == null)
                        selectedBallot = ballots.FirstOrDefault();
                    var pictures = new List<ArticlePicture>();
                    if (model.Pictures != null && model.Pictures.Count > 0)
                    {
                        var uniqueFileNames = await UploadedFiles(model);
                        foreach (var fileName in uniqueFileNames)
                        {
                            pictures.Add(new ArticlePicture
                            {
                                Url = $"{fileName}"
                            });
                        }
                        await _picturesRepository.RemovePictures(model.NewsId);
                    }

                    var newsFeed = new Article
                    {
                        Link = model.Link,
                        Embed = model.Embed,
                        Title = model.Title,
                        Body = model.Body,
                        BallotId = selectedBallot.BallotId,
                        ElectionId = selectedBallot.ElectionId,
                        Timestamp = DateTime.Now,
                        AuthorId = model.SelectedAuthorId.GetValueOrDefault(),
                        Pictures = pictures
                    };
                    newsFeed.Id = model.NewsId;
                    await _articleRepository.AddArticle(newsFeed);
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            var newsFeedViewModel = await BuildEditNewsFeedViewModel();
            return View(newsFeedViewModel);
        }

        private async Task<List<string>> UploadedFiles(NewsViewModel model)
        {
            var filenames = new List<string>();
            var noPictures = model.Pictures == null || (model.Pictures != null && model.Pictures.Count == 0);
            var lessThanTwoPictures = model.Pictures != null && model.Pictures.Count > 0 && model.Pictures.Count < 3;

            if (noPictures || lessThanTwoPictures)
            {
                foreach (var picture in model.Pictures)
                {
                    var uniqueFileName = Guid.NewGuid() + "_" + Path.GetFileName(picture.FileName);
                    
                    await UploadFileToS3(picture, uniqueFileName);

                    filenames.Add(new Uri(new Uri($"https://{_awsS3Settings.Value.BucketName}.s3.amazonaws.com/"), uniqueFileName).ToString());
                }
                return filenames;
            }
            throw new ArgumentException("Wrong number of pictures uploaded");
        }

        public async Task UploadFileToS3(IFormFile file, string filename)
        {
            using (var client = new AmazonS3Client(_awsS3Settings.Value.AccessKeyId, _awsS3Settings.Value.AccessKeySecret, RegionEndpoint.EUWest1))
            {
                using (var newMemoryStream = new MemoryStream())
                {
                    file.CopyTo(newMemoryStream);

                    var uploadRequest = new TransferUtilityUploadRequest
                    {
                        InputStream = newMemoryStream,
                        Key = filename,
                        BucketName = _awsS3Settings.Value.BucketName,
                        CannedACL = S3CannedACL.PublicRead
                    };

                    var fileTransferUtility = new TransferUtility(client);
                    await fileTransferUtility.UploadAsync(uploadRequest);
                }
            }
}
    }
}
