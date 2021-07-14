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

        Task<bool> FileExists(string directory, string file);

        Task<bool> SaveFileUploaded(string directory, string file, Stream fileStream);

    }
}
