using System.Collections.Generic;

namespace Trinity
{
    public class ModelCommandResult<T> : CommandResult where T : class
    {
        public IEnumerable<T> Data { get; set; }

        public string ToJson()
        {
            return null;
        }


    }
}