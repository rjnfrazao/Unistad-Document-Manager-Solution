using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Unistad_Document_Manager.Utils;

namespace Unistad_Document_Manager.Pages.Upload
{
    public class LogModel : PageModel
    {
        // logger 
        private readonly ILogger<IndexModel> _logger;


        // http client 
        private readonly IHttpClientFactory _clientFactory;

        public LogModel(IHttpClientFactory clientFactory, ILogger<IndexModel> logger, IConfiguration configuration)
        {
            _clientFactory = clientFactory;
            _logger = logger;

            // Get the URI end point
            apiURI = CommonFunctions.GetApiUrl(configuration);

            jobStatusList = new List<JobEntityString>();
        }



        [BindProperty]
        public IList<JobEntityString> jobStatusList { get; set; }

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
                            jobStatusList = await JsonSerializer.DeserializeAsync<IList<JobEntityString>>(responseStream);


                            // Timestamp order by descending, so newest records are displayed on top. jobStatusList.OrderByDescending(r => r.Timestamp);
                            jobStatusList = jobStatusList.OrderByDescending(r => r.timestamp).ToList();
                        }
                    }
                    else
                    {
                        // API call returned an error.                   
                        _logger.LogError($"Document Manager : Index : OnGet : Error calling the API to retrieve all records.");

                    }

                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"Document Manager : Index : OnGet : Unexpected erro : " + ex.Message);
            }


        }
    }


}
