using System;

namespace Trinity
{
    public class ModelCommandExecutedEventArgs<T> : EventArgs
        where T : class
    {
        public ModelCommandResult<T> Result { get; set; }
    }
}