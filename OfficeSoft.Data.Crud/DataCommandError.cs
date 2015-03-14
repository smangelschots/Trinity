
namespace OfficeSoft.Data.Crud
{
    using System;

    public class DataError
    {
        public bool HasError { get; set; }

        public string StackTrace { get; set; }
        public Exception Exception { get; set; }

        public ErrorType ErrorType { get; set; }

        public string Message { get; set; }
    }
}
