
namespace ConfigurationLibrary
{

    /// <summary>
    /// Global settings where some contants are defined and used across the application.
    /// </summary>
    public static class ConfigSettings
    {

        // ----- Storage Variables -----

        // Name of the storage connection string configuration
        public const string STORAGE_CONNECTIONSTRING_NAME = "DefaultStorageConnection";

        // Container used to store uploaded images
        // * REMOVE WE DON'T USE BLOBS public const string UPLOADED_CONTAINERNAME = "unistad";



        // ----- Storage Queue Variables -----

        // Name of the queue connection string configuration
        public const string QUEUE_CONNECTIONSTRING_NAME = "DefaultStorageConnection"; 

        // Name of the queue connection string configuration
        public const string QUEUE_TOPROCESS_NAME = "unistad-toprocess";



        // ----- Table Storage Variables -----

        // Name of the table used to store the job status
        public const string TABLE_JOBS_NAME = "unistadjobs";

        // Name of the table used to store the job status
        public const string TABLE_PATITION_KEY = "unistad";



        // ----- File share Variables -----

        public const string FILE_SHARE_FOLDER_DELIMITER = "\\";

        // Name of the table used to store the job status
        public const string FILE_SHARE_NAME = "unistad-files";

        // Uploaded folder
        public const string FILE_SHARE_UPLOADED_FOLDER = "_jobs_uploaded";

        // Failed folder
        public const string FILE_SHARE_FAILED_FOLDER = "_jobs_failed";

        // Root folder to store the files processed.
        public const string FILE_SHARE_UNISTAD_FOLDER = "unistad";



        // Section in appsettings where the application configuration is stored.
        public const string APP_SETTINGS_SECTION = "ApplicationSettings";




    }
}
