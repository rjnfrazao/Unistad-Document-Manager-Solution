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
            string storageConnectionString = configuration.GetConnectionString(ConfigSettings.STORAGE_CONNECTIONSTRING_NAME).ToString();
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
            }
            else
            {
                _log.LogError($"File share {0} wasn't created", ConfigSettings.FILE_SHARE_NAME);
                return false;
            }
        }



        private async Task CreateRecursiveIfNotExists(ShareDirectoryClient directory)
        {

            string GetParentDirectory(string path)
            {
                string parent = "";
                string add_delimiter = "";

                //var delimiter = new char[] { ConfigSettings.FILE_SHARE_FOLDER_DELIMITER. };
                var nestedFolderArray = path.Split(ConfigSettings.FILE_SHARE_FOLDER_DELIMITER);
                for (var i = 0; i < nestedFolderArray.Length-1; i++)
                {
                    if (i > 0)  add_delimiter = ConfigSettings.FILE_SHARE_FOLDER_DELIMITER; // was delimiter
                    
                    parent = parent + add_delimiter + nestedFolderArray[i];
                }
 
                return parent;
            }

            if (! await directory.ExistsAsync())
            {
                string parentDir = GetParentDirectory(directory.Path);

                await CreateRecursiveIfNotExists(_shareClient.GetDirectoryClient(parentDir));
                await directory.CreateAsync();
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
                //await directoryClient.CreateIfNotExistsAsync();
                await CreateRecursiveIfNotExists(directoryClient);

                // Ensure that the directory exists
                if (await directoryClient.ExistsAsync())
                {
                    // Returns directory client.
                    return directoryClient;
                }
                else
                {

                    _log.LogError($"Directory {0} wasn't created", directory);
                    return null;
                }

            }
            else
            {
                return null;
            }

        }



        /// <summary>
        /// Directory exists or not.
        /// </summary>
        /// <param name="directory">Directory name.</param>
        /// <returns>True - Directory exists, otherwise returns false.</returns>
        public bool DirectoryExists(string directory)
        {
            // TO BE IMPLEMENTED IN FUTURE.
            throw new Exception("DirecotryExits method pending to be implemented in FileShare Class.");
        }


        /// <summary>
        /// Check if the file already exists in the directory.
        /// </summary>
        /// <param name="directory">Directory name.</param>
        /// <param name="file">File name with extension.</param>
        /// <returns>True - File exists, otherwise returns false.</returns>
        public async Task<bool> FileExists(string directory, string file)
        {
            try
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
                }
                else
                {
                    return false;
                }

            } catch (Exception)
            {
                return false;
            }
        }



        /// <summary>
        /// Move file share
        /// </summary>
        /// <param name="source">Source folder and file .</param>
        /// <param name="destination">Destination folder and file name. </param>
        /// <returns>True means the file was moved, otherwise returns false.</returns>
        public async Task<bool> MoveFileUploaded(string source, string destination)
        {

            string sourceFile = "";
            string sourceFolder = "";
            string destinationFile = "";
            string destinationFolder = "";


            // Work out the file names and folder names.
            sourceFile = Path.GetFileName(source);
            sourceFolder = Path.GetDirectoryName(source);
            destinationFile = Path.GetFileName(destination);
            destinationFolder = Path.GetDirectoryName(destination);


            // Get a reference to the source directory
            ShareDirectoryClient sourceFolderClient = await GetDirectoryClient(sourceFolder);

            // Check the source folder exists
            if (sourceFolderClient == null)
            {
                // LOG ERROR source folder doesn't exist

                return false;
            } 

            // Get a reference to the source file 
            ShareFileClient sourceFileClient = sourceFolderClient.GetFileClient(sourceFile);

            // Ensure that the source file exists
            if (!await sourceFileClient.ExistsAsync())
            {
                // LOG ERROR source file doesn't exist

                return false;
            }


            // Get a reference to the destination folder, if folder doesn't exist create it.
            ShareDirectoryClient destinationFolderClient = await GetDirectoryClient(destinationFolder);

            // Creates if doesn't exist.
            // await destinationFolderClient.CreateIfNotExistsAsync();

            // Get a reference to the destination file 
            ShareFileClient destinationFileClient = destinationFolderClient.GetFileClient(destinationFile);

            if (await destinationFileClient.ExistsAsync())
            {
                // LOG ERROR destination folder can't be overwritten.

                return false;
            }

            // Start the copy operation
            await destinationFileClient.StartCopyAsync(sourceFileClient.Uri);

            // Ensure that the file was uploaded
            if (await destinationFileClient.ExistsAsync())
            {
                await sourceFileClient.DeleteAsync();
                _log.LogInformation($"File {sourceFolder}{sourceFile} moved to : {destinationFolder}{destinationFile}.");

                return true;
            }
            else
            {
                _log.LogError($"File {sourceFolder}{sourceFile} wasn't moved to  {destinationFolder}{destinationFile}.");
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
            ShareDirectoryClient directoryClient = await GetDirectoryClient(directory);

            // Check the client exists
            if (directoryClient != null)
            {
                // Get a reference to a file object
                ShareFileClient destFileCLient = directoryClient.GetFileClient(file);

                // if the file doesn't exist. Create one before the upload.
                if (! await destFileCLient.ExistsAsync())
                {
                    //Create an empty file if the file doesn't exist.
                    await destFileCLient.CreateAsync(fileStream.Length);
                }

                fileStream.Position = 0;

                // Start the copy operation
                await destFileCLient.UploadAsync(fileStream);
                

                // Ensure that the file was uploaded
                if (await destFileCLient.ExistsAsync())
                {
                    return true;
                }
                else
                {
                    _log.LogError($"File {file} wasn't saved into the uploaded directory : {directory}.", file, directory);
                    return false;
                }
            }
            else
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

            if (directory.Substring(0, 3) == "c:\\")
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

                    Stream stream = new MemoryStream();
                    
                    // Download the file
                    ShareFileDownloadInfo download = await fileClient.DownloadAsync();

                    await download.Content.CopyToAsync(stream);

                    stream.Position = 0;

                    return stream;
                    

                }
            }

            return null;

        }


        /// <summary>
        /// Get the file
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task DeleteFile(string directory, string fileName)
        {

            // Avoid warning message
            await Task.FromResult(0);

        }

    }
}


