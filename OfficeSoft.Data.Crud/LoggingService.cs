using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace OfficeSoft.Data.Crud
{
    public class LoggingService
    {

        public static string ConnectionString { get; set; }
        public static ErrorType LoggType = ErrorType.Error;

        //TODO make with real service website

        public static void SetMessage(string log, string message, ErrorType errorType)
        {
            try
            {

                //TODO wijzigen dit moet herschreven worden
                if (string.IsNullOrEmpty(ConnectionString))
                {
                    System.IO.File.WriteAllText(@"Error.txt", "No ConnectionString was set for the LoggingService");
                    return;
                }

                bool InsertToLog = false;

                switch (LoggType)
                {
                    case ErrorType.Error:
                        if (errorType == ErrorType.Error)
                            InsertToLog = true;
                        break;
                    case ErrorType.Warning:
                        if (errorType == ErrorType.Warning)
                            InsertToLog = true;
                        if (errorType == ErrorType.Error)
                            InsertToLog = true;

                        break;
                    case ErrorType.Information:
                            InsertToLog = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("errorType");
                }

                if (InsertToLog)
                    using (var conn = new SqlConnection(ConnectionString))
                    {
                        using (var cmd = new SqlCommand("INSERT INTO [dbo].[ErrorLog]([Message],[LogName],[EventType],[InsertDate],[InsertBy])VALUES(@Message, @Logname, @EventType, @InsertDate, @InsertBy)", conn))
                        {
                            cmd.Parameters.Add("@Message", SqlDbType.NVarChar).Value = message;
                            cmd.Parameters.Add("@Logname", SqlDbType.VarChar, 50).Value = log;
                            cmd.Parameters.Add("@EventType", SqlDbType.VarChar, 50).Value = Enum.GetName(typeof(ErrorType), errorType);
                            cmd.Parameters.Add("@InsertDate", SqlDbType.DateTime).Value = DateTime.Now;
                            cmd.Parameters.Add("@InsertBy", SqlDbType.VarChar, 50).Value = Environment.UserName;
                            // open connection, execute INSERT, close connection
                            conn.Open();
                            cmd.ExecuteNonQuery();
                            conn.Close();
                        }
                    }
            }
            catch (Exception exception2)
            {
                System.IO.File.WriteAllText(@"Error.txt", exception2.Message);
            }
        }

    }
}
