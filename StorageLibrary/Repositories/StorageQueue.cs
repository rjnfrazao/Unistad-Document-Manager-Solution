using StorageLibrary.DataTransferObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace StorageLibrary.Repositories
{
    public class StorageQueue : IQueue
    {


        /// <summary>
        /// Gets or sets the interface for configuration.
        /// </summary>
        private IConfiguration _configuration { get; set; }

        /// <summary>
        /// Gets or sets the interface for Logger.
        /// </summary>
        private ILogger _logger { get; set; }


        public StorageQueue(ILogger<StorageQueue> logger, IConfiguration configuration)
        {
            _logger = logger;

            _configuration = configuration;

        }

        /// <summary>
        /// Adds the queue message.
        /// </summary>
        /// <param name="jobMessage">Json message to be added to the queue.</param>
        /// <param name="timeToLiveInSeconds">The time to live in seconds.</param>
        /// <returns></returns>
        public async Task AddQueueMessage(QueueJobMessage jobMessage, int? timeToLiveInSeconds)
        {
            QueueClient queue = await GetCloudQueue();

            // Create a message and add it to the queue.
            string message = JsonSerializer.Serialize(jobMessage);

            var timeToLiveInConfiguration = Int16.Parse(_configuration.GetSection($"{ConfigSettings.APP_SETTINGS_SECTION}:DefaultTimeToLiveInSeconds").ToString());

            await queue.SendMessageAsync(message, default, TimeSpan.FromSeconds(timeToLiveInSeconds ?? timeToLiveInConfiguration), default);
        }

        /// <summary>
        /// Gets the cloud queue.
        /// </summary>
        /// <returns>The Queue client.</returns>
        private async Task<QueueClient> GetCloudQueue()
        {
            // Retrieve storage account from connection string.
            // * CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_configuration.GetConnectionString(ConfigSettings.QUEUE_CONNECTIONSTRING_NAME));

            // Create the queue client.
            // * CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a queue.
            // * CloudQueue queue = queueClient.GetQueueReference(ConfigSettings.QUEUE_TOPROCESS_NAME);

            // Connection String
            var connectionString = _configuration.GetConnectionString(ConfigSettings.QUEUE_CONNECTIONSTRING_NAME);

            // Get the Client for the queue
            QueueClient queueClient = new QueueClient(connectionString, ConfigSettings.QUEUE_TOPROCESS_NAME);

            // Create the queue if it doesn't already exist.
            await queueClient.CreateIfNotExistsAsync();

            return queueClient;

        }

    }
}
