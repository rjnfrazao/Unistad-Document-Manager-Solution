using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
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
            // Add Azure AD - Microsoft Identity Authentication
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)               
                .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"));

            services.AddControllersWithViews(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            });

            services.AddRazorPages()
                .AddMvcOptions(options => { })
                .AddMicrosoftIdentityUI();

            // Support Generic IConfiguration access for generic string access
            services.AddSingleton(Configuration);

            // Added to be used in the entire application;
            services.AddHttpClient();

            // Allow anounymous pages.
            services.AddRazorPages(options =>
            {
                options.Conventions.AllowAnonymousToPage("/Privacy");
                options.Conventions.AllowAnonymousToPage("/Index");
            });


            // FIX : Redirect UTI in Docker containers needs this configuration to replace http by https.
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                              ForwardedHeaders.XForwardedProto;
                // Only loopback proxies are allowed by default.
                // Clear that restriction because forwarders are enabled by explicit
                // configuration.
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

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

            app.UseAuthentication();
            app.UseAuthorization();

            // FIX : Part 2 - Fix the redirect uri to https in the authentication process
            app.UseForwardedHeaders();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
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
