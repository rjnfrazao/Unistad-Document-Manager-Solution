using StorageLibrary.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StorageLibrary.Repositories
{

    /// <summary>
    /// Implements the interface for the Queue.
    /// </summary>
    public interface IQueue
    {
        Task AddQueueMessage(QueueJobMessage imageProcessMessage, int? timeToLiveInSeconds);
    }
}
