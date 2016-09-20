using System;
using System.Collections.Generic;

namespace Trinity
{
    public interface ICommandResult
    {
        string Name { get; set; }
        IDataCommand DataCommand { get; set; }
        int RecordsAffected { get; set; }
        bool HasErrors { get; }
        DataCommandType CommandType { get; set; }
        List<DataError> CommandErrors { get; set; }
        void AddError(ErrorType errorType, string message, Exception exception = null);
        List<string> Messages { get; set; }
        void AddMessage(string message);


    }
}