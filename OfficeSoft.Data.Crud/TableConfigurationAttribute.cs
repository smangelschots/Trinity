namespace OfficeSoft.Data.Crud
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public class TableConfigurationAttribute : Attribute, ITableConfigurationAttribute
    {
        public TableConfigurationAttribute(string tableName)
        {
            this.TableName = tableName;
        }
        public string TableName { get; private set; }
    }
}