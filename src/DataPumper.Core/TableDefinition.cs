using System.Linq;

namespace DataPumper.Core
{
    public class TableName
    {
        public string Schema { get; }
        
        public string Name { get; }

        public TableName(string fullName)
        {
            var parts = fullName.Split('.');
            if (parts.Length == 1)
                Name = parts.First();
            else
            {
                Schema = parts[0].TrimStart('[').TrimEnd(']');
                Name = parts[1].TrimStart('[').TrimEnd(']');
            }
        }

        public override string ToString()
        {
            return !string.IsNullOrEmpty(Schema) ? $"[{Schema}].[{Name}]" : $"[{Name}]";
        }
    }
}