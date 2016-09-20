using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Trinity.MsSql
{



    public class SqlDataBindingList<T> : DataBindingList<T>
               where T : class
    {

        public SqlDataBindingList(string connectionString, string tableName, string[] primaryKeys) : base(new SqlServerDataManager<T>(connectionString))
        {
            this.DataManager.ClearCommands();
            this.TableName = tableName;
            this.PrimaryKeys = primaryKeys;
        }

    }
}
