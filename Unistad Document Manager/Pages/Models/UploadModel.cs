using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Unistad_Document_Manager.Pages.Models
{
    public class UploadModel
    {
        [Required]
        public List<IFormFile> Files { get; set; }
    }
}
