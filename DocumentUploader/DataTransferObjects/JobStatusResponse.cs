
using StorageLibrary.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DocumentUploader.DataTransferObjects
{
    public class JobStatusResponse 
    {

        public JobStatusResponse(JobEntity entity)
        {
            //RowKey = entity.RowKey;
            jobId = entity.RowKey;
            partition = entity.PartitionKey;
            status = entity.status;
            statusDescription = entity.statusDescription;
            fileSource = entity.fileSource;
            fileResult = entity.fileResult;

        }


        public string jobId { get; set; }

        public string partition { get; set; }

        public int status { get; set; }

        [MaxLength(512)]
        public string statusDescription { get; set; }

        [MaxLength(512)]
        public string fileSource { get; set; }

        [MaxLength(512)]
        public string fileResult { get; set; }

    }
}
