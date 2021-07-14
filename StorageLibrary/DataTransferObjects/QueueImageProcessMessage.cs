using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StorageLibrary.DataTransferObjects
{
    public class QueueImageProcessMessage
    {
        /// <summary>
        /// Name of the container.
        /// </summary>
        /// <value>Name of the container.</value>
        public string containerName { get; set; }

        /// <summary>
        /// Name of the file stored in the container.
        /// </summary>
        /// <value>GUID generated to the file uploaded.</value>
        public string fileName { get; set; }

        /// <summary>
        /// Image conversion mode.
        /// </summary>
        /// <value>Image conversion mode.</value>
        public int imageConversionMode{ get; set; }

        public QueueImageProcessMessage (string container, string file, int mode)
        {
            containerName = container;
            fileName = file;
            imageConversionMode = mode;

        }
    }
}
