using Microsoft.AspNetCore.Builder;  


namespace DocumentUploader.Middleware
{

    /// <summary>
    /// Class configured in the startup to add it in the chain of middlewares.
    /// </summary>
    public static class ExceptionHandlerMiddlewareExtensions  
    {  
        public static void UseExceptionHandlerMiddleware(this IApplicationBuilder app)  
        {  
            app.UseMiddleware<ExceptionHandlerMiddleware>();  
        }  
    }  
}  
