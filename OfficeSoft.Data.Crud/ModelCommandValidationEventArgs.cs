namespace OfficeSoft.Data.Crud
{
    using System;

    public class ModelCommandValidationEventArgs<T> : EventArgs
        where T : class
    {
        public IDataCommand<T> ModelCommand { get; set; }
    }
}