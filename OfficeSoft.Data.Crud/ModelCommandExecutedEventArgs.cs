namespace OfficeSoft.Data.Crud
{
    using System;

    public class ModelCommandExecutedEventArgs<T> : EventArgs
        where T : class
    {
        public ModelCommandResult<T> Result { get; set; }
    }
}