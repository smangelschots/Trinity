using System;

namespace Trinity
{
    public class LoggingEventArgs
    {
        public string Message { get; set; }
        public string Log { get; set; }

        public LogType LogType { get; set; }

        public string UserName { get; set; }


    }
}