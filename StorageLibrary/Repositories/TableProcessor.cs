using ConfigurationLibrary;
using StorageLibrary.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using StorageLibrary.Library;

namespace StorageLibrary.Repositories
{
    public class TableProcessor
    {

        public TableProcessor(IJobTable jobTable)
        {
            _jobTable = jobTable;
        }


        public IJobTable _jobTable { get; set; }

        /// <summary>
        /// Created the new job record into the table.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="jobId">The job identifier (GUI).</param>
        /// <param name="fileName">Image source to be converted.</param>
        /// <param name="userName">User who is uploading the file.</param>
        public async Task CreateJobTableWithStatus(ILogger log, string jobId, string fileName, string userName)
        {
            // The initial status "Uploaded"
            EnumJobStatusCode status = EnumJobStatusCode.Queued;

            log.LogInformation($"[+] Creating Job status. Job Id {jobId} - {status} - {JobStatusCode.GetStatus((int)status)}. File {fileName}");
            
           
            // Insert or replace record into the table.
            await _jobTable.InsertOrReplaceJobEntity(jobId, (int)status, fileName, userName);
            
        }


        /// <summary>
        /// Updates the job record table with status.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="jobId">The job identifier (GUI).</param>
        /// <param name="status">The status ID.</param>
        /// <param name="statusDescription">In case of failure. The status description is passed here.</param>
        /// <param name="fileResult">Image resulted after the conversion.</param>
        public async Task UpdateJobTableWithStatus(ILogger log, string jobId, int status, string statusDescription, string fileResult)
        {
            log.LogInformation($"[+] Updating Job status. Job Id {jobId} - {status} - {JobStatusCode.GetStatus(status)}");
            
            // Initiate the JobTable
            //JobTable jobTable = new JobTable(log, mode.ToString());

            // Update record into the table.
            await _jobTable.UpdateJobEntityStatus(jobId, (int)status, statusDescription, fileResult);


        }


        /// <summary>
        /// Retrieve Job Entity by Id.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="name">Document name.</param>
        public async Task<JobEntity> RetrieveJobEntityByName(ILogger log, string name)
        {
            log.LogInformation($"[+] Retrieving JobStatus Record By Name {name}");

            // Initiate the JobTable
            // JobTable jobTable = new JobTable(log, ConfigSettings.IMAGEJOBS_PARTITIONKEY);

            // Update record into the table.
            return await _jobTable.RetrieveJobEntity(name);

        }

    }
}
