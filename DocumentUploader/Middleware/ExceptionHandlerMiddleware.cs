using Microsoft.AspNetCore.Http;     
using System;       
using System.Net;    
using System.Threading.Tasks;  
using StorageLibrary.Library;
using StorageLibrary.DataTransferObjects;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;
using DocumentUploader.Exceptions;

namespace DocumentUploader.Middleware    
{

    /// <summary>
    /// Define the custom global error handler at the middleware layer.
    /// </summary>
    public class ExceptionHandlerMiddleware    
    {    
        private readonly RequestDelegate _next;

        /// <summary>
        /// Injects the .
        /// </summary>
        public ExceptionHandlerMiddleware(RequestDelegate next)    
        {    
            _next = next;
        }

        /// <summary>
        /// Invoke the next middleware in the chanin, in case of a failure happens the catch starts to handle the exception.
        /// </summary>
        public async Task Invoke(HttpContext context)    
        {    
            try    
            {    
                await _next.Invoke(context);    
            }    
            catch (Exception ex)    
            {    
                await HandleExceptionMessageAsync(context, ex).ConfigureAwait(false);       
            }    
        }


        /// <summary>
        /// Handle the exception, via returning the error response with appropriated content to the client..
        /// </summary>
        private Task HandleExceptionMessageAsync(HttpContext context, Exception exception)  
        {
            ErrorResponse result;
            
            context.Response.ContentType = "application/json";  
            int statusCode = (int)HttpStatusCode.BadRequest;  

            
            // If additional data was passes, this is a custom error message;
            if (exception.Data.Count!=0) 
            {
                // Get the additional data passed;
                var data = exception.Data;
                
                // Get the Erro Response JSON to be returned.
                if (exception is InvalidDataInput)
                {
                    // This exception type requires to add a customized msg
                    result = ErrorLibrary.GetErrorResponse(data["errorNumber"].ToString(), data["paramName"].ToString(), data["paramValue"].ToString(), data["customizedMsg"].ToString());
                } else
                {
                    result = ErrorLibrary.GetErrorResponse(data["errorNumber"].ToString(), data["paramName"].ToString(), data["paramValue"].ToString(), "");
                }
                
            } else {
            
                // Not a pre defined error.
                result = ErrorLibrary.GetErrorResponse(exception.Message, exception.TargetSite.ToString(), exception.Message, null);  
            }
            
            // building the response
            context.Response.ContentType = "application/json";  
            context.Response.StatusCode = statusCode;

            // bringing to JSON format.
            string json = JsonSerializer.Serialize(result);
            CancellationToken cancellationToken = new CancellationToken(false);

            // not sure if this implementation is correct, as I am using an async method withoud await.
            Task resultTask = context.Response.WriteAsync(json, cancellationToken);

            return resultTask;
        }  
    }    
}   
