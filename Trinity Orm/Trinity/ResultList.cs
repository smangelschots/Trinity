using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Trinity
{
    public class ResultList : List<ICommandResult>
    {

        public event SentCommandResultMessageEvent SentCommandResultMessage;
        public delegate void SentCommandResultMessageEvent(object sender, SentCommandResultMessageEventArgs args);

        public class SentCommandResultMessageEventArgs
        {
            public CommandResult Result { get; }

            public SentCommandResultMessageEventArgs(CommandResult result)
            {
                Result = result;
            }
        }


        private string GetError()
        {
            var temString = string.Empty;
            var errors = this.Where(m => m.HasErrors);

            string result = temString;
            foreach (ICommandResult dataResult in errors)
            {
                foreach (DataError error in dataResult.CommandErrors)
                {
                    if (error.Exception != null)
                        result += string.Format("{0} {1} {2} {3}", error.ErrorType.ToEnumValue<ErrorType>(string.Empty), error.Message, error.Exception.Message, Environment.NewLine);
                    else
                    {
                        result += string.Format("{0} {1} {2}", error.ErrorType.ToEnumValue<ErrorType>(string.Empty), error.Message, Environment.NewLine);
                    }
                }
            }
            return result;
        }



        public string Error
        {
            get { return GetError(); }
        }




        public int GetRecordsAffected()
        {
            int recordsAffectedCount = 0;
            foreach (var command in this)
            {
                recordsAffectedCount += command.RecordsAffected;
            }

            return recordsAffectedCount;
        }


        public List<ICommandResult> AffectedCommands
        {
            get
            {
                return this.Where(m => m.RecordsAffected > 0).ToList(); ;
            }
        }

        public List<ICommandResult> ErrorCommands
        {
            get { return this.Where(m => m.HasErrors).ToList(); }
        }

        public int AffectedRecords
        {
            get { return GetRecordsAffected(); }
        }

        public int Errors
        {
            get { return this.Count(m => m.HasErrors); }
        }


        public bool HasErrors
        {
            get { return this.Any(m => m.HasErrors); }
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
            OnSentCommandResultMessage(new SentCommandResultMessageEventArgs(result));


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
            OnSentCommandResultMessage(new SentCommandResultMessageEventArgs(result));

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
            OnSentCommandResultMessage(new SentCommandResultMessageEventArgs(result));

            this.Add(result);

            return this;
        }

        protected virtual void OnSentCommandResultMessage(SentCommandResultMessageEventArgs args)
        {
            SentCommandResultMessage?.Invoke(this, args);

        }
    }


}