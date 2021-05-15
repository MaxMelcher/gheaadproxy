using System;
using System.Diagnostics;
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
using Yarp.ReverseProxy.Abstractions.Config;
using System.Linq;

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
            var proxyBuilder = services.AddReverseProxy().AddTransforms(builderContext =>
            {
                builderContext.AddRequestTransform(transformContext =>
                {
                    var proxyHeaders = transformContext.ProxyRequest.Headers;
                    proxyHeaders.Remove("MFA");

                    if (proxyHeaders.TryGetValues("BASIC", out var basic) &&
                        proxyHeaders.TryGetValues("AUTHORIZATION", out var auth))
                    {
                        proxyHeaders.Remove("AUTHORIZATION");
                        proxyHeaders.Remove("BASIC");

                        proxyHeaders.TryAddWithoutValidation("AUTHORIZATION", basic);
                        Console.WriteLine("!!! REWROTE TOKEN TO GHE");
                    }
                    else if (proxyHeaders.TryGetValues("BASIC", out var basic2))
                    {
                        proxyHeaders.Remove("BASIC");
                        proxyHeaders.TryAddWithoutValidation("AUTHORIZATION", basic);
                        Console.WriteLine("!!! REWROTE TOKEN TO GHE (OAUTH)");
                    }
                    else if (proxyHeaders.TryGetValues("AUTHORIZATION", out var auth2))
                    {
                        if (auth2.Any(x => x.ToLower().StartsWith("bearer") && x.Length > 50))
                        {
                            proxyHeaders.Remove("AUTHORIZATION");
                            Console.WriteLine("!!! REMOVED OAUTH TOKEN TO GHE");
                        }
                    }

                    foreach (var header in proxyHeaders.ToList())
                    {
                        var keys = string.Join(", ", header.Value);
                        Console.WriteLine($"\tTO GHE: {header.Key}:{keys}");
                    }

                    return default;
                });

            });
            // Initialize the reverse proxy from the "ReverseProxy" section of configuration
            proxyBuilder.LoadFromConfig(Configuration.GetSection("ReverseProxy"));

            /*
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
            */
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


            app.Use(async (context, next) =>
            {
                if (context.Request.Headers.ContainsKey("MFA"))
                {
                    Console.WriteLine("!!! REWRITE AUTH FOR PROXY");
                    var mfa = context.Request.Headers["MFA"];
                    var auth = context.Request.Headers["AUTHORIZATION"];

                    //swapping the auth header with the MFA header
                    context.Request.Headers.Remove("AUTHORIZATION");
                    context.Request.Headers.Add("AUTHORIZATION", mfa);
                    context.Request.Headers.Add("BASIC", auth);

                    foreach (var h in context.Request.Headers)
                    {
                        Console.WriteLine($"\tFROM CLIENT: {h.Key}: {h.Value}");
                    }
                }
                else if (context.Request.Headers.ContainsKey("AUTHORIZATION"))
                {
                    var auth = context.Request.Headers["AUTHORIZATION"];

                    //removing the bearer auth for the endpoint /api/v3/user
                    //it will be added back later for the proxy call to GHE
                    if (auth.Any(x => x.ToLower().StartsWith("bearer") && x.Length < 50))
                    {
                        context.Request.Headers.Remove("AUTHORIZATION");
                        context.Request.Headers.Add("BASIC", auth);
                    }
                }

                await next();
            });

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