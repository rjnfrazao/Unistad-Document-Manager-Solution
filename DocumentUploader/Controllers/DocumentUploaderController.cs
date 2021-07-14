using Azure;
using StorageLibrary.DataTransferObjects;
using StorageLibrary.Library;
using StorageLibrary.Models;
using StorageLibrary.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DocumentUploader.Exceptions;
using DocumentUploader.DataTransferObjects;
using StorageLibrary;

namespace DocumentUploader.Controllers
{
    [Route("api/v1/")]
    [ApiController]
    public class DocumentUploaderController : ControllerBase
    {

        /// <summary>
        /// The get blob by identifier route
        /// </summary>
        private const string _retrieveJobById = "RetrieveJobById";


        /// <summary>
        /// Storage instance
        /// </summary>
        private readonly IStorage _storage;


        /// <summary>
        /// Queue instance
        /// </summary>
        private readonly IQueue _queue;


        /// <summary>
        /// File Share instance
        /// </summary>
        private readonly IFileShare _fileShare;

        /// <summary>
        /// Logger instance
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Configuration instance
        /// </summary>
        private readonly IConfiguration _configuration;



        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentUploaderController"/> class.
        /// </summary>
        /// <param name="storage">Storage interface.</param>
        /// <param name="queue">Queue interface.</param>
        /// <param name="logger">Log object used to log messages.</param>
        /// <param name="configuration">Configuration object with the application settings.</param>
        public DocumentUploaderController(IStorage storage,
                                IQueue queue,
                                IFileShare fileShare,
                                ILogger<DocumentUploaderController> logger,
                                IConfiguration configuration)

        {
            _storage = storage;
            _queue = queue;
            _fileShare = fileShare;
            _logger = logger;
            _configuration = configuration;

        }



        /// <summary>
        /// Stores the file received via API into the storage File Share, add a message to the storage queue, in the end add a record to the Job Table Status.
        /// </summary>
        /// <param name="fileData">File data</param>
        /// <returns>Returns 201 if file is created, and 400 in case of BadRequest.</returns>
        // Post: tasks/
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.Created)]               //201
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]   //400
        [HttpPost]
        [Route("uploadedimages")]
        public async Task<IActionResult> Create(IFormFile fileData)
        {
            string directory = "";
            string fileName = "";
            

            try
            {

                // Log create process started.
                _logger.LogInformation(LoggingEvents.InsertItem, $"POST : Starting upload image. ConversionMode : {imageConversionMode}");


                // Initialize variables.
                var containerName = ConfigSettings.UPLOADED_CONTAINERNAME;
                fileName = fileData.FileName;
                directory = ConfigSettings.FILE_SHARE_UPLOADED_FOLDER;

                 // "fileData" Parameter is required 
                /*if (imageConversionMode is null)
                {
                    throw new ParameterIsRequired("fileData");
                }*/


                // * REVIEW IF NEEDED or NOT -> Validate the container name, file name, and file data.
                //ObjectResult objectResult = _storage.ValidateData(containerName, fileName, fileData);
                //if (objectResult != null) return (BadRequestObjectResult)objectResult;

                // Validate if uploaded file is duplicated.
                if (await _fileShare.FileExists(directory, fileName))
                {
                    var message = $"POST: File {fileName} already exist in the uploaded folder: {directory} .";
                    throw new InvalidDataInput(message);
                }



                // Log data input is valid.
                _logger.LogInformation(LoggingEvents.InsertItem, $"POST : File data is valid.");


                // * REVIEW IF NEEDED or NOT -> Clean the container name to avoid unecessary error exceptions, such as caused by capital letter.
                containerName = _storage.CleanContainerName(containerName);

                // Save to the uploaded directory
                using Stream fileStream = fileData.OpenReadStream();
                if (await _fileShare.SaveFileUploaded(directory, fileName, fileStream))
                {
                        // string imageSource = uri.ToString();
                    // Log file was created
                    _logger.LogInformation(LoggingEvents.InsertItem, $"POST : File was stored successfuly.");

                    var jobId = Guid.NewGuid().ToString(); 

                    // Add to the Queue.
                    var message = new QueueJobMessage(containerName, jobId, fileName);
                    await _queue.AddQueueMessage(message, null);

                    // (#) Instatiate TableProcessor but inject JobTable object.
                    var tableProcessor = new TableProcessor(new JobTable(_logger, _configuration, conversionMode.ToString()));

                    // Create record job status with status Queued.
                    await tableProcessor.CreateJobTableWithStatus(_logger, fileName, conversionMode.ToString(), imageSource);

                    // Created successfuly. Returns created status with the location of the blob uploaded
                    //return CreatedAtRoute(uri.ToString(), new { containerName=containerName, fileName = fileName });
                    return CreatedAtRoute(_retrieveJobById, new { imageConversionMode = conversionMode, id = fileName }, null);
                    //return Created(imageSource, null);

                }
                else
                {
                    throw new InvalidDataInput($"POST: File failed to be created in the storage. HttpStatusReturned : {statusCode} and file location URI : {uri}");
                }


            }
            // ** REFACTORING is needed passing this catch to the Exception Middleware and inplementing the log in the middleware. not here.
            // Exception throw by the Azure Storage Blob
            catch (RequestFailedException ex)
                when (ex.ErrorCode == "InvalidResourceName")
            {
                // Just in case any invalid resource name was't checked previously. Such as starting with symbols or others situations.
                _logger.LogError(LoggingEvents.InsertItem, ex, $"POST : Invalid Resource Name. File : {fileName} or Container : {containerName}.");

                // Data parameters to be passed to the exception middleware.
                ex.Data["errorNumber"] = (int)ApiErrorCode.InvalidContainerName;
                ex.Data["paramName"] = "containerName";
                ex.Data["paramValue"] = containerName;

                // rethrow to the middleware.
                throw;
            }
            catch (InvalidDataInput ex)
            {
                // Raised in case a duplicated file was uploaded, before the previous one be consumed / processed. 
                _logger.LogError(LoggingEvents.InsertItem, ex, ex.Message);

                // Data parameters to be passed to the exception middleware.
                ex.Data["errorNumber"] = (int)ApiErrorCode.EntityAlreadyExist;
                ex.Data["paramName"] = "fileData.FileName";
                ex.Data["paramValue"] = fileData.FileName;
                ex.Data["customizedMsg"] = ApiSettings.UPLOADED_FILE_DUPLICATED_MSG; // inform middleware to customize error message. Replace [customizedMsgHere] by this string.

                // rethrow to the middleware.
                throw;
            }
            catch (ParameterIsRequired ex)
            {
                // Just in case any invalid resource name was't checked previously. Such as starting with symbols or others situations.
                _logger.LogError(LoggingEvents.InsertItem, ex, $"POST : The parameter {ex.Message} is required.");

                // Data parameters to be passed to the exception middleware.
                ex.Data["errorNumber"] = (int)ApiErrorCode.ParameterIsRequired;
                ex.Data["paramName"] = ex.Message;
                ex.Data["paramValue"] = "";

                // rethrow to the middleware.
                throw;
            }
            catch (Exception ex)
            {
                // Just in case any invalid resource name was't checked previously. Such as starting with symbols or others situations.
                _logger.LogError(LoggingEvents.InsertItem, ex, $"POST : Internal Error. Error message : {ex.Message}");

                // Data parameters to be passed to the handling error middleware.
                ex.Data["errorNumber"] = (int)ApiErrorCode.InternalError;
                ex.Data["paramName"] = "Not applicable";
                ex.Data["paramValue"] = "";

                // rethrow to the middleware.
                throw;
            }


        }


        /// <summary>
        /// Gets the specified file based on the name (GUID).
        /// </summary>
        /// <param name="id">Name of the file</param>
        /// <returns>
        /// Ok - returns the file
        /// Not Found - returns error reponse message.
        /// </returns>
        /// <remarks>
        /// Demo Notes:
        /// In case file is not found, returns 404 error.</remarks>

        [ProducesResponseType(typeof(Stream), (int)HttpStatusCode.OK)] //200
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]   //404
        [HttpGet("uploadedimages/{id}")]
        public async Task<ActionResult> Get(string id)
        {
            string containerName = "";
            string fileName = "";

            try
            {

                // Initialize variables.
                containerName = ApiSettings.UPLOADED_CONTAINERNAME;
                fileName = id;

                // Log Get starts
                _logger.LogInformation(LoggingEvents.GetItem, $"GET : Starting process. File : {id} at Container : {containerName}.");


                // Validate data input
                ObjectResult objectResult = _storage.ValidateData(containerName, fileName);
                if (objectResult != null) return (BadRequestObjectResult)objectResult;

                // LOG data validation completed.
                _logger.LogInformation(LoggingEvents.InsertItem, $"GET : All paramenters are valid.");


                // Validate container and file existance.
                objectResult = _storage.ValidateExistance(containerName, fileName);
                if (objectResult != null) return (NotFoundObjectResult)objectResult;

                // LOG existance validation completed.
                _logger.LogInformation(LoggingEvents.InsertItem, $"GET : Container and file exist.");

                // validate if the file exists.
                if (!_storage.FileExist(containerName, fileName))
                {
                    // File doesn't exist                
                    return NotFound(ErrorLibrary.GetErrorResponse(((int)ApiErrorCode.EntityNotFound).ToString(), "fileName", fileName, null));
                }

                // Returns the file
                (MemoryStream memoryStream, string contentType) = await _storage.GetFileAsync(containerName, fileName);
                return File(memoryStream, contentType);

            }
            catch (Exception ex)
            {
                // Log an error.
                _logger.LogError(LoggingEvents.GetItem, $"GET Blob Unexpected error when returning {fileName} at container {containerName}.");

                // Data parameters to be passed to the handling error middleware.
                ex.Data["errorNumber"] = (int)ApiErrorCode.InternalError;
                ex.Data["paramName"] = "Not applicable";
                ex.Data["paramValue"] = "";

                // rethrow to the middleware.
                throw;

            }


        }



        /// <summary>
        /// Returns all blobs in the container.
        /// </summary>
        /// <returns>Ok - List of files (blobs).
        ///          BadRequest - Container not found or any other internal error.
        /// </returns>
        /// <remarks></remarks>    

        [ProducesResponseType(typeof(BlobNameResponse[]), (int)HttpStatusCode.OK)] //200
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]  //400
        [HttpGet("uploadedimages")]

        public async Task<IActionResult> RetrieveAllBlobs()
        {
            string containerName = "";

            try
            {

                // Initialize variables.
                containerName = ApiSettings.UPLOADED_CONTAINERNAME;

                // Log retrieve all starts
                _logger.LogInformation(LoggingEvents.GetItem, $"GET ALL Blobs : Starting process. Container : {containerName}.");


                // Validate data input
                ObjectResult objectResult = _storage.ValidateData(containerName);
                if (objectResult != null) return (BadRequestObjectResult)objectResult;

                // Validate container existance.
                objectResult = _storage.ValidateExistance(containerName);
                if (objectResult != null) return (NotFoundObjectResult)objectResult;

                // LOG existance validation completed.
                _logger.LogInformation(LoggingEvents.GetItem, $"GET ALL Blobs : Container exist.");


                // Retrieve Blobs
                var returnAllBlobs = new List<BlobNameResponse>();
                List<string> allBlobs = await _storage.GetListOfBlobs(containerName);

                // Loop Blobs to build the response.
                foreach (string b in allBlobs)
                {
                    //r.name = b;    
                    returnAllBlobs.Add(new BlobNameResponse { id = b });
                }

                return new ObjectResult(returnAllBlobs);

            }
            catch (Exception ex)
            {
                // Log an error.
                _logger.LogError(LoggingEvents.GetItem, $"Get All Blobs : Unexpected error when retrieving Blobs at container {containerName}.");

                // Data parameters to be passed to the handling error middleware.
                ex.Data["errorNumber"] = (int)ApiErrorCode.InternalError;
                ex.Data["paramName"] = "Not applicable";
                ex.Data["paramValue"] = "";

                // rethrow to the middleware.
                throw;
            }


        }


        /// <summary>
        /// Returns all Jobs in the storage table (imageconversionjobs).
        /// </summary>
        /// <returns>Ok - List of files (blobs).
        ///          BadRequest - Container not found or any other internal error.
        /// </returns>
        /// <remarks></remarks>    

        [ProducesResponseType(typeof(JobStatusResponse[]), (int)HttpStatusCode.OK)] //200
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]  //400
        [HttpGet("jobs")]

        public async Task<IActionResult> RetrieveAllJobs()
        {
            try
            {
                _logger.LogInformation(LoggingEvents.GetItem, $"GET ALL Jobs : Storage table {ApiSettings.TABLE_JOBS_NAME}.");

                // Initiate a list of JobStatusResponse
                var jobStatusList = new List<JobStatusResponse>();
                var jobStatus = new JobStatusResponse(new JobEntity());

                // ** REFACTORING is needed -> Notice mode added, but we will return all records.
                var jobTable = new JobTable(_logger, _configuration, "1");

                foreach (JobEntity entity in await jobTable.RetrieveJobEntityAll())
                {
                    jobStatus = new JobStatusResponse(entity);

                    jobStatusList.Add(jobStatus);
                }

                return new ObjectResult(jobStatusList);

            }
            catch (Exception ex)
            {
                // Log an error.
                _logger.LogError(LoggingEvents.GetItem, $"Get ALL Jobs : Unexpected error when retrieving job status records. Storage table {ApiSettings.TABLE_JOBS_NAME}.");

                // Data parameters to be passed to the handling error middleware.
                ex.Data["errorNumber"] = (int)ApiErrorCode.InternalError;
                ex.Data["paramName"] = "Not applicable";
                ex.Data["paramValue"] = "";

                // rethrow to the middleware.
                throw;
            }
        }


        /// <summary>
        /// Retrieve Job by imageconversionMode and Job Id (GUID).
        /// </summary>
        /// <param name="imageConversionMode">Image conversion mode applied.</param>
        /// <param name="id">Name (GUID) of the file or id of the file.</param>
        /// <returns>Ok - Job status response record.
        ///          Not Found - Entity could not be found.
        /// </returns>
        /// <remarks></remarks>    

        [ProducesResponseType(typeof(JobStatusResponse[]), (int)HttpStatusCode.OK)]     //200
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]     //404
        [HttpGet("imageconversionmodes/{imageConversionMode}/jobs/{id}", Name = "RetrieveJobById")]

        public async Task<IActionResult> RetrieveJobById(int imageConversionMode, string id)
        {
            try
            {

                _logger.LogInformation(LoggingEvents.GetItem, $"GET a Job : Image conversion mode {imageConversionMode} , Id {id}.");

                // Validate the image conversion mode.
                if (imageConversionMode < 1 || imageConversionMode > ApiSettings.CONVERSIONMODE_MAX)
                {
                    throw new InvalidDataInput($"POST: Image conversion mode invalid. Mode = {imageConversionMode}.");
                }

                // ** REFACTORING is needed in case conversion mode is removed from the constructor.
                var jobTable = new JobTable(_logger, _configuration, imageConversionMode.ToString());

                // Entity has the data returned from the storage table.
                // var entity = await table.ExecuteQuerySegmentedAsync(jobStatusQuery, null);
                var entity = await jobTable.RetrieveJobEntity(id);

                if (entity == null)
                {
                    return new NotFoundObjectResult(ErrorLibrary.GetErrorResponse(((int)ApiErrorCode.EntityNotFound).ToString(), "id", id, null));
                }

                // Update the JobStatusResponse DataObject with the JobEntity data
                var jobStatusResponse = new JobStatusResponse(entity); // jobEntity

                // return the JobStatusReponse
                return new ObjectResult(jobStatusResponse);
            }
            catch (InvalidDataInput ex)
            {
                // Just in case any invalid resource name was't checked previously. Such as starting with symbols or others situations.
                _logger.LogError(LoggingEvents.InsertItem, ex, ex.Message);

                // Data parameters to be passed to the exception middleware.
                ex.Data["errorNumber"] = (int)ApiErrorCode.InvalidParameter;
                ex.Data["paramName"] = "imageConversionMode";
                ex.Data["paramValue"] = imageConversionMode;
                ex.Data["customizedMsg"] = ApiSettings.CONVERSIONMODE_VALID_MSG; // inform middleware to customize error message. Replace [customizedMsgHere] by this string.

                // rethrow to the middleware.
                throw;
            }
            catch (Exception ex)
            {
                // Log an error.
                _logger.LogError(LoggingEvents.GetItem, $"GET a Job: Unexpected error when retrieving a job status record.");

                // Data parameters to be passed to the handling error middleware.
                ex.Data["errorNumber"] = (int)ApiErrorCode.InternalError;
                ex.Data["paramName"] = "Not applicable";
                ex.Data["paramValue"] = "";

                // rethrow to the middleware.
                throw;
            }

        }
    }
}