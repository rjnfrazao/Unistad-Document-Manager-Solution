

namespace StorageLibrary.Library
{
    /// <summary>
    /// Enum. Defines the list of errors.
    /// </summary>
    public enum ApiErrorCode
        {
            NoError = 0,
            EntityAlreadyExist = 1,
            InvalidParameter = 2,
            ParameterIsRequired = 3,
            EntityNotFound = 4,
            ParameterIsNull=5,
            InternalError,

        // Error code below are not used yet. Planned for future.
            ParameterTooLong ,


            ParameterTooShort,
            
            InvalidContainerName,
            InvalidFileName,
            FileUploadFail,
            ParameterIsEmpty
           
    }

}
