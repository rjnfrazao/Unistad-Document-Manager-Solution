using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StorageLibrary.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Unistad_Document_Manager
{
    public class Startup
    {


        private readonly HttpClient _httpClient;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            _httpClient = new HttpClient();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();

            // Support Generic IConfiguration access for generic string access
            services.AddSingleton(Configuration);

            // Added to be used in the entire application;
            services.AddHttpClient();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }


        /// <summary>
        /// This function was created just to remove the following warning : "Calling 'BuildServiceProvider' from application code results in copy of Singleton warning"
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        private ServiceProvider BuildTheProvider(IServiceCollection services)
        {
            return services.BuildServiceProvider();
        }


        /// <summary>
        /// Dummy class required due to the configuration of the Logger in Configure method.
        /// </summary>
        /*public class ApplicationLogs
        {

        }*/

    }
}
