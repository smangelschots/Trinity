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
                        result += string.Format("{0} {1} {2} {3}", error.ErrorType.ToEnumValue<LogType>(string.Empty), error.Message, error.Exception.Message, Environment.NewLine);
                    else
                    {
                        result += string.Format("{0} {1} {2}", error.ErrorType.ToEnumValue<LogType>(string.Empty), error.Message, Environment.NewLine);
                    }
                }
            }
            return result;
        }


        private string GetInformation()
        {
            var temString = string.Empty;
            var infos = this.Where(m => m.HasErrors == false);


            string result = temString;
            int rows = 0;
            foreach (ICommandResult dataResult in infos)
            {
                rows +=  dataResult.RecordsAffected;
               
                foreach (var message in dataResult.Messages)
                {
                    result += string.Format($"{message} {Environment.NewLine}");
                }

            }
            result += $"**** totaal rows {rows} *****";

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
                ErrorType = LogType.Error,
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
                ErrorType = LogType.Information,
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
                ErrorType = LogType.Warning,
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

        public override string ToString()
        {
            return GetInformation();
        }
    }


}