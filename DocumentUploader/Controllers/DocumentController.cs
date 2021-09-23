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
using ConfigurationLibrary;

namespace DocumentUploader.Controllers
{
    [Route("api/v1/")]
    [ApiController]
    public class DocumentController : ControllerBase
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
        /// Initializes a new instance of the <see cref="DocumentController"/> class.
        /// </summary>
        /// <param name="storage">Storage interface.</param>
        /// <param name="queue">Queue interface.</param>
        /// <param name="fileShare">File share interface.</param>
        /// <param name="logger">Log object used to log messages.</param>
        /// <param name="configuration">Configuration object with the application settings.</param>
        public DocumentController(IStorage storage,
                                IQueue queue,
                                IFileShare fileShare,
                                ILogger<DocumentController> logger,
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
        /// <param name="user">User's id uploading the file.</param>
        /// <returns>Returns 201 if file is created, and 400 in case of BadRequest.</returns>
        // Post: tasks/
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.Created)]               //201
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]   //400
        [HttpPost]
        [Route("upload")]
        public async Task<IActionResult> Create(IFormFile fileData, [FromForm] String user)
        {
            string directory = "";
            string fileName = "";
            

            try
            {

                // Log create process started.
                _logger.LogInformation(LoggingEvents.InsertItem, $"POST : Start v1.0 - {user} uploading document.");

                // Initialize variables.

                fileName = fileData.FileName;

                // There are no FileShare in Azurite, so in development use File System to store the files.
                // Correct class is injected. Dependency Inject is constructed in Startup.CongifurationServices
                if (_configuration.GetValue<bool>($"{ConfigSettings.APP_SETTINGS_SECTION}:UseDevelopmentStorage"))
                {

                    // Add the root folder location in the file system.
                    directory = _configuration.GetValue<string>($"{ConfigSettings.APP_SETTINGS_SECTION}:DevelopmentFileSystemRoot") + ConfigSettings.FILE_SHARE_UPLOADED_FOLDER;

                }
                else
                {

                    directory = ConfigSettings.FILE_SHARE_UPLOADED_FOLDER;
                }


                // "fileData" Parameter is required 
                if (fileName is null)
                {
                    throw new ParameterIsRequired("fileName");
                }


                // "user" Parameter is required 
                if (user is null)
                {
                    throw new ParameterIsRequired("user");
                }


                // Log create process started.
                _logger.LogInformation(LoggingEvents.InsertItem, $"POST : Uploading file. {fileName}");


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
                // containerName = _storage.CleanContainerName(containerName);

                // Save to the uploaded directory
                using Stream fileStream = fileData.OpenReadStream();
                if (await _fileShare.SaveFileUploaded(directory, fileName, fileStream))
                {
                    // string imageSource = uri.ToString();
                    // Log file was created
                    _logger.LogInformation(LoggingEvents.InsertItem, $"POST : File was stored successfuly.");

                    var jobId = Guid.NewGuid().ToString(); 

                    // Add to the Queue.
                    var message = new QueueJobMessage(ConfigSettings.TABLE_PARTITION_KEY, jobId, fileName, user);
                    await _queue.AddQueueMessage(message, null);

                    // (#) Instatiate TableProcessor but inject JobTable object.
                    var tableProcessor = new TableProcessor(new JobTable(_logger, _configuration, ConfigSettings.TABLE_PARTITION_KEY));

                    // Create record job status with status Queued.
                    await tableProcessor.CreateJobTableWithStatus(_logger, jobId, fileName, user);

                    // Created successfuly. Returns created status with the location of the blob uploaded
                    return CreatedAtRoute(_retrieveJobById, new { id = jobId }, null);
                    

                }
                else
                {
                    throw new InvalidDataInput($"POST: File failed to be created in the File Share. File name : {fileName}");
                }


            }
            // ** REFACTORING is needed passing this catch to the Exception Middleware and inplementing the log in the middleware. not here.
            // Exception throw by the Azure Storage Blob
            catch (RequestFailedException ex)
                when (ex.ErrorCode == "InvalidResourceName")
            {
                // Just in case any invalid resource name was't checked previously. Such as starting with symbols or others situations.
                _logger.LogError(LoggingEvents.InsertItem, ex, $"POST : Invalid Resource Name. File : {fileName}");

                // Data parameters to be passed to the exception middleware.
                ex.Data["errorNumber"] = (int)ApiErrorCode.InvalidContainerName;
                ex.Data["paramName"] = "fileName";
                ex.Data["paramValue"] = fileName;

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
        /// Retrieve Job by Job Id (GUID).
        /// </summary>
        /// <param name="id">GUI of the job. This is generated everytime a document is uploaded.</param>
        /// <returns>Ok - Job record details stored in the Table Storage
        ///          Not Found - Job id could not be found.
        /// </returns>
        /// <remarks></remarks>    

        [ProducesResponseType(typeof(JobStatusResponse[]), (int)HttpStatusCode.OK)]     //200
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]   //400
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]     //404
        [HttpGet("job/{id}", Name = "RetrieveJobById")]

        public async Task<IActionResult> RetrieveJobById(string id)
        {
            try
            {

                _logger.LogInformation(LoggingEvents.GetItem, $"GET the job {id}.");


                // ** REFACTORING is needed in case conversion mode is removed from the constructor.
                var jobTable = new JobTable(_logger, _configuration, ConfigSettings.TABLE_PARTITION_KEY);

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
                ex.Data["paramName"] = "job";
                ex.Data["paramValue"] = id;
                ex.Data["customizedMsg"] = ApiSettings.INVALID_DATA_INPUT_MSG; // inform middleware to customize error message. Replace [customizedMsgHere] by this string.

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


        /// <summary>
        /// Workarount to implement using Swagger the optional routing parameter. 
        /// </summary>
       /// <returns> Ok - List of all jobs .
        ///          Empty - No 
        ///          BadRequest - Container not found or any other internal error.
        /// </returns>
        /// <remarks></remarks>    

        [ProducesResponseType(typeof(JobStatusResponse[]), (int)HttpStatusCode.OK)]     //200
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]     //404
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]   //400
        [HttpGet("jobs/")]

        public async Task<IActionResult> RetrieveAllJobs()
        {
            return await RetrieveAllJobs("");
        }


        /// <summary>
        /// Returns all Jobs in the storage table.
        /// </summary>
        /// <param name="userName">If user name is provided returns jobs uploaded by the user, if blank returns all jobs.</param>
        /// <returns>Ok - List of jobs .
        ///          Empty - No 
        ///          BadRequest - Container not found or any other internal error.
        /// </returns>
        /// <remarks></remarks>    

        [ProducesResponseType(typeof(JobStatusResponse[]), (int)HttpStatusCode.OK)]     //200
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]     //404
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]   //400
        [HttpGet("jobs/{userName}")]

        public async Task<IActionResult> RetrieveAllJobs(string userName = "")
        {
            try
            {
                _logger.LogInformation(LoggingEvents.GetItem, $"GET all jobs by user {userName}. Storage table {ConfigSettings.TABLE_JOBS_NAME}.");

                // Initiate a list of JobStatusResponse
                var jobStatusList = new List<JobStatusResponse>();
                var jobStatus = new JobStatusResponse(new JobEntity());

                // ** REFACTORING is needed -> Notice mode added, but we will return all records.
                var jobTable = new JobTable(_logger, _configuration, ConfigSettings.TABLE_PARTITION_KEY);

                foreach (JobEntity entity in await jobTable.RetrieveJobEntityAll(userName))
                {
                    jobStatus = new JobStatusResponse(entity);

                    jobStatusList.Add(jobStatus);
                }

                // In case records weren't found, returns entity not found error.
                if (jobStatusList.Count == 0) return new NotFoundObjectResult(ErrorLibrary.GetErrorResponse(((int)ApiErrorCode.EntityNotFound).ToString(), "user", userName, null));

                return new ObjectResult(jobStatusList);

            }
            catch (Exception ex)
            {
                // Log an error.
                _logger.LogError(LoggingEvents.GetItem, $"[+] Unexpected error when retrieving job status records. Storage table {ConfigSettings.TABLE_JOBS_NAME}.");

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