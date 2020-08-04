using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;

namespace Quirco.DataPumper
{
    public class DPConfiguration
    {
        public IConfigurationRoot ConfigurationXml => ConfigurationManager.Configuration ??
            (ConfigurationManager.Configuration = new ConfigurationBuilder()
                .AddXmlFile("data-pumper.config")
                .AddXmlFile("data-pumper.local.config", true)
                .Build());

        public string ConnectionString => ConfigurationXml.Get<string>("Core:ConnectionString");

        public string CurrentDateQuery => ConfigurationXml.Get<string>("Core:CurrentDateQuery");

        public string ActualityColumnName => ConfigurationXml.Get<string>("Core:ActualityColumnName");

        public string Properties => ConfigurationXml.Get<string>("Core:Properties");

        public string HistoricColumnFrom => ConfigurationXml.Get<string>("Core:HistoricColumns:From");

        public string HistoricColumnTo => ConfigurationXml.Get<string>("Core:HistoricColumns:To");

        public ConfigJobItem[] Jobs => ConfigurationXml.GetSection("Jobs").GetChildren().Select(c => new ConfigJobItem 
        {
            Name = c.Key,
            SourceTableName = c.Get<string>("Source"),
            TargetTableName = c.Get<string>("Target"),
            HistoricMode = c.Get("HistoricMode", false),
            TargetSPQueryBefore = c.Get("TargetSP:SPQueryBefore", ""),
            TargetSPQueryAfter = c.Get("TargetSP:SPQueryAfter", "")
        }).ToArray();
    }

    public class ConfigJobItem
    {
        public string Name { get; set; }

        public string SourceTableName { get; set; }

        public string TargetTableName { get; set; }

        public bool HistoricMode { get; set; }

        /// <summary>
        /// Запрос на вызов хранимой процедуры до выполнения задания
        /// </summary>
        public string TargetSPQueryBefore { get; set; }

        /// <summary>
        /// Запрос на вызов хранимой процедуры после выполнения задания
        /// </summary>
        public string TargetSPQueryAfter { get; set; }

        public override string ToString()
        {
            return $"{Name} [{SourceTableName}] -> [{TargetTableName}] HistoricMode='{HistoricMode}'";
        }
    }
}
