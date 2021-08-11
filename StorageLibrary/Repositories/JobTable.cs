using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Documents;
using System;
using System.Threading.Tasks;
using StorageLibrary.Models;
using ConfigurationLibrary;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using ConfigurationLibrary;
using StorageLibrary.Library;

namespace StorageLibrary.Repositories
{
    public class JobTable : IJobTable
    {
        private CloudTableClient _tableClient;
        private CloudTable _table;
        private string _partitionKey { get; set; }

        private ILogger _log;

        /// <summary>
        /// Classes responsible for all operations of read, add, and updated records to the storage table.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="configuration"></param>
        /// <param name="partitionKey"></param>
        public JobTable(ILogger log, IConfiguration configuration, string partitionKey)
        {

            string storageConnectionString = configuration.GetConnectionString(ConfigSettings.STORAGE_CONNECTIONSTRING_NAME);  //Environment.GetEnvironmentVariable(ConfigSettings.STORAGE_CONNECTIONSTRING_NAME);
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
 
            // Create the table client.
            _tableClient = storageAccount.CreateCloudTableClient();

            // Create the CloudTable object that represents the "jobentity" table.
            _table = _tableClient.GetTableReference(ConfigSettings.TABLE_JOBS_NAME);

            _table.CreateIfNotExistsAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            _partitionKey = partitionKey;

            _log = log;
        }


        /// <summary>
        /// Retrieves the job entity.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <returns>JobEntity.</returns>
        public async Task<JobEntity> RetrieveJobEntity(string jobId)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<JobEntity>(_partitionKey, jobId);
            TableResult retrievedResult = await _table.ExecuteAsync(retrieveOperation);

            return retrievedResult.Result as JobEntity;
        }


        /// <summary>
        /// Retrieves the job entity based on the filter handler and blob name, as the same file can be used for different conversions.
        /// </summary>
        /// <param name="filterHandler">Filter mode applied.</param>
        /// <param name="name">File name.</param>
        /// <returns>JobEntity.</returns>
        public async Task<JobEntity> RetrieveJobEntityByName(string filterHandler, string name)
        {
            var returnList = new List<JobEntity>();

            // return the records filtered by conversion mode.
            var entityList = await this.RetrieveJobEntityByMode(filterHandler);

            // ** Detailed log for debug.
            //_log.LogInformation($"[+++] RetrieveJobEntityByName - returned a list with {entityList.Count} item(s).");

            // Loop the records to filter based on the criteria required.
            foreach (JobEntity entity in entityList)
            {
                // Criteria is file successfully converted, contains the file name, and not yet deleted.
                if ((entity.status==(int)EnumJobStatusCode.Converted) &&
                    (entity.fileSource.Contains(name)))
                {
                    // ** Detailed log for debug.
                    //_log.LogInformation($"[+++] RetrieveJobEntityByName - Found a match with the following table entity {entityList}.");

                    returnList.Add(entity);
                }
            }

            /* REMOVE IT / ideally would return just one record, in case not throw an exception
            if (returnList.Count!=1)
            {
                // ** Detailed log for debug.
                //_log.LogInformation($"[+++] RetrieveJobEntityByName - Found the following quantity of matches {returnList.Count} while just one was expected.");

                return null;
            }*/

            return returnList[0];
        }


        /// <summary>
        /// Retrieve the list of Job the job entity based on the filter handler
        /// </summary>
        /// <param name="filterHandler">Filter mode to be filtered.</param>
        /// <returns>JobEntity.</returns>
        public async Task<List<JobEntity>> RetrieveJobEntityByMode(string filterHandler)
        {

            var entityList = new List<JobEntity>();

            // Construct the query operation for PartitionKey=_partitionKey.
            TableQuery<JobEntity> query = new TableQuery<JobEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, _partitionKey));

            
            TableContinuationToken token = new TableContinuationToken();

            // loop the records returned to filter based on the conversion mode defined.
            foreach (JobEntity entity in await _table.ExecuteQuerySegmentedAsync(query, null))
            {
                // filter by conversion mode.
                if (entity.PartitionKey==filterHandler)
                {
                    entityList.Add(entity);
                }               
            }

            return entityList;
        }


        /// <summary>
        /// Retrieve all records
        /// </summary>
        /// <returns>List of JobEntity.</returns>
        public async Task<List<JobEntity>> RetrieveJobEntityAll()
        {

            var entityList = new List<JobEntity>();

            // Construct the query operation for PartitionKey=_partitionKey.
            TableQuery<JobEntity> query = new TableQuery<JobEntity>();

            //TableContinuationToken token = new TableContinuationToken();

            // loop the records returned to filter based on the conversion mode defined.
            foreach (JobEntity entity in await _table.ExecuteQuerySegmentedAsync(query, null))
            {
                    entityList.Add(entity);
            }

            return entityList;
        }


        /// <summary>
        /// Updates the job entity.
        /// </summary>
        /// <param name="jobEntity">The job entity.</param>
        public async Task<bool> UpdateJobEntity(JobEntity jobEntity)
        {
            TableOperation replaceOperation = TableOperation.Replace(jobEntity);
            TableResult result = await _table.ExecuteAsync(replaceOperation);

            if (result.HttpStatusCode >199 && result.HttpStatusCode < 300)
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// Updates the job entity deleted field to True. This means this blob was deleted from the source container.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        public async Task UpdateJobEntityToDeleted(string jobId)
        {
            JobEntity jobEntityToReplace = await RetrieveJobEntity(jobId);
            if (jobEntityToReplace != null)
            {

                // Update record
                await UpdateJobEntity(jobEntityToReplace);
            }
        }


        /// <summary>
        /// Updates the job entity status.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <param name="status">The status.</param>
        /// <param name="statusDescription">Desciption to be updated, in case not provided, it's used the pre defined description, according the status.</param>
        /// <param name="fileResult">Image resulted after the conversion. (Image successfuly converted or failed one).</param>
        public async Task UpdateJobEntityStatus(string jobId, int status, string statusDescription, string fileResult)
        {
            JobEntity jobEntityToReplace = await RetrieveJobEntity(jobId);
            if (jobEntityToReplace != null)
            {
                // in case status description is not provided, use the predefined description of the status
                if (statusDescription=="") statusDescription = JobStatusCode.GetStatus(status);  
                
                // assign new values.
                jobEntityToReplace.status = status;
                jobEntityToReplace.statusDescription = statusDescription;
                jobEntityToReplace.fileResult = fileResult;
                
                // Update record
                await UpdateJobEntity(jobEntityToReplace);
            }
        }


        /// <summary>
        /// Inserts the or replace job entity.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <param name="status">The status.</param>
        /// <param name="fileSource">File Source.</param>
        public async Task InsertOrReplaceJobEntity(string jobId, int status, string fileSource)
        {

            // Get the desciptive message
            string message = JobStatusCode.GetStatus(status);
            
            JobEntity jobEntityToInsertOrReplace = new JobEntity();
            jobEntityToInsertOrReplace.RowKey = jobId;
            jobEntityToInsertOrReplace.PartitionKey = _partitionKey;        // Partition key defined when object is instatiated.
            jobEntityToInsertOrReplace.status = status;
            jobEntityToInsertOrReplace.statusDescription = message;
            jobEntityToInsertOrReplace.fileSource = fileSource;


            TableOperation insertReplaceOperation = TableOperation.InsertOrReplace(jobEntityToInsertOrReplace);
            TableResult result = await _table.ExecuteAsync(insertReplaceOperation);

        }
    }
}
