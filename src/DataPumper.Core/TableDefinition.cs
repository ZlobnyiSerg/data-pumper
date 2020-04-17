using System.Linq;

namespace DataPumper.Core
{
    public class TableDefinition
    {
        public TableName Name { get; }
        
        public string Description { get; }
        
        public FieldDefinition[] Fields { get; set; }

        public TableDefinition(TableName name, string description)
        {
            Name = name;
            Description = description;
        }
    }

    public class TableName
    {
        public string Schema { get; }
        
        public string Name { get; }

        public TableName(string schema, string name)
        {
            Schema = schema.TrimStart('[').TrimEnd(']');
            Name = name.TrimStart('[').TrimEnd(']');
        }
        
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
            if (!string.IsNullOrEmpty(Schema))
                return $"[{Schema}].[{Name}]";
            return $"[{Name}]";
        }
    }
}