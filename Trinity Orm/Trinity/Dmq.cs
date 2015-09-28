using System.Data.Common;
using System.Linq;

namespace Trinity
{
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
