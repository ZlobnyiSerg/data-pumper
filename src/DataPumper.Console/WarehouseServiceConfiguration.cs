using Microsoft.Extensions.Configuration;
using Quirco.DataPumper;

namespace DataPumper.Console
{
    public class WarehouseServiceConfiguration
    {
        private readonly IConfiguration _configuration;

        public WarehouseServiceConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        public string MetadataConnectionString => _configuration.GetRequiredWithFallback("Core:MetadataConnectionString", "Core:ConnectionString");

        public string SourceConnectionString => _configuration.Get<string>("Core:ConnectionString");

        public string TargetProvider => _configuration.Get<string>("Core:TargetProvider");

        public string TargetConnectionString => _configuration.Get<string>("Core:TargetConnectionString");

        public string HangFireDashboardUrl => _configuration.Get<string>("Core:HangFireDashboardUrl", "http://localhost:9019");
        
        public string HangfireConnectionString => _configuration.Get<string>("Core:HangfireConnectionString");
        
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
