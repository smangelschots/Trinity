using System.Collections.Generic;

namespace Trinity
{
    public class ExpandoColumn : DataColumn
    {
        public override void SetValue(object target, object val) { (target as IDictionary<string, object>)[this.ColumnName] = val; }
        public override object GetValue(object target)
        {
            object val = null;
            (target as IDictionary<string, object>).TryGetValue(this.ColumnName, out val);
            return val;
        }
        public override object ChangeType(object val) { return val; }
    }
}