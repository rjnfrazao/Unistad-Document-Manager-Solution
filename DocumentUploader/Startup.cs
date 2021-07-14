using DocumentUploader.Middleware;
using StorageLibrary.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DocumentUploader
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // Support Generic IConfiguration access for generic string access
            services.AddSingleton(Configuration);

            // Add Storage Service. Scoped means the same instance for the request but different across requests
            services.AddScoped<IStorage, Storage>();

            // Add Queue Service. Scoped means the same instance for the request but different across requests
            services.AddScoped<IQueue, StorageQueue>();

            // Setup Swagger Document
            SetupSwaggerDocument(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            app.UseExceptionHandlerMiddleware();


            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }


            // Setup Swagger 
            SetupSwaggerJsonGenerationAndUI(app);

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }


        /// <summary>
        /// Sets up the swagger documents
        /// </summary>
        /// <param name="services">The service collection</param>

        private void SetupSwaggerDocument(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "UNISTAD Document Uploader API",
                    Version = "v1",
                    Description = "UNISTAD document uploader application. These API allows to upload files to be organized into Storage File folders.",
                    TermsOfService = new Uri("http://www.fluminense.com.br"),
                    Contact = new OpenApiContact
                    {
                        Name = "Ricardo Frazao",
                        Email = "dontcare@nowhere.co"
                    },

                });

                // #E-94 Tip: Added based on instructions in class.
                // Use method name as operationId so that ADD REST Client... will work
                c.CustomOperationIds(apiDesc =>
                {
                    return apiDesc.TryGetMethodInfo(out MethodInfo methodInfo) ? methodInfo.Name : null;
                });



                // #E-94 Tip: according instructions during the classe. Location where xml is saved.
                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);


            });
        }

        /// <summary>
        /// #E-94 Tip: Sets up the Swagger JSON file and Swagger Interactive UI.  Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application builder</param>
        private static void SetupSwaggerJsonGenerationAndUI(IApplicationBuilder app)
        {
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger(c =>
            {
                // Use the older 2.0 format so the ADD REST Client... will work
                c.SerializeAsV2 = true;
            });

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            //       specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "UNISTAD Document Manager v1");

                // Serve the Swagger UI at the app's root (http://localhost:<port>)
                c.RoutePrefix = string.Empty;
            });

        }

    }
}
