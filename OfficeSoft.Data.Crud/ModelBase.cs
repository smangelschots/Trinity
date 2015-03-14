using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace OfficeSoft.Data.Crud
{
    public enum EditType
    {
        Begin,
        End,
        Cancel
    }

    public delegate void EditEventHandler(object sender, EditEventHandlerArgs args);

    [Serializable]
    public abstract class ModelBase : IModelBase, IModelConfigurationManager
    {
        [Bindable(false)]
        [Browsable(false)]
        [Ignore]
        public Dictionary<string, object> OldValues { get; set; }

        [Bindable(false)]
        [Browsable(false)]
        [Ignore]
        public Dictionary<string, string> Errors { get; set; }

        [Bindable(false)]
        [Browsable(false)]
        [Ignore]
        public string RowError { get; set; }

        [Bindable(false)]
        [Browsable(false)]
        [Ignore]
        public string Error { get; set; }


        protected ModelBase()
        {
            this.Errors = new Dictionary<string, string>();
            this.OldValues = new Dictionary<string, object>();
        }

        public string this[string columnName]
        {
            get
            {

                if (this.Errors.ContainsKey(columnName))
                    return this.Errors[columnName];
                return string.Empty;
            }
        }

        public bool HasErrors()
        {
            if (this.Errors.Count > 0) return true;
            return false;
        }

        public void ClearErrors()
        {
            this.Errors.Clear();
            this.RowError = string.Empty;
        }

        public virtual void SetColumnError(string columnName, string error)
        {
            if (this.Errors.ContainsKey(columnName) == false)
                this.Errors.Add(columnName, error);
            else
                this.Errors[columnName] = error;
        }

        [field: NonSerialized]
        public event PropertyChangingEventHandler PropertyChanging;

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        [field: NonSerialized]
        public event EditEventHandler EditObject;


        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public virtual void SendPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public virtual void SendPropertyChanging(string propertyName)
        {
            if (this.PropertyChanging != null)
                this.PropertyChanging(this, new PropertyChangingEventArgs(propertyName));

        }

        public void BeginEdit()
        {
            if (this.EditObject != null)
                this.EditObject(this, new EditEventHandlerArgs(EditType.Begin));

        }

        public void EndEdit()
        {
            if (this.EditObject != null)
                this.EditObject(this, new EditEventHandlerArgs(EditType.End));
        }

        public void CancelEdit()
        {
            if (this.EditObject != null)
                this.EditObject(this, new EditEventHandlerArgs(EditType.Cancel));
        }

        public void AcceptChanges()
        {
            this.OldValues.Clear();
        }

        [Bindable(false)]
        [Browsable(false)]
        [Ignore]
        public bool IsChanged { get; private set; }

        public void RejectChanges()
        {
            //TODO Implement ChangeTracking
        }

        [Bindable(false)]
        [Browsable(false)]
        [Ignore]
        public IModelConfiguration Configuration { get; set; }
    }
}