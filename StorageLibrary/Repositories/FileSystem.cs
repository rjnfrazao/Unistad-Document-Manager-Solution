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
    public class FileSystem : IFileShare
    {

        private ILogger _log;


        /// <summary>
        /// Contruction initialize the file share client.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="configuration"></param>
        public FileSystem(ILogger log, IConfiguration configuration)
        {
            _log = log;
        }


        /// <summary>
        /// Directory exists or not.
        /// </summary>
        /// <param name="directory">Directory name.</param>
        /// <returns>True - Directory exists, otherwise returns false.</returns>
        public bool DirectoryExists(string directory)
        {
            if (Directory.Exists(directory))
            {
                return true;
            }
            else
            {
                return false;
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
            // Avoid warning message
            await Task.FromResult(0);

            try
            {
                // If directory doesn't exist, file doesn't exist.
                if (!this.DirectoryExists(directory)) return false;

                if (File.Exists($"{directory}{file}")) return true;

                return false;

                //Stream fs = File.OpenRead($"{directory}\\{file}");
                //return true;
            }
            catch (FileNotFoundException)
            { 
                return false;
            }
        }



        /// <summary>
        /// Move the 
        /// Source file must exist, destination file can't exist, otherwise an error is logged.
        /// </summary>
        /// <param name="sourceFile">File to be moved.</param>
        /// <param name="destinationFile">Destination folder and file name.</param>
        /// <returns>True - File moved successfully, otherwise returns false.</returns>
        public async Task<bool> MoveFileUploaded(string sourceFile, string destinationFile)
        {
            // Avoid warning message
            await Task.FromResult(0);

            try
            {
                FileInfo info = new FileInfo(sourceFile);

                // Source file must exist.
                if (!info.Exists)
                {
                    _log.LogError($"Moving file failed. File {sourceFile} doesn't exist. [Error: 251]");
                    return false;
                }

                // Destination file can't be overwritten.
                info = new FileInfo(destinationFile);
                if (info.Exists)
                {
                    _log.LogError($"Moving file failed. Destination file {destinationFile} already exist. [Error: 252]");
                    return false;
                }

                File.Move(sourceFile, destinationFile);

                _log.LogInformation($"File {sourceFile} was moved to {destinationFile} .");

                return true;

            }
            catch (Exception e)
            {
                _log.LogError($"Unknown error happen moving the file {sourceFile} to {destinationFile} . [Error: 253]. Exception : {e.Message}. ");
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
            // Avoid warning message
            await Task.FromResult(0);

            try
            {
                DirectoryInfo info = new DirectoryInfo(directory);
                if (!info.Exists)
                {
                    info.Create();
                }

                string path = Path.Combine(directory, file);
                using (FileStream outputFileStream = new FileStream(path, FileMode.Create))
                {
                    fileStream.CopyTo(outputFileStream);
                }

                _log.LogInformation($"File {file} was saved into the directory : {directory}.", file, directory);
                return true;

            } catch (Exception)
            {
                _log.LogError($"File {file} wasn't saved into the uploaded directory : {directory}.", file, directory);
                return false;
            }


        }



        /// <summary>
        /// Get the file
        /// </summary>
        /// <param name="directory">Directory</param>
        /// <param name="file">file name</param>
        /// <returns></returns>
        public async Task<Stream> GetFile(string directory, string fileName)
        {
            // Avoid warning message
            await Task.FromResult(0);

            try
            {
                DirectoryInfo info = new DirectoryInfo(directory);
                if (!info.Exists)
                {
                    return null;
                }

                FileInfo fileInfo = new FileInfo($"{directory}\\{fileName}");

                if (!fileInfo.Exists)
                {
                    return null;
                }
                else
                {
                    Stream fs = File.OpenRead($"{directory}\\{fileName}");
                    
                    return fs;
                    
                        
                }

            } 
            catch (Exception)
            {
                _log.LogError($"Error opening the file {fileName} wasn't found in the {directory}.", fileName, directory);
                return null;
            } 

        }


        /// <summary>
        /// Delete the file
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task DeleteFile(string directory, string fileName)
        {
            // Avoid warning message
            await Task.FromResult(0);

            try
            {
                if (File.Exists($"{directory}{fileName}"))
                {
                    File.Delete($"{directory}{fileName}");
                    _log.LogInformation($"File {fileName} deleted from {directory} .");
                }
                else
                {
                    throw new Exception();
                }
                
            }
            catch (Exception)
            {
                _log.LogError($"Error deleting the file {fileName} at the {directory}. [Error:251 ] ");
                throw; 
            }
        }

    }

}


