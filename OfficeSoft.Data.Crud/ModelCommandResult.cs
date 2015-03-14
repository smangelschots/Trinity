namespace OfficeSoft.Data.Crud
{
    using System.Collections.Generic;

    public class ModelCommandResult<T> : CommandResult where T : class
    {
        public IEnumerable<T> Data { get; set; }

        public string ToJson()
        {
            return null;
        }


    }
}