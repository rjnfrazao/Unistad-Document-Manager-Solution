using Microsoft.Azure.Cosmos.Table;
using System.ComponentModel.DataAnnotations;


namespace StorageLibrary.Models
{
    public class JobEntity : TableEntity
    {
        // 1 : File Uploaded / 2 : Job is running / 3 : Job Completed with Success / 4 : Job Failed
        public int status { get; set; }

        [MaxLength(512)]
        public string statusDescription { get; set; }

        [MaxLength(512)]
        public string fileSource { get; set; }

        [MaxLength(512)]
        public string fileResult { get; set; }

    }
}
