using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Quirco.DataPumper
{
    public class DataPumperConfiguration
    {
        private readonly IConfiguration _configuration;
        
        public string ConnectionString => ConfigurationManager.ConnectionString ?? _configuration.Get<string>("Core:ConnectionString");

        public string CurrentDateQuery => _configuration.Get<string>("Core:CurrentDateQuery");

        public string ActualityColumnName => _configuration.GetRequired<string>("Core:ActualityColumnName");

        public string TenantField => _configuration.Get<string>("Core:TenantField");

        public string HistoricColumnFrom => _configuration.Get<string>("Core:HistoricColumns:From");

        public string HistoricColumnTo => _configuration.Get<string>("Core:HistoricColumns:To");

        public string EmailFrom => _configuration.Get<string>("EmailNotifications:Sender:Email");

        public string PasswordFrom => _configuration.Get<string>("EmailNotifications:Sender:Password");

        public string Recipients => _configuration.Get<string>("EmailNotifications:Recipients", null);

        public string ServerAdress => _configuration.Get<string>("EmailNotifications:SmtpServer:Adress");

        public int ServerPort => _configuration.Get<int>("EmailNotifications:SmtpServer:Port");

        public PumperJobItem[] Jobs => _configuration.GetSection("Jobs").GetChildren().Select(c => new PumperJobItem
        {
            Name = c.Key,
            SourceTableName = c.Get<string>("Source"),
            TargetTableName = c.Get<string>("Target"),
            HistoricMode = c.Get("HistoricMode", false),
            PreRunQuery = c.Get<string>("Queries:PreRun"),
            PostRunQuery = c.Get<string>("Queries:PostRun")
        }).ToArray();

        public DataPumperConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;
        }
    }

    public class PumperJobItem
    {
        public string Name { get; set; }

        public string SourceTableName { get; set; }

        public string TargetTableName { get; set; }

        public bool HistoricMode { get; set; }

        /// <summary>
        /// Запрос на вызов хранимой процедуры до выполнения задания
        /// </summary>
        public string PreRunQuery { get; set; }

        /// <summary>
        /// Запрос на вызов хранимой процедуры после выполнения задания
        /// </summary>
        public string PostRunQuery { get; set; }

        public override string ToString()
        {
            return $"{Name} [{SourceTableName}] -> [{TargetTableName}] HistoricMode='{HistoricMode}'";
        }
    }
}