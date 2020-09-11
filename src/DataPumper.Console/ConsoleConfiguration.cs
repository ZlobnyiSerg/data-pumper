using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPumper.Console
{
    public class ConsoleConfiguration
    {
        public IConfigurationRoot ConfigurationXml => ConfigurationManager.Configuration ??
            (ConfigurationManager.Configuration = new ConfigurationBuilder()
                .AddXmlFile("console.config")
                .AddXmlFile("console.local.config", true)
                .Build());

        public string SourceConnectionString => ConfigurationXml.Get<string>("Core:SourceConnectionString");

        public string TargetConnectionString => ConfigurationXml.Get<string>("Core:TargetConnectionString");

        public string HangFireDashboardUrl => ConfigurationXml.Get<string>("Core:HangFireDashboardUrl", "http://localhost:9019");

        public string[] TenantCodes
        {
            get
            {
                string tenantCodes = ConfigurationXml.Get<string>("Core:TenantCodes");
                if (string.IsNullOrEmpty(tenantCodes)) return null;

                return tenantCodes.Split(',');
            }
        }
    }
}
