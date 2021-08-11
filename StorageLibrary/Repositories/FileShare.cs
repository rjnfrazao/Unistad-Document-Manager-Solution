using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using ConfigurationLibrary;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;


namespace StorageLibrary.Repositories
{
    public class FileShare : IFileShare
    {

        private ShareClient _shareClient;

        private ILogger _log;


        /// <summary>
        /// Contruction initialize the file share client.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="configuration"></param>
        public FileShare(ILogger log, IConfiguration configuration)
        {

            // Storage connection string
            string storageConnectionString = configuration.GetSection(ConfigSettings.STORAGE_CONNECTIONSTRING_NAME).Value;  
            //string storageConnectionString = Environment.GetEnvironmentVariable(ConfigSettings.STORAGE_CONNECTIONSTRING_NAME);

            // Create the File Share client.
            _shareClient = new ShareClient(storageConnectionString, ConfigSettings.FILE_SHARE_NAME);

            _log = log;
        }


        /// <summary>
        /// Create the file share, in case it doesn't exist.
        /// </summary>
        /// <returns>True - file share exists, otherwise returns false</returns>
        private async Task<bool> InitializeFileShare()
        {

            // Create the share if it doesn't already exist
            await _shareClient.CreateIfNotExistsAsync();

            // Ensure that the share exists
            if (await _shareClient.ExistsAsync())
            {
                return true;
            } else
            {
                _log.LogError($"File share {0} wasn't created", ConfigSettings.FILE_SHARE_NAME);
                return false;
            }
        }


    
 
        /// <summary>
        /// Returns the directory client. In case directory doesn't exist, the directory is created.
        /// </summary>
        /// <param name="directory">directory name</param>
        /// <returns>Directory client, if fails returns null.</returns>
        private async Task<ShareDirectoryClient> GetDirectoryClient(string directory)
        {

            // Assure the file share exists.
            if (await InitializeFileShare())
            {

                // Get a reference to the directory
                ShareDirectoryClient directoryClient = _shareClient.GetDirectoryClient(directory);

                // Create the directory if it doesn't already exist
                await directoryClient.CreateIfNotExistsAsync();

                // Ensure that the directory exists
                if (await directoryClient.ExistsAsync())
                {
                    // Returns directory client.
                    return directoryClient;
                } else
                {
                    _log.LogError($"Directory {0} wasn't created", directory);
                    return null;
                }

            } else
            {
                return null;
            }

        }



        /// <summary>
        /// Check if the file already exists in the directory.
        /// </summary>
        /// <param name="directory">Directory name.</param>
        /// <param name="file">File name with extension.</param>
        /// <returns>True - File exists, otherwise returns false.</returns>
        public async Task<bool> FileExists(string directory, string file)
        {

            ShareDirectoryClient directoryClient = await GetDirectoryClient(directory);

            // Assure the file share exists.
            if (directoryClient != null)
            {

                // Get a reference to a file object
                ShareFileClient fileClient = directoryClient.GetFileClient(file);

                // check if the file exists
                if (await fileClient.ExistsAsync())
                {
                    return true;

                }
                else
                {
                    return false;
                }
            } else
            {
                return false;
            }
        }



        /// <summary>
        /// Save file uploaded into the folder, if the file exists in the destination, the file will be overwritten.
        /// If you can't overwrite the file, please check before if the file exists.
        /// </summary>
        /// <param name="directory">Directory name.</param>
        /// <param name="file">File name with extension.</param>
        /// <param name="fileStream">Stream of the file uploaded.</param>
        /// <returns>True - File saved successfully, otherwise returns false.</returns>
        public async Task<bool> SaveFileUploaded(string directory, string file, Stream fileStream)
        {

            // Get a reference to the directory
            ShareDirectoryClient directoryClient = _shareClient.GetDirectoryClient(directory);

            // Check the client exists
            if (directoryClient!=null)
            {
                // Get a reference to a file object
                ShareFileClient destFileCLient = directoryClient.GetFileClient(file);

                // Start the copy operation
                await destFileCLient.UploadAsync(fileStream);

                // Ensure that the file was uploaded
                if (await destFileCLient.ExistsAsync())
                {
                    return true;
                } else
                {
                    _log.LogError($"File {file} wasn't saved into the uploaded directory : {directory}.", file, directory);
                    return false;
                }
            } else
            {
                // Directory wasn't initiated.
                return false;
            }

        }



        /// <summary>
        /// Get the file
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task<Stream> GetFile(string directory, string fileName)
        {

            if (directory.Substring(0,3) == "c:\\")
            {
                Stream fs = File.OpenRead($"{directory}\\{fileName}");
                return fs;
            }

            // Get a reference to the directory
            ShareDirectoryClient directoryClient = _shareClient.GetDirectoryClient(directory);

            // Check the client exists
            if (directoryClient != null)
            {
                // Get a reference to the file object
                ShareFileClient fileClient = directoryClient.GetFileClient(fileName);


                // Ensure that the file exists
                if (await fileClient.ExistsAsync())
                {
                    // Download the file
                    ShareFileDownloadInfo download = await fileClient.DownloadAsync();

                    return download.Content;

                    /* Save the data to a local file, overwrite if the file already exists
                    using (Stream stream = null)
                    {
                        await download.Content.CopyToAsync(stream);
                        //await stream.FlushAsync();
                        return stream;
                    }
                    */
                }
            }

            return null;

        }


    }

}


