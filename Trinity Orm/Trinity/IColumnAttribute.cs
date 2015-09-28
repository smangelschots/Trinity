namespace Trinity
{
    public interface IColumnAttribute
    {
        string Name { get; set; }

        string TableName { get; set; }
    }
}