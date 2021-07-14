namespace StorageLibrary.DataTransferObjects
{
    
     /// <summary>
     /// POCO class which holds structure for the Error Response.
     /// </summary>
     public class ErrorResponse
    {


        /// <summary>
        /// Number of the error.
        /// </summary>
        /// <value>The error number.</value>
        public int errorNumber { get; set; }

        /// <summary>
        /// Parameter which caused the error.
        /// </summary>
        /// <value>Parameter name.</value>
        public string parameterName { get; set; }

        /// <summary>
        /// Value that caused the error.
        /// </summary>
        /// <value>Parameter value.</value>
        public string parameterValue { get; set; }

        /// <summary>
        /// Error description.
        /// </summary>
        /// <value>Error description.</value>
        public string errorDescription { get; set; }



    }
}
