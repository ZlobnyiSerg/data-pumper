using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;

namespace Quirco.DataPumper
{
    public class DataPumperConfiguration
    {
        public IConfigurationRoot ConfigurationXml => ConfigurationManager.Configuration ??
            (ConfigurationManager.Configuration = new ConfigurationBuilder()
                .AddXmlFile("data-pumper.config")
                .AddXmlFile("data-pumper.local.config", true)
                .Build());

        public string ConnectionString => ConfigurationManager.ConnectionString ?? ConfigurationXml.Get<string>("Core:ConnectionString");

        public string CurrentDateQuery => ConfigurationXml.Get<string>("Core:CurrentDateQuery");

        public string ActualityColumnName => ConfigurationXml.Get<string>("Core:ActualityColumnName");

        public string Properties => ConfigurationXml.Get<string>("Core:Properties");

        public string HistoricColumnFrom => ConfigurationXml.Get<string>("Core:HistoricColumns:From");

        public string HistoricColumnTo => ConfigurationXml.Get<string>("Core:HistoricColumns:To");

        public string EmailFrom => ConfigurationXml.Get<string>("SmtpSender:Sender:Email");

        public string PasswordFrom => ConfigurationXml.Get<string>("SmtpSender:Sender:Password");

        public List<string> Targets => ConfigurationXml.GetList<string>("SmtpSender:Targets", ',');

        public string ServerAdress => ConfigurationXml.Get<string>("SmtpSender:SmtpServer:Adress");

        public int ServerPort => ConfigurationXml.Get<int>("SmtpSender:SmtpServer:Port");

        public PumperJobItem[] Jobs => ConfigurationXml.GetSection("Jobs").GetChildren().Select(c => new PumperJobItem 
        {
            Name = c.Key,
            SourceTableName = c.Get<string>("Source"),
            TargetTableName = c.Get<string>("Target"),
            HistoricMode = c.Get("HistoricMode", false),
            PreRunStoreProcedureOnTarget = c.Get("TargetSP:SPQueryBefore", ""),
            PostRunStoredProcedureOnTarget = c.Get("TargetSP:SPQueryAfter", "")
        }).ToArray();

        public DataPumperConfiguration()
        {
            if (string.IsNullOrEmpty(Properties))
                throw new ApplicationException($"Required set 'Properties' in data-pumper.config");

            if (string.IsNullOrEmpty(ActualityColumnName))
                throw new ApplicationException($"Required set 'ActualityColumnName' in data-pumper.config");

            if (string.IsNullOrEmpty(HistoricColumnFrom))
                throw new ApplicationException($"Required set 'HistoricColumns.From' in data-pumper.config");

            if (string.IsNullOrEmpty(HistoricColumnTo))
                throw new ApplicationException($"Required set 'HistoricColumns.To' in data-pumper.config");

            if (string.IsNullOrEmpty(CurrentDateQuery))
                throw new ApplicationException($"Required set 'CurrentDateQuery' in data-pumper.config");
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
        public string PreRunStoreProcedureOnTarget { get; set; }

        /// <summary>
        /// Запрос на вызов хранимой процедуры после выполнения задания
        /// </summary>
        public string PostRunStoredProcedureOnTarget { get; set; }

        public override string ToString()
        {
            return $"{Name} [{SourceTableName}] -> [{TargetTableName}] HistoricMode='{HistoricMode}'";
        }
    }
}
