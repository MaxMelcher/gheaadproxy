using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;

namespace MaxMelcher.GHEAADProxy
{
    // Sets up the ASP.NET application with the reverse proxy enabled.
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            // Default configuration comes from AppSettings.json file in project/output
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add capabilities to
        // the web application via services in the DI container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add the reverse proxy to capability to the server
            var proxyBuilder = services.AddReverseProxy();
            // Initialize the reverse proxy from the "ReverseProxy" section of configuration
            proxyBuilder.LoadFromConfig(Configuration.GetSection("ReverseProxy"));



            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                            .AddMicrosoftIdentityWebApp(options =>
                            {
                                Configuration.Bind("AzureAd", options);

                                options.Events.OnAuthorizationCodeReceived = ctx =>
                                {
                                    Console.WriteLine("### OnAuthorizationCodeReceived");
                                    return Task.CompletedTask;
                                };

                                options.Events.OnMessageReceived = ctx =>
                                {
                                    Console.WriteLine("### OnMessageReceived");
                                    return Task.CompletedTask;
                                };

                                options.Events.OnRedirectToIdentityProvider = ctx =>
                                {
                                    Console.WriteLine("### OnRedirectToIdentityProvider");
                                    return Task.CompletedTask;
                                };

                                options.Events.OnTokenValidated = ctx =>
                                {
                                    Console.WriteLine("### OnTokenValidated");
                                    return Task.CompletedTask;
                                };

                                options.Events.OnAuthenticationFailed = ctx =>
                                {
                                    Console.WriteLine("### OnAuthenticationFailed");
                                    return Task.CompletedTask;
                                };

                                options.Events.OnRedirectToIdentityProvider = ctx =>
                                {
                                    Console.WriteLine("### OnRedirectToIdentityProvider");
                                    return Task.CompletedTask;
                                };
                            });

            services.AddMicrosoftIdentityWebApiAuthentication(Configuration, "AzureAd");

            services.AddAuthorization(options =>
            {
                options.AddPolicy("authenticated", policy =>
                    policy.RequireAuthenticatedUser());
            });

            services.AddHealthChecks();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request 
        // pipeline that handles requests
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Enable endpoint routing, required for the reverse proxy
            app.UseRouting();

            // Require Authentication
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health").WithMetadata(new AllowAnonymousAttribute());
            });

            // Register the reverse proxy routes
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapReverseProxy();
            });
        }
    }

    public class Logger
    {
        RequestDelegate next;

        public Logger(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            //Request handling comes here
            var url = context.Request.Path;
            var response = context.Response.StatusCode;
            Console.WriteLine($"### REQUEST {url} - {response}");
            await next.Invoke(context);
        }
    }
}