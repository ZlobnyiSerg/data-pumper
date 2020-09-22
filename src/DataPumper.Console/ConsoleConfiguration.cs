using Microsoft.Extensions.Configuration;

namespace DataPumper.Console
{
    public class ConsoleConfiguration
    {
        private readonly IConfiguration _configuration;

        public ConsoleConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string ConnectionString => _configuration.Get<string>("Core:ConnectionString");

        public string TargetConnectionString => _configuration.Get<string>("Core:TargetConnectionString");

        public string HangFireDashboardUrl => _configuration.Get<string>("Core:HangFireDashboardUrl", "http://localhost:9019");
        
        public string ScheduleCron => _configuration.Get<string>("Core:ScheduleCron");

        public string[] TenantCodes
        {
            get
            {
                var tenantCodes = _configuration.Get<string>("Core:TenantCodes");
                if (string.IsNullOrEmpty(tenantCodes)) return null;

                return tenantCodes.Split(',');
            }
        }
    }
}
