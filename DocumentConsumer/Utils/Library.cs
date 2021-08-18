using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocumentConsumer.Utils
{
    class Library
    {

        /// <summary>
        /// Initialize a dictionary, based on configuration data existing in the local.settings.json.
        /// The format must be [data]:[n]:name and [data]:[n]:code
        /// Example:
        /// "Stadium:[0]:name" : "Education City"
        /// "Stadium:[0]:code" : "EC"
        /// </summary>
        /// <param name="configuration">configuration file.</param>
        /// <param name="map">prefix of the data dictionary to be retrieved. Examples : Stadiums, Services, or DocumentType</param>
        /// <returns>The dictionary populated with the data retrieved from the local.settings.json configuration file.</returns>
        public static Dictionary<string, string> InitializeDictionary(IConfiguration configuration, string map)
        {
            int i = 0;
            string keyName;
            string keyCode;
            string name;
            string acronym;
            bool keyExist = true;

            // initialize the dictionary.
            Dictionary<string, string> dictionary = new Dictionary<string, string>();


            do
            {

                keyName = map + ":[" + i + "]:name";
                keyCode = map + ":[" + i + "]:code";

                try
                {
                    name = configuration.GetValue<string>(keyName);
                    acronym = configuration.GetValue<string>(keyCode);
                    dictionary.Add(name, acronym);
                }
                catch (Exception)
                {
                    keyExist = false;   // Trigger to exit the loop.
                }

                i++;    // next item

            } while (keyExist);         // While configuration key exists continue the loop
            //var mapping = configuration.GetValue<T>(map);

            return dictionary;
        }



        /// <summary>
        /// Initialize a list, based on configuration data existing in the local.settings.json.
        /// The format must be [data]:[n]:value 
        /// Example:
        /// "Edrms:[0]:value" : "SC-I60"
        /// "Edrms:[1]:value" : "SC-C05"
        /// </summary>
        /// <param name="configuration">configuration file.</param>
        /// <param name="map">prefix of the data dictionary to be retrieved. Examples : Edrms. </param>
        /// <returns>The List  populated with the data retrieved from the local.settings.json configuration file.</returns>
        public static List<string> InitializeList(IConfiguration configuration, string map)
        {
            int i = 0;
            string keyValue;
            string value;
            bool keyExist = true;

            // initialize the list.
            List<string> list = new List<string>();


            do
            {

                keyValue = map + ":[" + i + "]:value";


                value = configuration.GetValue<string>(keyValue);
                if (value != null)
                    list.Add(value);
                else
                    keyExist = false;   // When value is null, Triggers to exit the loop.
 
                i++;    // next item

            } while (keyExist);         // While configuration key exists continue the loop
            //var mapping = configuration.GetValue<T>(map);

            return list;
        }

    }
}
