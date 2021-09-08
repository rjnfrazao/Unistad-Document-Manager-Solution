using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Unistad_Document_Manager.Pages
{
    public class PrivacyModel : PageModel
    {

        // Configuration information
        private IConfiguration _configuration;

        private readonly ILogger<PrivacyModel> _logger;

        public PrivacyModel(ILogger<PrivacyModel> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string apiURI = "";

        public void OnGet()
        {

            apiURI = _configuration.GetValue<string>("ApplicationSettings:ApiConsumerUrl");
        }
    }
}
