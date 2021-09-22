using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Unistad_Document_Manager.Utils
{
    public static class CommonFunctions

    {
        public static string GetApiUrl(IConfiguration configuration)
        {
            // Get the URI end point
            // apiURI first check an environment variable configured at host, otherwise use the one from appsettings.json
            var apiURI = configuration.GetValue<string>("APPSETTING_ApiConsumerUrl");
            if ((apiURI == null) || (apiURI == ""))
            {
                apiURI = configuration.GetValue<string>("ApplicationSettings:ApiConsumerUrl");
            }

            return apiURI;
        }
    }
}
