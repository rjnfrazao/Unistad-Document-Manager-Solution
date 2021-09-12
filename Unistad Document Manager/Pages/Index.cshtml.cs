using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using StorageLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Unistad_Document_Manager.utils;
using System.Threading.Tasks;
using System.Text.Json;

namespace Unistad_Document_Manager.Pages
{
    public class IndexModel : PageModel
    {


        private readonly ILogger<IndexModel> _logger;

        // Configuration information
        private IConfiguration _configuration;

        // http client 
        private readonly IHttpClientFactory _clientFactory;

        public IndexModel(IHttpClientFactory clientFactory, ILogger<IndexModel> logger, IConfiguration configuration)
        {
            _clientFactory = clientFactory;
            _logger = logger;

            // Get the URI end point
            apiURI = Utils.GetApiUrl(configuration);
        }


        [BindProperty]
        public IList<JobEntity> jobStatusList { get; set; }

        private string apiURI = "";

        public async Task OnGet()
        {

            using (var client = _clientFactory.CreateClient())      // initialize the http client             
            {
                _logger.LogInformation($"Home Page. Document Manager. {User.Identity.Name}.");

                // set the http request
                var request = new HttpRequestMessage(HttpMethod.Get, apiURI + $"jobs/{User.Identity.Name}");

                // submit the request and wait for the response.
                var response = await client.GetAsync(request.RequestUri);


                if (response.IsSuccessStatusCode)
                {
                    using (var responseStream = await response.Content.ReadAsStreamAsync())
                    {
                        jobStatusList = await JsonSerializer.DeserializeAsync<IList<JobEntity>>(responseStream);
                    }
                }
                else
                {
                    // API call returned an error.                   
                    _logger.LogError($"Error calling the API to retrieve all records.");

                }

            } 


        }
    }
}
