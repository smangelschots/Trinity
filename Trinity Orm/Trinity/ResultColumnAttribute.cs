using System;

namespace Trinity
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ResultColumnAttribute : ColumnConfigurationAttribute
    {
        public ResultColumnAttribute() { }
        public ResultColumnAttribute(string name) : base(name) { }
    }
}