using System;

namespace Trinity
{
    public class LoggingEventArgs
    {
        public string Message { get; set; }
        public string Log { get; set; }

        public LogType LogType { get; set; }

        public string UserName { get; set; }


        public override string ToString()
        {
            return $"{DateTime.Now} - {Log} - {LogType.ToString()} - {Message} - {UserName}";
        }
    }
}