
using System.Data;

namespace Trinity.MsSql
{
    public class SqlDataParameter : SqlColumnMap, IDataParameter
    {
        public string Name { get; set; }

        public DataRowVersion SourceVersion { get; set; }

        public object Value { get; set; }

        public string SourceColumn { get; set; }

        public ParameterDirection Direction { get; set; }

        public bool IsSelectParameter { get; set; }

        public SqlDataParameter()
        {
            this.Direction = ParameterDirection.Input;
        }

    }
}
