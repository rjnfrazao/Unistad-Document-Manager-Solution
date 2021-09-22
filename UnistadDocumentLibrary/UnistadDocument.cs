using ConfigurationLibrary;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UnistadDocumentLibrary
{
    public class UnistadDocument
    {


        private Dictionary<string, string> _stadiumDictionary;

        private Dictionary<string, string> _serviceDictionary;

        private Dictionary<string, string> _documentDictionary;

        private List<string> _edrmsList;

        private Dictionary<string, string> _targetStadiumFolder;

        private Dictionary<string, string> _targetServiceFolder;

        private Dictionary<string, string> _targetDocumentTypeFolder;


        /// <summary>
        /// Contructor requires the dictionaries used to map the value into a code. The three dictionraires are :
        /// Stadium : Map stadium name to stadium code. Ex. Education City Stadium -> EC
        /// Servicce : Map service name to service code. Ex. Access Control System -> ACS
        /// Document Type : Map document name to document code. Ex. High Level Funcional -> HLFD
        /// </summary>
        /// <param name="stadiumDictionary">Stadium mapping information.</param>
        /// <param name="serviceDictionary">Service mapping information.</param>
        /// <param name="documentDictionary">Document type mapping information.</param>
        /// <param name="edrmsList">List of prefixes used in the EDRMS number to identify an UNISTAD document. </param>
        /// <param name="targetStadiumFolder">Target stadium folder mapping information. </param>
        /// <param name="targetServiceFolder">Target service folder mapping information. </param>
        /// <param name="targetDocumentTypeFolder">Target document folder mapping information. </param>

        public UnistadDocument(Dictionary<string, string> stadiumDictionary,
            Dictionary<string, string> serviceDictionary, 
            Dictionary<string, string> documentDictionary, 
            List<string> edrmsList,
            Dictionary<string, string> targetStadiumFolder,
            Dictionary<string, string> targetServiceFolder,
            Dictionary<string, string> targetDocumentTypeFolder)
        {

            if (stadiumDictionary.Count == 0)
            {
                throw new NullReferenceException("Stadium dictionary can't be empty.");
            }

            if (serviceDictionary.Count == 0)
            {
                throw new NullReferenceException("Service dictionary can't be empty.");
            }

            if (documentDictionary.Count == 0)
            {
                throw new NullReferenceException("Document dictionary can't be empty.");
            }

            _stadiumDictionary = stadiumDictionary;
            _serviceDictionary = serviceDictionary;
            _documentDictionary = documentDictionary;
            _edrmsList = edrmsList;
            _targetStadiumFolder = targetStadiumFolder;
            _targetServiceFolder = targetServiceFolder;
            _targetDocumentTypeFolder = targetDocumentTypeFolder;
        }


        // PDF cover page
        public string CoverPage { get; set ; }

        // PDF second page
        public string SecondPage { get; set; }

        // Flag when false indicates the conversion for any reason failed.
        public bool ConversionOk { get; set; }

        // When conversion fails, this contains the error message.
        public string ConversionErrorMessage { get; set; }



        /// <summary>
        /// This method is used to find out the Stadium Code, Service Code, and Document Type Code.
        /// Scan the page to find out if any item of the data dictionary exists in the page. When found returns the value associated with the key value.
        /// In case more than one item is found, returns the key values separated by "-"
        /// </summary>
        /// <param name="page">Page content to be scanned.</param>
        /// <param name="dictionary">Data dictionay with the key values pair to be scanned.</param>
        /// <param name="firstOccurrency">If true, find first occurrency only, otherwise finds all occurrencies.</param>
        /// <returns>Returns the key vaues separated by "-". Example ID-SB - In case is found Integrated Display and Scoreboard in the page.</returns> 
        private string getCode(string page, Dictionary<string, string> dictionary, bool firstOccurrency)
        {
            string value = "";
            // Assure cover page text is upper case.
            page = page.ToUpper();

            foreach (KeyValuePair<string, string> entry in dictionary)
            {
                // if current key exists in the path name (no case sensitive comparison)
                if (page.Contains(entry.Key.ToUpper()))
                {
                    value = value + "-" + entry.Value;

                    if (firstOccurrency) break;     // If first occurrency is flagged, exit the loop.
                }
            }

            // remove the initial "-"
            if (value!="") { value = value.Substring(1); }

            return value;
        }



        /// <summary>
        /// Get the EDRMS reference number. This is the number used to uniquely identify the documents.
        /// </summary>
        /// <returns>In case the EDRMS number exist, returns the EDRSM number, otherwise returns ""</returns>
        private string getEDRMS()
        {

            // Assure cover page text is upper case.
            CoverPage = CoverPage.ToUpper();
            // "^\\w{2}-\\w{3}-\\w{3}-\\w{3}-\\w{3}-\\w{2}-\\d{4,5}$"
            Regex exp = new Regex("\\w{2}-\\w{3}-\\w{3}-\\w{3}-\\w{3}-\\w{2}-\\d{4,5}");

            // Apply the regular expression in the cover page
            Match match = exp.Match(CoverPage);

            if (match.Success)
            {
                return match.Value.Trim();
            }
            else
            {
                return "";
            }
        }


        /// <summary>
        /// Get the EDRMS reference number. This is the number used to uniquely identify the documents.
        /// It checks two patterns, first : "0.1", "0.2", where the digit after dot is the version or
        /// second : " 1 10-JAN-21" or " 2 15-JUL-20" where the first number is the version.
        /// </summary>
        /// <returns>In case the EDRMS number exist, returns the EDRSM number, otherwise returns ""</returns>
        private string getVersion()
        {


            int higher = 0;         // Higher version number
            int number = 0;         // Current version number

            // Assure cover page text is upper case.
            string page = SecondPage.ToUpper();

            // Sheck must start from "Revision" word onwards
            page = page.Substring(page.IndexOf("REVISION")+1);

            // First Check - Try to find anything with 0.1, 0.2, etc as version is stored using this format in some files.
            // expression to be found for the version number.
            Regex exp = new Regex("\\s0\\.\\d{1,2}");

            // Apply the regular expression in the Second page
            MatchCollection collection = exp.Matches(page);

            // loop all version found in the second page.
            foreach (Match match in collection)
            {
                number = Int16.Parse(match.Value.Substring(match.Value.IndexOf(".")+1));          // Get the current version number.
                if (number>higher) { higher = number; }                                         // Update the higher version.
            }

            if (higher != 0)
            {
                return "-V" + higher.ToString();
            }

            // Second Check - In case first check failed, then try to find anything with space + digit + space + dd-MMM-yy, the first digit is the version.
 
            // expression to be found for the version number. Example " 1 10-JAN-21", the digit "1" is the version.
            exp = new Regex("\\d{1,2}\\s\\d{1,2}[-/]\\w{1,10}[-/]\\d{2,4}\\s");

            // Apply the regular expression in the Second page
            collection = exp.Matches(page);

            // loop all version found in the second page.
            foreach (Match match in collection)
            {

                number = Int16.Parse(match.Value.Trim().Substring(0,2));          // Get the current version number.
                if (number > higher) { higher = number; }                                         // Update the higher version.
            }

            if (higher != 0)
            {
                return "-V" + higher.ToString();
            }

            return "";

        }


        /// <summary>
        /// Main method of this class. This returns the name of the document, based on its content.
        /// The file name is [Stadium code]-[Service code]-[Document type code]-[EDRMS number]-[Documet version], where first version 0 is ommitted.
        /// The following data is mandatory : Stadium, Sercive, Document Type and EDRMS number.
        /// Example of document name EC-IPTV-SAD-SC-C05-CAB-ORD-DBF-IT-00001.pdf
        /// EC - Education City Stadium
        /// IPTV - IPTV Service
        /// SAD - Solution Architecture Document
        /// SC-C05-CAB-ORD-DBF-IT-00001 - ERMS Number.
        /// </summary>
        /// <returns>
        /// First string contains the target folder where the file, the second string contains the file name, 
        /// in case any mandatory field isn't found returns an empty string.
        /// </returns>
        public (string, string) getDocumentName()
        {

            ConversionOk = true;
            ConversionErrorMessage = "";

            // Get Stadium code, if not found update the error message.
            string stadium = getCode(CoverPage, _stadiumDictionary, true);
            if (stadium == "") { ConversionErrorMessage = "Stadium not found. [Error:114]"; }

            // Get the subfolder of the Stadium
            string stadiumFolder = getTargetFolder(stadium, _targetStadiumFolder);
            if (stadiumFolder == "") { ConversionErrorMessage = ConversionErrorMessage + "Stadium target folder not found. [Error:115]"; }

            // Get Service code, if not found update the error message.
            string service = getCode(CoverPage, _serviceDictionary, false);
            if (service == "") { ConversionErrorMessage = ConversionErrorMessage + "Service not found. [Error:114]"; }

            // Get the subfolder of the Service
            string serviceFolder = getTargetFolder(service, _targetServiceFolder);
            if (serviceFolder == "") { ConversionErrorMessage = ConversionErrorMessage + "Service target folder not found. [Error:115]"; }

            // Get Document type code, if not found update the error message.
            string documentType = getCode(CoverPage, _documentDictionary, true);
            if (documentType == "") { ConversionErrorMessage = ConversionErrorMessage + "Document Type not found. [Error:114]"; }

            // Get the subfolder of the Document Type
            string documentTypeFolder = getTargetFolder(documentType, _targetDocumentTypeFolder);
            if (documentTypeFolder == "") { ConversionErrorMessage = ConversionErrorMessage + "Document Type target folder not found. [Error:115]"; }

            // Get EDRMS number, if not found update the error message.
            string edrsNumber = getEDRMS();
            if (edrsNumber == "") { ConversionErrorMessage = ConversionErrorMessage + "EDRMS reference number not found. [Error:114]"; }

            // Get document version.
            string version = getVersion();

            // In case an error happened, set the conversion flag to false.
            if (ConversionErrorMessage != "") { ConversionOk = false; }

            return ($"{stadiumFolder}{ConfigSettings.FILE_SHARE_FOLDER_DELIMITER}{serviceFolder}{ConfigSettings.FILE_SHARE_FOLDER_DELIMITER}{documentTypeFolder}", $"{stadium}-{service}-{documentType}-{edrsNumber}{version}");
        }


        /// <summary>
        /// Returns the target folder associated the code provided. The dictionary contains the mapping between codes and associated folder.
        /// </summary>
        /// <param name="code">Code to search (Stadium code or Service code or Document Type code.</param>
        /// <param name="dictionary">Dictionary maps each code to a target folder. (Stadium target folder, Service target folder, or Document type target folder) </param>
        /// <returns>the target folder, where the file will be saved. Example for Stadium = EC the target folder is "\04. EC"
        /// Example: for Service = IPTV, the target folder is "\Package 1\Base".
        /// In case not found returns "".</returns>
        public string getTargetFolder(string code, Dictionary<string, string> dictionary )
        {

            // Split the code, just in case multiples services are found. Ex.: ID-SB : Integrated Display and Scoreboard.
            var arrayCodes = code.Split('-'); 

            // Use the first code only to work out the subfolder.
            string firstCode = arrayCodes[0];

            // Loop until it finds the code on the dictiorary with codes and subfolder info.
            foreach(KeyValuePair<string, string> item in dictionary)
            {
                if (item.Key.Contains(firstCode))
                {
                    return item.Value;
                }
            }

            return "";
        }


    }
}
