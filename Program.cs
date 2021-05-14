using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;


namespace MaxMelcher.GHEAADProxy
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            // Create a Kestrel web server, and tell it to use the Startup class
            // for the service configuration
            var myHostBuilder = Host.CreateDefaultBuilder(args);
            myHostBuilder.ConfigureWebHostDefaults(webHostBuilder =>
                {
                    webHostBuilder.UseStartup<Startup>();
                });
                
            var myHost = myHostBuilder.Build();
            myHost.Run();
        }
    }
}