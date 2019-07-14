using System;

namespace Trinity
{
    public class ModelCommandValidationEventArgs<T> : EventArgs
        where T : class
    {
        public IDataCommand<T> ModelCommand { get; set; }
    }
}