using System.Linq;

namespace DataPumper.Core
{
    public class DataSource
    {
        public string? Schema { get; }
        
        public string Name { get; }

        public string SourceFullName { get; }
        
        public bool IsStoredProcedure { get; }

        public DataSource(string fullName, bool isStoredProcedure = false)
        {
            SourceFullName = fullName;
            IsStoredProcedure = isStoredProcedure;
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

        public string ToStringUniversal()
        {
            return !string.IsNullOrEmpty(Schema) ? $"{Schema}.{Name}" : Name;
        }
    }
}