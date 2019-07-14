using System;
using System.IO;

namespace Trinity
{
    namespace BouwNet.Contracts
    {



        internal class HtmlLog
        {
            public HtmlLog()
            {
                this.LogTime = DateTime.Now;
            }

            public string Message { get; set; }

            public LogType Type { get; set; }

            public DateTime LogTime { get; set; }


            public Exception Exception { get; set; }

            public override string ToString()
            {
                if (this.Exception != null)
                {
                    return string.Format("{0} {1}  {2} {3} {4} {5} {6}", this.Type.ToString(), this.Message, this.LogTime.ToLongDateString(), Environment.NewLine, this.Exception.ToString(), Environment.NewLine, this.Exception.StackTrace);
                }
                return string.Format("{0} {1}  {2}", this.Type.ToString(), this.Message, this.LogTime.ToLongDateString());
            }

            public string ToHtml()
            {


                if (this.Exception != null)
                {

                    return string.Format(
                        "<tr CLASS='{0}'><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td></tr>",
                        this.Type,
                        this.Message,
                        this.LogTime.ToString(),
                        this.Exception.ToString(),
                        this.Exception.StackTrace);
                }

                return string.Format(
                    "<tr CLASS='{0}'><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td></tr>",
                    this.Type,
                    this.Message,
                    this.LogTime.ToString(),
                    "",
                    "");

            }


        }



        public class HtmlLogWriter
        {



            public string LogLogDestination { get; set; }
            public string LogName { get; set; }

            public static int StartInsertIndex = 0;



            private void WriteToHtmlLog(string text)
            {

                var file = Path.Combine(LogLogDestination, LogName);

                if (File.Exists(file) == false)
                {
                    CreateHtmlLog(file);
                }
                using (StreamWriter logFile = File.AppendText(file))
                {
                    logFile.WriteLine(text);
                    logFile.Close();
                }
            }


            private static void CreateHtmlLog(string file)
            {
                using (StreamWriter logFile = File.AppendText(file))
                {
                    for (int i = 0; i < htmlLog.Length; i++)
                    {
                        if (htmlLog[i].Contains("<!-- Insert -->"))
                        {
                            StartInsertIndex = i;
                        }
                        logFile.WriteLine(htmlLog[i]);
                    }
                    logFile.Close();
                }
            }



            private void AddLog(HtmlLog log)
            {
                if (log == null) return;
                WriteToHtmlLog(log.ToHtml());

            }
            public void LogError(string message, Exception exception)
            {
                var log = new HtmlLog() { Message = message, LogTime = DateTime.Now, Type = LogType.Error, Exception = exception };
                this.AddLog(log);
            }

            public void Log(string log, string message, LogType logType)
            {
                var htmlLog = new HtmlLog() { Message =  message, LogTime = DateTime.Now, Type = LogType.Warning };
                this.AddLog(htmlLog);
            }




            private static string[] htmlLog = new string[]
                                       {
                                       "<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd'>",
                                       "<html xmlns='http://www.w3.org/1999/xhtml'>",
                                       "<head>",
                                       "<meta http-equiv='Content-Type' content='text/html; charset=utf-8' />",
                                       "<style type='text/css'>",
                                       "body {font: normal 11px auto 'Trebuchet MS', Verdana, Arial, Helvetica, sans-serif;color: #4f6b72;background: #E6EAE9;}",
                                       "a {color: #c75f3e;}",
                                       "caption {padding: 0 0 5px 0;width: 100%;	 font: italic 11px 'Trebuchet MS', Verdana, Arial, Helvetica, sans-serif;text-align: right;}",
                                       "th {font: bold 11px 'Trebuchet MS', Verdana, Arial, Helvetica, sans-serif;color: #4f6b72;border-right: 1px solid #C1DAD7;border-bottom: 1px solid #C1DAD7;border-top: 1px solid #C1DAD7;letter-spacing: 2px;text-transform: uppercase;text-align: left;padding: 6px 6px 6px 12px;}",
                                       "td {border-right: 1px solid #C1DAD7;border-bottom: 1px solid #C1DAD7;background: #fff;padding: 6px 6px 6px 12px;color: #4f6b72;}",
                                       "</style>",
                                       "</head>",
                                       "<body>",
                                       "<table cellspacing='0'>",
                                       "<tr><th>Error Type</th><th>Message</th><th>Date</th><th>Exeption</th><th>Callstack</th></tr>",
                                       "<!-- Insert -->",
                                       };



        }
    }
}
