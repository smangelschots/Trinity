using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace OfficeSoft.Data.Crud
{
    public class MySqlDataParameter : MySqlColumnMap, IDataParameter
    {
        public string Name { get; set; }
        public DataRowVersion SourceVersion { get; set; }
        public object Value { get; set; }
        public string SourceColumn { get; set; }
        public ParameterDirection Direction { get; set; }
        public bool IsSelectParameter { get; set; }
    }
}
