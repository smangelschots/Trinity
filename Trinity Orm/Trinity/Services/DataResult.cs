

using System;
using System.Text;

namespace Trinity
{
    public class DataResult
    {
        public string ToJson()
        {
            var stringBuilder = new StringBuilder();
            //stringBuilder.Append("{");
            //for (int index1 = 0; index1 < this.Data.Rows.Count; ++index1)
            //{
            //    for (int index2 = 0; index2 < Enumerable.Count<string>((IEnumerable<string>)this.Data.Columns); ++index2)
            //        stringBuilder.Append(string.Format("{{{0}:{1}}},", (object)this.Data.Columns[index2], (object)this.GetJsonValue(this.Data.DataTypes[index2], this.Data.Rows[index1].Values[index2])));
            //}
            //stringBuilder.Append("}");
            return ((object)stringBuilder).ToString();
        }

        private string GetJsonValue(string dataType, object value)
        {
            switch (dataType)
            {
                case "image":
                    try
                    {
                        return string.Format("\"{0}\"", (object)Encoding.UTF8.GetString(value as byte[]));
                    }
                    catch (Exception ex)
                    {
                        return string.Empty;
                    }
                case "datetime":
                    return string.Format("\"{0}\"", value.ToDateTime().ToString("yyyy-MM-dd HH:mm:ss"));
                case "int":
                    return string.Format("{0}", value);
                default:
                    return string.Format("\"{0}\"", value);
            }
        }


    }
}
