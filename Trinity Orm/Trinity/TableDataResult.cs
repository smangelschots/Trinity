using System;
using System.Collections.Generic;
using System.Data;

namespace Trinity
{
    public class TableDataResult : ICommandResult
    {
        public string Name { get; set; }
        public IDataCommand DataCommand { get; set; }

        public int RecordsAffected { get; set; }

        public bool HasErrors { get; private set; }

        public DataCommandType CommandType { get; set; }

        public List<DataError> CommandErrors { get; set; }

        public void AddError(ErrorType errorType, string message, Exception exception = null)
        {

            //TODO implement error management 
        }

        public List<string> Messages { get; set; }

        public void AddMessage(string message)
        {
            //TODO implement message management 
        }

        public DataTable Table { get; set; }    

    }
}