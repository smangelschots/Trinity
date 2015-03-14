using System;
using System.Linq.Expressions;

namespace OfficeSoft.Data.Crud
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Data.Common;
    using System.Linq;

    public static class Dmq
    {

        public static DbParameter GetParameter(this DbParameterCollection parameters, string name)
        {
            return parameters.Cast<DbParameter>().FirstOrDefault(item => item.ParameterName == name);
        }

        public static bool ContainsParameter(this DbParameterCollection parameters, string name)
        {
            return parameters.Cast<DbParameter>().Any(item => item.ParameterName == name);
        }



    }
}
