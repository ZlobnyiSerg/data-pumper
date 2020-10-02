using System;
using Common.Logging;
using Quirco.DataPumper.DataModels;
using Topshelf;

namespace DataPumper.Console
{
    internal class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program)); 

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;

            Log.Info("Data Pumper is running...");
            HostFactory.Run(x =>
            {
                x.Service<WarehouseService>(
                    s =>
                    {
                        s.ConstructUsing(() => new WarehouseService());
                        s.WhenStarted(ws => ws.Start());
                        s.WhenStopped(ws => ws.Stop());
                    });
            });
        }

        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
                Log.Fatal("Unhandled exception in service", exception);
            else
                Log.Fatal("Unhandled error of unknown type: " + e.ExceptionObject);
        }
    }
}
