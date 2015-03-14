using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OfficeSoft.Data.Crud
{
    using System.ComponentModel;

    public interface IModelBase : INotifyPropertyChanging, INotifyPropertyChanged, IEditableObject, IDataErrorInfo, IRevertibleChangeTracking
    {
        new event PropertyChangingEventHandler PropertyChanging;

        new event PropertyChangedEventHandler PropertyChanged;


        void SetColumnError(string columnName, string error);

        void SendPropertyChanging(string propertyName);

        void SendPropertyChanged(string propertyName);


        Dictionary<string, string> Errors { get; set; }
        Dictionary<string, object> OldValues { get; set; }

        IModelConfiguration Configuration { get; set; }


        bool HasErrors();
        void ClearErrors();
    }
}
