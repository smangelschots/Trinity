namespace OfficeSoft.Data.Crud
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ResultList : List<ICommandResult>
    {


        public string GetErrorString()
        {
            var temString = string.Empty;
            var errors = this.Where(m => m.HasErrors);

            string result = temString;
            foreach (ICommandResult dataResult in errors)
            {
                foreach (DataError error in dataResult.CommandErrors)
                {
                    if (error.Exception != null)
                        result += string.Format("{0} {1} {2} {3}",  error.ErrorType.ToEnumValue<ErrorType>(string.Empty) , error.Message ,  error.Exception.Message , Environment.NewLine);
                    else
                    {
                        result += string.Format("{0} {1} {2}", error.ErrorType.ToEnumValue<ErrorType>(string.Empty), error.Message, Environment.NewLine);
                    }
                }
            }
            return result;
       }
        

        public bool HasErrors()
        {
            return this.Any(m => m.HasErrors);
        }

        public ResultList CreateError(string message)
        {
            var result = new CommandResult();

            result.CommandErrors.Add(new DataError()
            {
                ErrorType = ErrorType.Error,
                HasError = true,
                Message = message
            });

            this.Add(result);

            return this;
        }
        public ResultList CreateInformation(string message)
        {
            var result = new CommandResult();

            result.CommandErrors.Add(new DataError()
            {
                ErrorType = ErrorType.Information,
                HasError = true,
                Message = message
            });

            this.Add(result);

            return this;
        }
        public ResultList CreateWarning(string message)
        {
            var result = new CommandResult();

            result.CommandErrors.Add(new DataError()
            {
                ErrorType = ErrorType.Warning,
                HasError = true,
                Message = message
            });

            this.Add(result);

            return this;
        }
    }
}