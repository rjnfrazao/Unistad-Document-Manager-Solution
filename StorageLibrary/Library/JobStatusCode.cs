
using System.Collections.Generic;

namespace StorageLibrary.Library
{

    /// <summary>
    /// Enum. Defines the list of job statuses.
    /// </summary>
    public enum EnumJobStatusCode
    {
        Queued = 1,
        Running = 2,
        Converted = 3,
        Failed = 4
    }


    public class JobStatusCode {

        static readonly Dictionary<int, string> CodeMap = new Dictionary<int, string>
        {
            { 1, "Image Queued." },
            { 2, "Job is running." },
            { 3, "Job completed with success." },
            { 4, "Job Failed ." }
        };

        public static string GetStatus(int code)
        {
            string name;
            if (!CodeMap.TryGetValue(code, out name))
            {
                // Error handling here
            }
            return name;
        }    
        
    }

}
