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
                Stream fs = File.OpenRead($"{directory}\\{file}");
                return true;
            }
            catch (FileNotFoundException)
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

                _log.LogInformation($"File {file} was saved into the uploaded directory : {directory}.", file, directory);
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

            } catch (Exception)
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
                File.Delete($"{directory}\\{fileName}");
            }
            catch (Exception)
            {
                _log.LogError($"Error deleting the file {fileName} at the {directory}.", fileName, directory);
            }
        }

    }

}


