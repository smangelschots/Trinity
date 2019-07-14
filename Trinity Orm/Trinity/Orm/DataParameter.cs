
using System.Data;
using System.Data.SqlClient;

namespace Trinity
{
    public class DataParameter : ColumnMap , IDataParameter
    {
        public string Name { get; set; }

        public DataRowVersion SourceVersion { get; set; }

        public object Value { get; set; }

     //   public string ParameterName { get; set; }

        public string SourceColumn { get; set; }

        public ParameterDirection Direction { get; set; }

        public bool IsSelectParameter { get; set; }

        public DataParameter()
        {
            this.SqlDbType = SqlDbType.NVarChar;
            this.Direction = ParameterDirection.Input;
        }

        public SqlParameter GetSqlParameter()
        {
            var sqlParameter = new SqlParameter();
            sqlParameter.ParameterName = string.Format("@{0}", (object)this.Name);
            sqlParameter.SqlDbType = this.SqlDbType;
            sqlParameter.Value = this.Value;
            sqlParameter.IsNullable = this.IsNullable;
            sqlParameter.Direction = this.Direction;
            sqlParameter.Size = this.Size;
           // sqlParameter.Scale = this.Scale;
            return sqlParameter;
        }
    }
}
