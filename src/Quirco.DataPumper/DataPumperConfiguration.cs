using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Quirco.DataPumper
{
    public class DataPumperConfiguration
    {
        private readonly IConfiguration _configuration;

        private string _overridenConnectionString;
        
        public string MetadataConnectionString => _overridenConnectionString ?? _configuration.GetRequiredWithFallback("Core:MetadataConnectionString", "Core:ConnectionString");

        public string CurrentDateQuery => _configuration.Get<string>("Core:CurrentDateQuery");

        public string ActualityColumnName => _configuration.GetRequired<string>("Core:ActualityColumnName");

        public string TenantField => _configuration.Get<string>("Core:TenantField");

        public string HistoricColumnsFrom => _configuration.Get<string>("Core:HistoricColumns:From", "HistoryDateFrom");

        public string HistoricColumnsTo => _configuration.Get<string>("Core:HistoricColumns:To", "HistoryDateTo");
        
        public int BackwardReloadDays => _configuration.Get("Core:BackwardReloadDays", -1);

        public DateTime? DeleteProtectionDate => GetDeleteProtectionDate();

        private DateTime? GetDeleteProtectionDate()
        {
            var date = _configuration.Get<string>("Core:DeleteProtectionDate");
            if (!string.IsNullOrEmpty(date))
                return DateTime.ParseExact(date, "dd.MM.yyyy", CultureInfo.InvariantCulture);
            return null;
        }

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
            PostRunQuery = c.Get<string>("Queries:PostRun"),
            Order = int.TryParse(c.Get("Order"), out int order) ? order : 500,
            StoredProcedure = c.Get<bool>("StoredProcedure")
        }).OrderBy(j => j.Order).ToArray();

        public DataPumperConfiguration(IConfiguration configuration, string connectionString = null)
        {
            _configuration = configuration;
            _overridenConnectionString = connectionString;
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

        /// <summary>
        /// Порядок отработки заданий
        /// </summary>
        public int Order { get; set; }
        
        public bool StoredProcedure { get; set; }

        public override string ToString()
        {
            return $"{Name} [{SourceTableName}] -> [{TargetTableName}] Historic='{HistoricMode}'";
        }
    }
}