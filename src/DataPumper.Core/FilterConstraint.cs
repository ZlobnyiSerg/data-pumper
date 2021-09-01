namespace DataPumper.Core
{
    public class FilterConstraint
    {
        public string FieldName { get; }
        
        public string[] Values { get; }

        public FilterConstraint(string fieldName, params string[] values)
        {
            FieldName = fieldName;
            Values = values;
        }
    }
}