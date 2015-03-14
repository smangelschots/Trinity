
namespace OfficeSoft.Data.Crud
{
    using System.Data;
    using System.Data.SqlClient;

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
