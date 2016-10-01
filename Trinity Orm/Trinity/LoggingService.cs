using System;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;

namespace Trinity
{

    //TODO Rewrite 
    public class LoggingService
    {


    

        public static LoggingService LogService
        {
            get;
            set;
        }


        public static string ConnectionString { get; set; }

        public static string ErrorLog
        {
            get { return _errorLog; }
        }

        public static ErrorType LoggType = ErrorType.Error;
        private static string _errorLog;

        //TODO make with real service website

        public static void SendToLog(string log, string message, ErrorType errorType)
        {
            try
            {
                if (string.IsNullOrEmpty(ConnectionString))
                {
                    //TODO remove make error report in class but not in file gives error on iis
                    _errorLog = "No ConnectionString was set for the LoggingService \n";
                    //System.IO.File.WriteAllText(@"Error.txt", "No ConnectionString was set for the LoggingService");
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
                if (LogService != null)
                {
                    LogService.RegisterError(exception2);
                }
                _errorLog += string.Format("\n{0}", exception2.Message);
            }
        }

        private void RegisterError(Exception exception2)
        {
            //TODO expand
            throw new NotImplementedException("Register Error has not been implemented");
        }
    }
}
