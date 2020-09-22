using System.Threading.Tasks;
using DataPumper.Sql;
using Hangfire;
using Quirco.DataPumper;

namespace DataPumper.Console
{
    public class DataPumperJobs
    {
        private readonly ConsoleConfiguration _configuration;

        public DataPumperJobs(ConsoleConfiguration configuration)
        {
            _configuration = configuration;
        }

        [JobDisplayName("Run single job: {0}")]
        [Queue(MainService.Queue)]
        public async Task RunJob(PumperJobItem jobItem)
        {
            var dataPumperService = new DataPumperService(new DataPumperConfiguration(_configuration.ConfigurationSource), _configuration.TenantCodes);

            var sourceProvider = new SqlDataPumperSourceTarget();
            await sourceProvider.Initialize(_configuration.ConnectionString);

            var targetProvider = new SqlDataPumperSourceTarget();
            await targetProvider.Initialize(_configuration.TargetConnectionString);

            await dataPumperService.RunJob(jobItem, sourceProvider, targetProvider);
        }

        [JobDisplayName("Run all jobs")]
        [Queue(MainService.Queue)]
        public async Task RunJobs(bool fullReload)
        {
            var dataPumperService = new DataPumperService(new DataPumperConfiguration(_configuration.ConfigurationSource), _configuration.TenantCodes);

            var sourceProvider = new SqlDataPumperSourceTarget();
            await sourceProvider.Initialize(_configuration.ConnectionString);

            var targetProvider = new SqlDataPumperSourceTarget();
            await targetProvider.Initialize(_configuration.TargetConnectionString);

            await dataPumperService.RunJobs(sourceProvider, targetProvider, fullReload);
        }
    }
}