using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Trinity.BouwNet.Contracts;

namespace Trinity
{


    public delegate void LoggingEventHandler(object sender, LoggingEventArgs e);

    //TODO Rewrite 
    public class LoggingService
    {

        public event LoggingEventHandler SentToLog;

        public static LoggingService LogService
        {
            get;
            set;
        }


        public static string ConnectionString { get; set; }
        public static string EventLogDestination;
        public static string TraceLogDestination;
        public static string HtmlLogDestination;
        public static bool WriteToEventLog;
        public static bool WriteToTraceLog;
        public static bool WriteToHtmlLog;


        public static string ErrorLog
        {
            get { return _errorLog; }
        }

        public string LogName
        { get; set; }

        public static LogType LoggType = LogType.Information;
        private static string _errorLog;

        private static HtmlLogWriter HtmlLogWriter;



        //TODO make with real service website
        public static void SendInfoToLog(string message)
        {


            SendToLog(LogService.LogName, message, LogType.Information);
        }
        public static void SendWarningToLog(string message)
        {
            SendToLog(LogService.LogName, message, LogType.Warning);
        }
        public static void SendErrorToLog(string message)
        {
            SendToLog(LogService.LogName, message, LogType.Error);
        }

        public static void SendErrorToLog(Exception e)
        {
            SendToLog(LogService.LogName, e.ToString(), LogType.Error);
        }


        public static void SendToLog(string message, LogType errorType)
        {
            SendToLog(LogService.LogName, message, LogType.Information);
        }

        public static void SendToLog(Exception error)
        {
            SendToLog(LogService.LogName, error.ToString(), LogType.Error);
        }



        public static void SendToLog(string log, string message, LogType errorType)
        {
            try
            {

                bool InsertToLog = false;
                switch (LoggType)
                {
                    case LogType.Error:
                        if (errorType == LogType.Error)
                            InsertToLog = true;
                        break;
                    case LogType.Warning:
                        if (errorType == LogType.Warning)
                            InsertToLog = true;
                        if (errorType == LogType.Error)
                            InsertToLog = true;

                        break;
                    case LogType.Information:
                        InsertToLog = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("errorType");
                }

                if (InsertToLog)
                {
                    _errorLog += $"{log} - {message} - {errorType.ToString()}" + Environment.NewLine;

                    if (LogService != null)
                        LogService.OnSentToLog(new LoggingEventArgs()
                        {
                            Log = log,
                            LogType = errorType,
                            Message = message,
                            UserName = Environment.UserName,
                        });
                    if (string.IsNullOrEmpty(ConnectionString) == false)
                    {
                        try
                        {
                            using (var conn = new SqlConnection(ConnectionString))
                            {
                                using (var cmd = new SqlCommand("INSERT INTO [dbo].[ErrorLog]([Message],[LogName],[EventType],[InsertDate],[InsertBy])VALUES(@Message, @Logname, @EventType, @InsertDate, @InsertBy)", conn))
                                {
                                    cmd.Parameters.Add("@Message", SqlDbType.NVarChar).Value = message;
                                    cmd.Parameters.Add("@Logname", SqlDbType.VarChar, 50).Value = log;
                                    cmd.Parameters.Add("@EventType", SqlDbType.VarChar, 50).Value = Enum.GetName(typeof(LogType), errorType);
                                    cmd.Parameters.Add("@InsertDate", SqlDbType.DateTime).Value = DateTime.Now;
                                    cmd.Parameters.Add("@InsertBy", SqlDbType.VarChar, 50).Value = Environment.UserName;
                                    // open connection, execute INSERT, close connection
                                    conn.Open();
                                    cmd.ExecuteNonQuery();
                                    conn.Close();
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            LogToEventLog(log, e.ToString(), LogType.Error);
                            LogToEventLog(log, message, errorType);
                        }

                    }
                    if (WriteToEventLog)
                    {
                        LogToEventLog(log, message, errorType);
                    }
                    if (WriteToHtmlLog)
                    {
                        //TODO Extend to slow now 
                        //  LogTotHtmlLog(log, message, errorType);
                    }
                    if (WriteToTraceLog)
                    {
                        LogToTraceLog(log, message, errorType);
                    }
                }

            }
            catch (Exception exception2)
            {
                LogToEventLog(log, exception2.ToString(), LogType.Error);
                LogToEventLog(log, message, errorType);
            }
        }



        private static void LogToEventLog(string log, string message, LogType errorType)
        {
            try
            {
                if (string.IsNullOrEmpty(EventLogDestination))
                    EventLogDestination = "TrinityOrm";

                if (!EventLog.SourceExists(log))
                {
                    EventLog.CreateEventSource(log, EventLogDestination);
                }
                if (errorType <= LoggType)
                {
                    switch (errorType)
                    {
                        case LogType.Information:
                            EventLog.WriteEntry(log, message, EventLogEntryType.Information);
                            break;
                        case LogType.Warning:
                            EventLog.WriteEntry(log, message, EventLogEntryType.Warning);
                            break;
                        case LogType.Error:
                            EventLog.WriteEntry(log, message, EventLogEntryType.Error);
                            break;
                    }
                }

            }
            catch (Exception e)
            {
                LogToTraceLog(log, e.ToString(), LogType.Error);
                LogToTraceLog(log, message, errorType);
            }


        }


        private static void LogTotHtmlLog(string log, string message, LogType errorType)
        {
            if (HtmlLogWriter == null)
            {
                HtmlLogWriter = new HtmlLogWriter();
            }

            if (string.IsNullOrEmpty(HtmlLogDestination))
            {
                HtmlLogDestination = @"c:\Temp";
            }


            HtmlLogWriter.LogLogDestination = HtmlLogDestination;
            HtmlLogWriter.LogName = "TrinityOrmLog.html";
            HtmlLogWriter.Log(log, message, errorType);
        }


        private static void LogToTraceLog(string log, string message, LogType errorType)
        {
            if (string.IsNullOrEmpty(TraceLogDestination))
            {
                TraceLogDestination = @"c:\Temp";
            }

            if (Directory.Exists(TraceLogDestination) == false)
            {
                Directory.CreateDirectory(TraceLogDestination);
            }

            var logTo = Path.Combine(TraceLogDestination, "TrinityOrmLog.log");

            if (Trace.Listeners.Count == 0)
                Trace.Listeners.Add(new TextWriterTraceListener(logTo, log));
            Trace.WriteLine(message, errorType.ToString());
            Trace.Flush();
        }


        public static void SendToLog(string log, Exception error)
        {
            SendToLog(log, error.ToString(), LogType.Error);
        }



        protected virtual void OnSentToLog(LoggingEventArgs e)
        {
            SentToLog?.Invoke(this, e);


        }

        public static void SendToLog(ResultList result)
        {
            if (result.HasErrors)
            {
                SendToLog(LogService.LogName, result.Error, LogType.Error);
            }
            else
            {
                SendToLog(LogService.LogName, result.ToString(), LogType.Information);
            }

        }
    }
}
