using StorageLibrary.Library;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace StorageLibrary.Repositories
{

    /// <summary>
    /// Implements the interface for the Storage.
    /// </summary>
    public interface IStorage
    {
        Task DeleteFile(string containerName, string fileName);
        Task<(MemoryStream fileStream, string contentType)> GetFileAsync(string containerName, string fileName);
        Task<byte[]> GetFileInByteArrayAsync(string containerName, string fileName);
        Task<List<string>> GetListOfBlobs(string containerName);
        Task<(HttpStatusCode, Uri)> UploadFile(string containerName, string fileName, Stream fileStream, string contentType);
        public string CleanContainerName(string containerName);
        public ObjectResult ValidateData(string containerName);
        public ObjectResult ValidateData(string containerName, string fileName);
        public ObjectResult ValidateData(string containerName, string fileName, IFormFile fileData);
        public ApiErrorCode ValidateFileName(string fileName);
        public bool ContainerExist(string containerName);
        public bool FileExist(string containerName, string fileName);
        public ObjectResult ValidateExistance(string containerName);
        public ObjectResult ValidateExistance(string containerName, string fileName);

    }
}
