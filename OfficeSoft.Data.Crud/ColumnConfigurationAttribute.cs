namespace OfficeSoft.Data.Crud
{
    using System;

    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnConfigurationAttribute : Attribute, IColumnAttribute
    {

        public string Name { get; set; }
        public string TableName { get; set; }


        public ColumnConfigurationAttribute() { }
        
        public ColumnConfigurationAttribute(string name)
        {
            this.Name = name;
        }

        public ColumnConfigurationAttribute(string name, string tableName)
        {
            this.Name = name;
            this.TableName = tableName;
        }
    }
}