namespace projects.Attributes;

public class ColumnSqlAttribute : Attribute
{
    public string Name { get; set; }
    public int Order { get; set; }
    public ColumnSqlAttribute()
    {

    }
    public ColumnSqlAttribute(string name, int order)
    {
        this.Name = name;
        this.Order = order;
    }
}