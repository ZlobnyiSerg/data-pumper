namespace DataPumper.PostgreSql;

public static class Helpers
{
    public static string Enquote(this string value, char quote = '"')
    {
        return $"{quote}{value}{quote}";
    }
}