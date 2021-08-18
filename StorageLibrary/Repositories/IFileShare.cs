using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageLibrary.Repositories
{
    public interface IFileShare 
    {

        public bool DirectoryExists(string directory);

        Task<bool> FileExists(string directory, string file);

        Task<bool> MoveFileUploaded(string sourceFile, string destinationFile);

        Task<bool> SaveFileUploaded(string directory, string file, Stream fileStream);

        Task<Stream> GetFile(string directory, string fileName);

        Task DeleteFile(string directory, string fileName);
    }
}
