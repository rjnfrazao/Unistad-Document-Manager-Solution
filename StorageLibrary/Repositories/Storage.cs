using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Net;
using Azure.Storage.Blobs.Models;
using Azure;
using StorageLibrary.Library;

namespace StorageLibrary.Repositories
{
    /// <summary>
    /// Encapsulate all methods required to interact with the Azure Storage.
    /// </summary>
    public class Storage : ControllerBase, IStorage 
    {

        private BlobServiceClient _blobServiceClient;


        /// <summary>
        /// Gets or sets the interface for configuration.
        /// </summary>
        private IConfiguration _configuration { get; set; }

        /// <summary>
        /// Gets or sets the interface for Logger.
        /// </summary>
        private ILogger _logger { get; set; }


        private bool IsInitialized { get; set; }

        /// <summary>
        /// Gets the storage connection string.
        /// </summary>
        /// <value>
        /// The storage connection string.
        /// </value>
        public string ConnectionString
        {
            get
            {
                // Connection string stored locally in Secrets.JSON and in production at Azure App configuration connection string;
                return _configuration.GetConnectionString(ConfigSettings.STORAGE_CONNECTIONSTRING_NAME);
                
                // return Environment.GetEnvironmentVariable(_configSettings.STORAGE_CONNECTIONSTRING_NAME);
            }
        }



        /// <summary>
        /// Initializes a new instance of the <see cref="Storage" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="logger">The Logger.</param>
        public Storage(IConfiguration configuration, ILogger<Storage> logger)
        {

            _logger = logger;

            _configuration = configuration;

        }



        /// <summary>
        /// Initializes this instance for use, this is not thread safe
        /// </summary>
        /// <returns>A task</returns>
        /// <remarks>This method is not thread safe</remarks>
        private void InitializeAsync()
        {
            if (!IsInitialized)
            {
                _blobServiceClient = new BlobServiceClient(this.ConnectionString);

                IsInitialized = true;
            }
        }


        /// <summary>
        /// Returns the blob service client
        /// </summary>
        private BlobServiceClient GetBlobServiceClient()
        {
            if (!IsInitialized)
            {
                InitializeAsync();
            }
            return _blobServiceClient;
        }



        /// <summary>
        /// Gets the blob client associated with the blob specified in the fileName
        /// </summary>
        /// <param name="containerName">The container name.</param>
        /// <param name="fileName">The file name which is the blob id</param>
        /// <returns>The corresponding BlobClient for the file at the container.</returns>
        private BlobClient GetBlobClient(string containerName, string fileName)
        {

            // Connection to the service
            _blobServiceClient = GetBlobServiceClient();

            BlobContainerClient blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            return blobContainerClient.GetBlobClient(fileName);
        }





        /// <summary>
        /// Uploads the file to the storage
        /// </summary>
        /// <param name="containerName">The name of the Container.</param>
        /// <param name="fileName">The filename of the file to upload which will be used as the blobId</param>
        /// <param name="fileStream">The correspnding fileStream associated with the fileName</param>
        /// <param name="contentType">The content type of the blob to upload</param>
        /// <return>
        ///         HttpStatusCode - Create - file was created sucssessfuly
        ///                           NoContent - file was updated sucssessfuly.
        ///         Uri - If created, returns the file's URI 
        /// </return>
        /// <remarks>
        ///         If container doesn't exist, creates a public one.
        /// </remarks>
        public async Task<(HttpStatusCode, Uri)> UploadFile(string containerName, string fileName, Stream fileStream, string contentType)
        {

            // Connection to the service
            _blobServiceClient = GetBlobServiceClient();

            // Get the blob container
            BlobContainerClient _blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            // If doesn~t exist, create as public container.
            _blobContainerClient.CreateIfNotExists(publicAccessType: PublicAccessType.BlobContainer);

            // Get the blob for the file
            BlobClient blobClient = GetBlobClient(containerName, fileName);
            if (blobClient.Exists())
            {
                // Blob already exists

                // Delete first
                await blobClient.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots);

                // Upload
                await blobClient.UploadAsync(fileStream);
                await blobClient.SetHttpHeadersAsync(new BlobHttpHeaders() { ContentType = contentType });

                return (HttpStatusCode.NoContent,null);
            }
            else
            {
                // Blob doesn't exist

                // Upload
                await blobClient.UploadAsync(fileStream);
                await blobClient.SetHttpHeadersAsync(new BlobHttpHeaders() { ContentType = contentType });

                return (HttpStatusCode.Created, blobClient.Uri);
            }

        }


        /// <summary>
        /// Deletes file from blob storage
        /// </summary>
        /// <param name="containerName">The name of the Container.</param> 
        /// <param name="fileName"></param>
        public async Task DeleteFile(string containerName, string fileName)
        {
            var blob = GetBlobClient(containerName.ToLower(), fileName);
            await blob.DeleteIfExistsAsync();
        }


        /// <summary>
        /// Gets the file from the blob storage
        /// </summary>
        /// <param name="containerName">The name of the Container.</param>
        /// <param name="fileName">The id of the blob to download</param>
        /// <returns>A memory stream, which must be disposed by the caller, that contains the downloaded blob</returns>
        public async Task<(MemoryStream fileStream, string contentType)> GetFileAsync(string containerName, string fileName)
        {
            BlobClient blobClient = GetBlobClient(containerName, fileName);
            using BlobDownloadInfo blobDownloadInfo = await blobClient.DownloadAsync();


            // Caller is expected to dispose of the memory stream
            MemoryStream memoryStream = new MemoryStream();
            await blobDownloadInfo.Content.CopyToAsync(memoryStream);

            // Reset the stream to the beginning so readers don't have to
            memoryStream.Position = 0;
            return (memoryStream, blobDownloadInfo.ContentType);
        }


        /// <summary>
        /// Gets the file from the blob storage
        /// </summary>
        /// <param name="containerName">The name of the Container.</param>
        /// <param name="fileName"></param>
        /// <returns>A byte array containing the downloaded blob content</returns>
        // <exception cref="InternalException">If the http status is anything other than 404</exception>
        // <exception cref="NotFoundException">If the blob can't be found</exception>
        public async Task<byte[]> GetFileInByteArrayAsync(string containerName, string fileName)
        {

            BlobClient blobClient = GetBlobClient(containerName, fileName);

            using BlobDownloadInfo blobDownloadInfo = await blobClient.DownloadAsync();
            using MemoryStream memoryStream = new MemoryStream();

            Response response = await blobClient.DownloadToAsync(memoryStream);

            if (response.Status == StatusCodes.Status200OK)
            {
                return memoryStream.ToArray();
            }

            if (response.Status == StatusCodes.Status404NotFound)
            {
                //throw new NotFoundException($"FileName: {fileName} ReasonPhrase: {response.ReasonPhrase} Attempt to download blob failed because it was not found");
                _logger.LogWarning("FileName: {fileName}, Attempt to download blob failed because it was not found", fileName);
            }

            //throw new InternalException($"FileName: {fileName} ReasonPhrase: {response.ReasonPhrase} Attempt to download blob failed because it was not found");
            _logger.LogWarning("FileName: {fileName}, Attempt to download blob failed because it was not found", fileName);
            return memoryStream.ToArray();
        }



        /// <summary>
        /// Returns all of the blob names in a container
        /// </summary>
        /// <returns>All of the blob names in a container</returns>
        /// <remarks>This does not scale, for scalability usitlize the pagaing functionaltiy
        /// to page through the blobs in t</remarks>
        public async Task<List<string>> GetListOfBlobs(string containerName)
        {

            // Connection to the service
            _blobServiceClient = GetBlobServiceClient();

            BlobContainerClient blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            List<string> blobNames = new List<string>();

            await foreach (BlobItem blobItem in blobContainerClient.GetBlobsAsync())
            {
                blobNames.Add(blobItem.Name);               
            }

            return blobNames;
        }



        /// <summary>
        /// Minor changes in the container name to assure it's accordingly name's rules.
        /// </summary>
        /// <returns>Container name formatted acording azure naming rules for the container.</returns>
        /// <remarks>Minor changes: to lower case.</remarks>
        public string CleanContainerName(string containerName)
        {
            return containerName.ToLower();
        }



        /// <summary>
        /// Perform minor validations on the container name to avoid unecessary exception handling.
        /// </summary>
        /// <returns>Returns the ApiErrorCode, otherwise NoError in case no error was noticed. <See cref="ApiErrorCode"/></returns>
        /// <remarks>Checks: Minimum of 3 characteres. Refactoring was implemented.</remarks>
        public ApiErrorCode ValidateContainerName(string containerName)
        {

            if (containerName.Length == 0)
            {
                return ApiErrorCode.ParameterIsNull;
            } else
            {
                return ApiErrorCode.NoError;
            }
        }



        /// <summary>
        /// Perform minor validations on the file name to avoid unecessary exception handling.
        /// </summary>
        /// <returns>Returns the ApiErrorCode, otherwise NoError in case no error was noticed. <See cref="ApiErrorCode"/></returns>
        /// <remarks>Checks: Minimum of 3 characteres.</remarks>
        public ApiErrorCode ValidateFileName(string fileName)
        {
            if (fileName.Length == 0)
            {
                return ApiErrorCode.ParameterIsNull;
            } else
            {
                return ApiErrorCode.NoError;
            }
        }



        /// <summary>
        /// Validate the container name.
        /// </summary>
        /// <param name="containerName">The name of the Container.</param>
        /// <returns>ObjectResult Bad request in case the name is invalid, otherwise returns null</returns>
        public ObjectResult ValidateData(string containerName)
        {

            ApiErrorCode errorCode = ValidateContainerName(containerName);
            if (errorCode != ApiErrorCode.NoError)
            {

                // Get Bad Request Object
                BadRequestObjectResult badRequest = BadRequest(ErrorLibrary.GetErrorResponse(((int)errorCode).ToString(), "containerName", containerName, null));

                // Upcasting to retur ObjectResult object
                ObjectResult objectResult = badRequest;

                // ** Testing LOG messages in Azure
                _logger.LogInformation(LoggingEvents.DataValidation, $"Container name is invalid.");

                return objectResult;
            }
            else
            {
                return null;
            }

        }



        /// <summary>
        /// This method is an oveload when required to validate the container name and the file name. 
        /// </summary>
        /// <param name="containerName">The name of the Container.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <returns>ObjectResult Bad request in case any name is invalid, otherwise returns null</returns>
        public ObjectResult ValidateData(string containerName, string fileName)
        {

            // Validate containerName
            ObjectResult objectResult = this.ValidateData(containerName);
            if (objectResult != null) return objectResult;

            // Validate the file name.
            ApiErrorCode errorCode = this.ValidateFileName(fileName);
            if (errorCode != ApiErrorCode.NoError)
            {

                // Get Bad Request Object
                BadRequestObjectResult badRequest = BadRequest(ErrorLibrary.GetErrorResponse(((int)errorCode).ToString(), "fileName", fileName, null));

                // Upcasting to return an ObjectResult object
                objectResult = badRequest;

                // ** Testing LOG messages in Azure
                _logger.LogInformation(LoggingEvents.DataValidation, $"File name is invalid.");

                return objectResult;
            }
            else
            {
                return null;
            }
        }



        /// <summary>
        /// This method is an oveload when required to validate the container name, the file name, and the file data.
        /// </summary>
        /// <param name="containerName">The name of the Container.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="fileData">The file data uploaded.</param>
        /// <returns>ObjectResult Bad request in case any name is invalid, or the file is invalid. Otherwise returns null</returns>
        public ObjectResult ValidateData(string containerName, string fileName, IFormFile fileData)
        {

            // Validate containerName and fileName
            ObjectResult objectResult = this.ValidateData(containerName, fileName);
            if (objectResult != null) return objectResult;

            // Validate if the file is null.
            if (fileData == null)
            {
                // Get Bad Request Object
                BadRequestObjectResult badRequest = BadRequest(ErrorLibrary.GetErrorResponse(((int)ApiErrorCode.ParameterIsNull).ToString(), "fileData", "", null));

                // Upcasting to return an ObjectResult object
                objectResult = badRequest;

                // ** Testing LOG messages in Azure
                _logger.LogInformation(LoggingEvents.DataValidation, $"Blob {fileName} was null.");

                return objectResult;
            }

            // validate if the file is empty
            if (fileData.Length == 0)
            {

                // Get Bad Request Object
                BadRequestObjectResult badRequest = BadRequest(ErrorLibrary.GetErrorResponse(((int)ApiErrorCode.ParameterIsEmpty).ToString(), "fileData", "", null));

                // Upcasting to return an ObjectResult onject
                objectResult = badRequest;

                // ** Testing LOG messages in Azure
                _logger.LogInformation(LoggingEvents.DataValidation, $"Blob {fileName} was empty.");

                return objectResult;
            }
            else
            {
                return null;
            }
        }



        /// <summary>
        /// Validate the existance of the container. 
        /// </summary>
        /// <param name="containerName">The name of the Container.</param>
        /// <returns>ObjectResult Bad request in case of non existance of container, otherwise returns null</returns>
        public ObjectResult ValidateExistance(string containerName)
        {

            ObjectResult objectResult;

            // validate if the container exists.
            if (!this.ContainerExist(containerName))
            {
                // Get Not Found Object
                NotFoundObjectResult notFound = NotFound(ErrorLibrary.GetErrorResponse(((int)ApiErrorCode.EntityNotFound).ToString(), "containerName", containerName, null));

                // Upcasting to return an ObjectResult object
                objectResult = notFound;

                // ** Testing LOG messages in Azure
                _logger.LogInformation(LoggingEvents.DataValidation, $"Blob {containerName} not found.");

                return objectResult;
            } else
            {
                return null;
            }
        }


        /// <summary>
        /// Validate the existance of the container and the file. 
        /// </summary>
        /// <param name="containerName">The name of the Container.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <returns>ObjectResult Bad request in case of non existance of container or file, otherwise returns null</returns>
        public ObjectResult ValidateExistance(string containerName, string fileName)
        {

            ObjectResult objectResult;

            // Check first if container exists.
            objectResult = this.ValidateExistance(containerName);
            if (objectResult != null) return objectResult;

            
            // Check if file exists.
            if (!this.FileExist(containerName, fileName))
            {
                // Get Not Found Object
                NotFoundObjectResult notFound = NotFound(ErrorLibrary.GetErrorResponse(((int)ApiErrorCode.EntityNotFound).ToString(), "fileName", fileName, null));

                // Upcasting to return an ObjectResult object
                objectResult = notFound;

                // Log file not found
                _logger.LogInformation(LoggingEvents.DataValidation, $"Blob {fileName} not found.");

                return objectResult;
            }
            else
            {
                return null;
            }
        }



        /// <summary>
        /// Check if container exists
        /// </summary>
        /// <param name="containerName">The name of the Container.</param>
        /// <returns>boolean - yes (container exist) otherwise false.</returns>
        public bool ContainerExist(string containerName)
        {

            // Connection to the service
            _blobServiceClient = GetBlobServiceClient();

            // Get the blob container
            BlobContainerClient _blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            return _blobContainerClient.Exists();
        }
        
     
     
     
        /// <summary>
        /// Check if file exists
        /// </summary>
        /// <param name="containerName">The name of the Container.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <returns>boolean - yes (file exist) otherwise false.</returns>
        public bool FileExist(string containerName, string fileName)
        {
            // Get the blob for the file
            BlobClient blobClient = GetBlobClient(containerName, fileName);
            
            // return if exists
            return blobClient.Exists();
        }

/*      ** REMOVED AS the approach is checking the job table now.
        /// <summary>
        /// Assignment #4 method required in the storage. 
        /// This methoddeletes all files from the container, where the metadata is marked to be deleted.
        /// </summary>
        /// <returns>All of the blob names in a container</returns>
        /// <remarks>This does not scale, for scalability usitlize the pagaing functionaltiy
        /// to page through the blobs in t</remarks>
        public async Task<List<string>> DeleteBlobsMarkedConverted(string containerName)
        {
            BlobContainerClient blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobs = blobContainerClient.GetBlobsAsync();


            // List<string> blobNames = new List<string>();

            await foreach (var blobPage in blobs.AsPages())
            {
                foreach (var blobItem in blobPage.Values)
                {
                    if(blobItem.Metadata[ConfigSettings.CONVERTED_METADATA_NAME] == ConfigSettings.CONVERTED_METADATA_VALUE)
                    {
                        blobItem.Name;
                    }
                }

            }

            return blobNames;
        }
*/ 

    }
}
