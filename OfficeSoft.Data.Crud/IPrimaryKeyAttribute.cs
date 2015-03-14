namespace OfficeSoft.Data.Crud
{
    public interface IPrimaryKeyAttribute
    {
        string Name { get; }

        string SequenceName { get; set; }

        bool IsAutoIncrement { get; set; }
    }
}