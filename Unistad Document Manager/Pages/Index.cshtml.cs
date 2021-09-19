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

        // logger 
        private readonly ILogger<IndexModel> _logger;


        // http client 
        private readonly IHttpClientFactory _clientFactory;

        public IndexModel(IHttpClientFactory clientFactory, ILogger<IndexModel> logger, IConfiguration configuration)
        {
            _clientFactory = clientFactory;
            _logger = logger;

            // Get the URI end point
            apiURI = Utils.GetApiUrl(configuration);

            jobStatusList = new List<JobEntity>();
        }


        [BindProperty]
        public IList<JobEntity> jobStatusList { get; set; }

        private string apiURI = "";

        public async Task OnGet()
        {
            try
            {

                using (var client = _clientFactory.CreateClient())      // initialize the http client             
                {
                    _logger.LogInformation($"Document Manager : Index : OnGet : by user {User.Identity.Name ?? "User not logged yet."}.");

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
                        _logger.LogError($"Document Manager : Index : OnGet : Error calling the API to retrieve all records.");

                    }

                }

            } catch (Exception ex)
            {
                _logger.LogError($"Document Manager : Index : OnGet : Unexpected erro : " + ex.Message);
            }


        }
    }
}
