using Common.Logging;
using DataPumper.Core;
using DataPumper.Sql;
using Microsoft.Practices.Unity;
using System;

namespace Quirco.DataPumper
{
    public class DataPumperService
    {

        private readonly IUnityContainer _container;

        private readonly IActualityDatesProvider _actualityDatesProvider;

        public DataPumperService(IUnityContainer container, IActualityDatesProvider actualityDatesProvider)
        {
            _container = container;
            _actualityDatesProvider = actualityDatesProvider;
        }

        public async void RunJobs(string sourceProviderName, string sourceConnectionString, string targetProviderName, string targetConnectionString)
        {
            InitDataPumperSource(sourceProviderName);
            InitDataPumperTarget(targetProviderName);

            var sourceProvider = _container.Resolve<IDataPumperSource>();
            await sourceProvider.Initialize(targetConnectionString);

            var targetProvider = _container.Resolve<IDataPumperTarget>();
            await targetProvider.Initialize(sourceConnectionString);
        }

        private void InitDataPumperSource(string targetProviderName)
        {
            if (targetProviderName == "Sql")
            {
                _container.RegisterType<IDataPumperSource, SqlDataPumperSourceTarget>(new HierarchicalLifetimeManager());                
            }
        }

        private void InitDataPumperTarget(string sourceProviderName)
        {
            if (sourceProviderName == "Sql")
            {
                _container.RegisterType<IDataPumperTarget, SqlDataPumperSourceTarget>(new HierarchicalLifetimeManager());
            }
        }
    }
}
