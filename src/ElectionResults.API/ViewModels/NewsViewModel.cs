using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ElectionResults.Core.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ElectionResults.API.ViewModels
{
    public class NewsViewModel
    {
        [Required(ErrorMessage = "Please enter title")]
        [Display(Name = "Title")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Please enter the link")]
        [Display(Name = "Link")]
        public string Link { get; set; }

        [Required(ErrorMessage = "Please enter the date")]
        public DateTime Date { get; set; }

        public List<SelectListItem> Elections { get; set; }

        [BindProperty]

        [Required(ErrorMessage = "Please select an election")]
        [Display(Name = "Elections")]
        public int? SelectedElectionId { get; set; }
        
        [Required(ErrorMessage = "Please select an author")]
        [Display(Name = "Author")]
        [BindProperty]
        public int? SelectedAuthorId { get; set; }

        public List<SelectListItem> Authors { get; set; }

        [Required(ErrorMessage = "Please add a description for the article")]
        [Display(Name = "Description")]
        public string Body { get; set; }

        [Display(Name = "Pictures")]
        public List<IFormFile> Pictures { get; set; }

        public List<ArticlePicture> UploadedPictures { get; set; }

        public int NewsId { get; set; }
    }
}
