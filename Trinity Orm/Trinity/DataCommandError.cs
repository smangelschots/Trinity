
using System;

namespace Trinity
{
    public class DataError
    {
        public bool HasError { get; set; }

        public string StackTrace { get; set; }
        public Exception Exception { get; set; }

        public LogType ErrorType { get; set; }

        public string Message { get; set; }
    }
}
