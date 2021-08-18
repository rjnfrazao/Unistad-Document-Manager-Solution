
using StorageLibrary.DataTransferObjects;


namespace StorageLibrary.Library
{

    /// <summary>
    /// Classe holds the logics related to Errors in general in the API, such as messages, error codes, and responses.
    /// </summary>

    public class ErrorLibrary
    {
        /// <summary>
        /// Converts an error number inside an encoded error description, to the standard error number
        /// </summary>
        /// <param name="encodedErrorDescription">The error description</param>
        /// <returns>The decoded error number</returns>
        public static int GetErrorNumberFromDescription(string encodedErrorDescription)
        {
            if (int.TryParse(encodedErrorDescription, out int errorNumber))
            {
                return errorNumber;
            }
            return 0;
        }



        /// <summary>
        /// Converts an error number inside an encoded error description, to the standard error response
        /// </summary>
        /// <param name="encodedErrorDescription">The error description</param>
        /// <returns>The decoded error message and number</returns>
        public static (string decodedErrorMessage, int decodedErrorNumber) GetErrorMessage(string encodedErrorDescription)
        {

            int errorNumber = GetErrorNumberFromDescription(encodedErrorDescription);

            //ApiErrorCode enumErrorCode = Enum.GetName(typeof(ApiErrorCode), errorNumber);

            // ** REFACTORING : This should be improved, the Enum shouldn't be converted in the switch case.
            switch (errorNumber)
            {
                case (int)ApiErrorCode.InternalError:
                    {
                        return ($"Internal Error: {encodedErrorDescription}", errorNumber);
                    }
                case (int)ApiErrorCode.EntityAlreadyExist:
                    {
                        return ("The entity already exists. [customizedMsgHere]", errorNumber);
                    }
                case (int)ApiErrorCode.InvalidParameter:
                    {
                        return ("The parameter provided is invalid. Valid parameter values are [customizedMsgHere]", errorNumber);
                    }
                case (int)ApiErrorCode.ParameterIsRequired:
                    {
                        return ("The parameter is required.", errorNumber);
                    }
                case (int)ApiErrorCode.EntityNotFound:
                    {
                        return ("The entity could not be found.", errorNumber);
                    }
                case (int)ApiErrorCode.ParameterIsNull:
                    {
                        return ("The parameter can't be null.", errorNumber);
                    }

                // The values below are for future use.
                case (int)ApiErrorCode.ParameterTooLong:
                    {
                        return ("The parameter value is too large.", errorNumber);
                    }
                case (int)ApiErrorCode.ParameterTooShort:
                    {
                        return ("The parameter value is too short.", errorNumber);
                    }
                case (int)ApiErrorCode.InvalidContainerName:
                    {
                        return ("The container's name is invalid.", errorNumber);
                    }
                case (int)ApiErrorCode.InvalidFileName:
                    {
                        return ("The file name is invalid.", errorNumber);
                    }                   
                case (int)ApiErrorCode.FileUploadFail:
                    {
                        return ("The file upload failed", errorNumber);
                    }
                case (int)ApiErrorCode.ParameterIsEmpty:
                    {
                        return ("The parameter can't be empty", errorNumber);
                    }
                default:
                    {
                        return ($"Raw Error: {encodedErrorDescription}", errorNumber);
                    }
            }
        }



        /// <summary>
        /// Returns a <see cref="System.String" /> that represents Error Response result.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="param">Param with the invalid value.</param>
        /// <param name="value">Invalid value.</param>
        /// <param name="customizedMsg">customizedMsg if needed.</param>
        /// <returns>
        /// A <see cref="System.String" /> that represents the Erro Response.
        /// </returns>
        public static ErrorResponse GetErrorResponse(string errorMessage, string param, string value, string customizedMsg)
        {
            // initiate Error Response object.
            ErrorResponse errorResponse = new ErrorResponse();
            
            // add the values to the object
            (errorResponse.errorDescription, errorResponse.errorNumber) = ErrorLibrary.GetErrorMessage(errorMessage);
            errorResponse.parameterName = param;
            errorResponse.parameterValue = value;

            // if customized msg was provided, this must be inserted in the error description (which is a standard message).
            if (customizedMsg != null)
            {
                errorResponse.errorDescription = errorResponse.errorDescription.Replace("[customizedMsgHere]", customizedMsg);
            }

            // returns the error response.
            return (errorResponse);
            
        }

 
    }
}
