using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using DataPumper.Sql;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Owin.Hosting;
using Microsoft.Practices.Unity;
using Newtonsoft.Json;
using Quirco.DataPumper;
using Topshelf;

namespace DataPumper.Console
{
    class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program)); 

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;

            Log.Info($"Data Pumper is running...");

            HostFactory.Run(x =>
            {
                x.Service<MainService>(
                    s =>
                    {
                        s.ConstructUsing(() => new MainService());
                        s.WhenStarted(ws => ws.Start());
                        s.WhenStopped(ws => ws.Stop());
                    });
            });
        }

        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception != null)
                Log.Fatal("Unhandled exception in service", exception);
            else
                Log.Fatal("Unhandled error of unknown type: " + e.ExceptionObject);
        }
    }
}
