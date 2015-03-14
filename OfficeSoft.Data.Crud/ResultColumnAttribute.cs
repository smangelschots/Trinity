namespace OfficeSoft.Data.Crud
{
    using System;

    [AttributeUsage(AttributeTargets.Property)]
    public class ResultColumnAttribute : ColumnConfigurationAttribute
    {
        public ResultColumnAttribute() { }
        public ResultColumnAttribute(string name) : base(name) { }
    }
}