namespace DataPumper.Core
{
    public class FieldDefinition
    {
        public string Name { get; private set; }
        
        public string Description { get; private set; }
        
        public FieldType Type { get; private set; }

        public FieldDefinition(string name, string description, FieldType type)
        {
            Name = name;
            Description = description;
            Type = type;
        }
    }
    
    public enum FieldType
    {
        Integer,
        String,
        DateTime,
        Bit,
        Binary
    }
}