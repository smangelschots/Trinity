namespace OfficeSoft.Data.Crud
{
    using System;
    using System.Reflection;

    public class DataColumn
    {
        public string ColumnName;
        public PropertyInfo PropertyInfo;
        public bool ResultColumn;
        public virtual void SetValue(object target, object val) { this.PropertyInfo.SetValue(target, val, null); }
        public virtual object GetValue(object target) { return this.PropertyInfo.GetValue(target, null); }
        public virtual object ChangeType(object val) { return Convert.ChangeType(val, this.PropertyInfo.PropertyType); }
    }
}