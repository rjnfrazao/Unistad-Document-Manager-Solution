using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageLibrary.DataTransferObjects
{
    /// <summary>
    /// Object contains the stucture of the Queue message.
    /// </summary>
    public class QueueJobMessage
    {

        /// <summary>
        /// Name of the partition of the table storage.
        /// </summary>
        /// <value>The value will be a constant defined in the configuration settings.</value>
        public string partitionName { get; set; }

        /// <summary>
        /// Row key of the table storage.
        /// </summary>
        /// <value>Job id. This is a Guid.</value>
        public string jobId { get; set; }


        /// <summary>
        /// Name of the file upoaded to be processed.
        /// </summary>
        /// <value>Original fila name uploaded.</value>
        public string fileName { get; set; }


        public QueueJobMessage(string partition, string job, string file)
        {
            partitionName = partition;
            jobId = job;
            fileName = file;

        }

    }
}
