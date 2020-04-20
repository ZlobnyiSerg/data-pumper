using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Topshelf;

namespace DataPumper.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            HostFactory.Run(c =>
            {
                c.Service<MainService>();
                c.SetServiceName("DataPumper");
                c.SetDisplayName("Data Pumper");
            });
        }
    }
}