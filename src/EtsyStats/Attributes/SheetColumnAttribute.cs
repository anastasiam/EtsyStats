namespace EtsyStats.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class SheetColumnAttribute : Attribute
{
    public string? ColumnName { get; set; }

    public int Order { get; set; }

    public SheetColumnAttribute()
    {
    }

    public SheetColumnAttribute(string columnName)
    {
        ColumnName = columnName;
    }
}