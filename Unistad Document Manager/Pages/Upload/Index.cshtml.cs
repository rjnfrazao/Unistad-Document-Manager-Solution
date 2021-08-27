using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using ConfigurationLibrary;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using StorageLibrary.DataTransferObjects;
using StorageLibrary.Repositories;
using Unistad_Document_Manager.Pages.Models;

namespace Unistad_Document_Manager.Pages.Upload
{
    public class IndexModel : PageModel
    {

        // Configuration information
        private IConfiguration _configuration;
        // http client 
        private readonly IHttpClientFactory _clientFactory;


        public IndexModel(IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            _clientFactory = clientFactory;
            _configuration = configuration;

            uploadResultList = new List<UploadResult>();

        }


        [BindProperty]
        public UploadModel DocumentUploaded { get; set; }

        // Result of each file uploaded.
        public class UploadResult
        {

            // File name
            public string fileName { get; set; }
            // Error or success message to be displayed on the page.
            public string textMessage { get; set; }

            // Bootstrap alert class to be used when displaying the message.
            public string htmlMessageClass { get; set; }
            
        }
        
        public List<UploadResult> uploadResultList { get; set; }


        public void OnGet()
        {
    
        }


        public async Task<IActionResult> OnPostUpload()
        {
            //htmlMessageClass = "alert-danger";

            uploadResultList.Clear();

            try
            {

                if (ModelState.IsValid)
                {
                    string fileName = null;


                    // check if file is not null
                    if(DocumentUploaded.Files !=null && DocumentUploaded.Files.Count > 0)
                    {
                        foreach(IFormFile file in DocumentUploaded.Files)
                        {

                            // get the file name
                            fileName = file.FileName;

                            // ** Improvement : Disabled this should be an API to check if file to be uploaded already exist. 
                            // As of now the API used to upload the file will return this error.
                            //if (!await _fileShare.FileExists(ConfigSettings.FILE_SHARE_UPLOADED_FOLDER,fileName))

                            HttpResponseMessage response;

                            // Get the file data
                            var filePath = Path.GetTempFileName();

                            // instatiate the memory stream which will receive the data
                            using (var stream = System.IO.File.Create(filePath))    // initialize the stream temp file
                            using (MemoryStream ms = new MemoryStream())            // initialize the memory stream
                            using (var form = new MultipartFormDataContent())       // initialize the form content
                            using (var client = _clientFactory.CreateClient())      // initialize the http client             
                            {
                                // file uploaded via post is stored into the file stream at the server.
                                await file.CopyToAsync(stream);

                                // set file stream in the beginning
                                stream.Position = 0;

                                // copy file stream to the memory stream
                                await stream.CopyToAsync(ms);

                                // set memory stream in the beginning
                                ms.Position = 0;

                                // copy memory stream to byte array content (format required into the http client post to the API).
                                using (ByteArrayContent filecontent = new ByteArrayContent(ms.ToArray()))
                                {
                                    // Set the Content Type for the file
                                    filecontent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType); // MediaTypeHeaderValue.Parse("multipart/form-data");

                                    // update the content (byte array) into the form field "fileData" API form field                      
                                    form.Add(filecontent, "fileData", file.FileName);


                                    // Get the URI end point
                                   string apiURL = _configuration.GetValue<string>("ApplicationSettings:ApiConsumerUrl");

                                    // set the http request
                                    var request = new HttpRequestMessage(HttpMethod.Put, apiURL);

                                    // submit the request and wait for the response.
                                    response = await client.PostAsync(request.RequestUri, form);

                                } // Dispose fileContent, not needed anymore.


                            } // Dispose file stream, memory stream, form content, and http client.

                            if (response.IsSuccessStatusCode)
                            {
                                // API call (PUT assync) was succssesfuly.
                                uploadResultList.Add(new UploadResult() {
                                    fileName = fileName,
                                    htmlMessageClass = "alert-success",
                                    textMessage = $"Uploaded successfuly.",
                                    });
                            
                            }
                            else
                            {
                                // API call returned an error.
                                using var responseStream = await response.Content.ReadAsStreamAsync();

                                ErrorResponse errorResponse = await JsonSerializer.DeserializeAsync<ErrorResponse>(responseStream);

                                // API call (PUT assync) was succssesfuly.
                                uploadResultList.Add(new UploadResult()
                                {
                                    fileName = fileName,
                                    htmlMessageClass = "alert-danger",
                                    textMessage = $"Error at {errorResponse.parameterName} with value {errorResponse.parameterValue}." +
                                                $"Error ({errorResponse.errorNumber}) message : {errorResponse.errorDescription}",
                                });

                            }
                        }
                        /*
                        // Read the file as Stream.
                        using (Stream stream = DocumentUploaded.Files.OpenReadStream())
                        {
                            // Assure the stream is in the beginning.
                            stream.Position = 0;

                            // Save the file into the uploaded folder.
                            bool resultOk = await _fileShare.SaveFileUploaded(ConfigSettings.FILE_SHARE_UPLOADED_FOLDER, fileName, stream);

                            // Check if the file was saved.
                            if (resultOk)
                            {
                                htmlMessageClass = "alert-success";
                                textMessage = "File uploaded successfuly.";
                            } 
                            else
                            {
                                // File wasn't saved.
                                textMessage = $"Internal error. The file {fileName} couldn't be saved, please try it again.";
                            }
                        }
                        */
                    } 
                    else
                    {
                        uploadResultList.Add(new UploadResult()
                        {
                            fileName = "Not found",
                            htmlMessageClass = "alert-danger",
                            textMessage = "The file is missing.",
                        });
               
                    }
                }
                else
                {
                    // ModelState is invalid.
                    uploadResultList.Add(new UploadResult()
                    {
                        fileName = "Not found",
                        htmlMessageClass = "alert-danger",
                        textMessage = "The file is invalid.",
                    });
                    
                }

                // Display the same page
                return Page();
            }
            catch (Exception ex)
            {
                // ModelState is invalid.
                uploadResultList.Add(new UploadResult()
                {
                    fileName = "Not found",
                    htmlMessageClass = "alert-danger",
                    textMessage = $"Internal error. Exception message {ex.Message}.",
                });
                
                return Page();
            }

        }
    }
}
