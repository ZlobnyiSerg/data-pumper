using System;
using System.Linq;
using System.Net.Mime;
using DataPumper.Web.DataLayer;
using DataPumper.Web.Services;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Topshelf;
using Host = Microsoft.Extensions.Hosting.Host;

namespace DataPumper.Web
{
    public class MainService : ServiceControl
    {
        private IHost _webHost;
        public const string JobId = "main-job";

        public bool Start(HostControl hostControl)
        {
            _webHost = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((ctx, e) =>
                {
                    e.AddJsonFile("appsettings.json")
                        .AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", true)
                        .AddJsonFile("appsettings.local.json", true)
                        .AddEnvironmentVariables();
                })
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
                .Build();

            _webHost.Start();

            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            _webHost?.Dispose();
            return true;
        }
    }
}