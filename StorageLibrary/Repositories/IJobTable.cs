using StorageLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StorageLibrary.Repositories
{
    /// <summary>
    /// Implements the interface to interact with the Job Table.
    /// </summary>
    public interface IJobTable
    {
        Task<JobEntity> RetrieveJobEntity(string jobId);
        Task<JobEntity> RetrieveJobEntityByName(string filterHandler, string name);
        Task<List<JobEntity>> RetrieveJobEntityByMode(string filterHandler);
        Task<List<JobEntity>> RetrieveJobEntityAll();
        Task<bool> UpdateJobEntity(JobEntity jobEntity);
        Task UpdateJobEntityStatus(string jobId, int status, string statusDescription, string fileResult);
        Task InsertOrReplaceJobEntity(string jobId, int status, string fileSource, string user);

    }
}
