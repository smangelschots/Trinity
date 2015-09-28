using System.ComponentModel;

namespace Trinity
{
    public class ModelCommandPropertyChangedEventArgs : PropertyChangedEventArgs
       
    {
        public ModelCommandPropertyChangedEventArgs(string propertyName)
            : base(propertyName)
        {


        }

        public object Value { get; set; }

        public IDataCommand ModelCommand { get; set; }
    }
}