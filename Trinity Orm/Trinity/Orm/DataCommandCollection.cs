using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Trinity
{
    public abstract class DataCommandCollection<T> : BindingList<T>
        where T : class
    {
        public IModelCommand<T> DataManager { get; set; }
        protected bool _cancelEvents;
        private bool newItemAdded = false;

        public ModelConfiguration<T> ModelConfiguration
        {
            get
            {
                if (DataManager != null) return DataManager.ModelConfiguration;
                return null;

            }
            set
            {
                if (DataManager != null) DataManager.ModelConfiguration = value;
            }
        }


        public event EventHandler<DataCommandCollectionEventArgs> Changing;

        public event EventHandler<DataCommandCollectionEventArgs> AfterModelAddedForInsert;
        public event EventHandler<DataCommandCollectionEventArgs> AfterModelAddedForUpdate;

        public event EventHandler<EventArgs> BeforeSave;
        public event EventHandler<AfterSaveEventArgs> AfterSave;

        protected virtual void OnAfterSave(AfterSaveEventArgs afterSaveEvent)
        {
            EventHandler<AfterSaveEventArgs> handler = this.AfterSave;
            if (handler != null)
            {
                handler(this, afterSaveEvent);
            }
        }

        protected virtual void OnBeforeSave()
        {
            EventHandler<EventArgs> handler = this.BeforeSave;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        protected virtual void OnAfterModelAddedForUpdate(DataCommandCollectionEventArgs e)
        {
            var model = e.NewItem as IModelBase;
            this.MergeModelConfiguration(model);

            EventHandler<DataCommandCollectionEventArgs> handler = this.AfterModelAddedForUpdate;
            if (handler != null)
            {
                handler(this, e);
            }
        }


        private void MergeModelConfiguration(IModelBase model)
        {
            if (ModelConfiguration != null)
            {
                if (model != null)
                {
                    if (model.Configuration == null)
                    {
                        model.Configuration = ModelConfiguration;
                    }
                    else
                    {
                        model.Configuration.MergeModelConfiguration(ModelConfiguration);
                    }
                    model.Configuration.SetModelConfiguration(model);
                }
            }

        }

        protected virtual void OnAfterModelAddedForInsert(DataCommandCollectionEventArgs e)
        {
            var model = e.NewItem as IModelBase;
            this.MergeModelConfiguration(model);
            EventHandler<DataCommandCollectionEventArgs> handler = this.AfterModelAddedForInsert;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public string TableName { get; set; }
        public string[] PrimaryKeys { get; set; }



        public DataCommandCollection(IModelCommand<T> dataManager)
        {
            this.DataManager = dataManager;
        }


        protected override void OnListChanged(ListChangedEventArgs e)
        {

            if(e.NewIndex < 0) return;
            if (_cancelEvents == true)
                return;

            switch (e.ListChangedType)
            {
                case ListChangedType.Reset:
                    return;
                case ListChangedType.ItemAdded:
                    if (newItemAdded == false)
                    {
                        break;
                    }

                    var item = this[e.NewIndex];
                    this.DataManager.Track(item).ForInsert().WithKeys(this.PrimaryKeys).InTo(this.TableName);
                    OnAfterModelAddedForInsert(new DataCommandCollectionEventArgs(ChangeType.Adding, item, null, e.NewIndex, false));
                    break;
                case ListChangedType.ItemDeleted:
                    break;
                case ListChangedType.ItemMoved:
                    break;
                case ListChangedType.ItemChanged:
                    break;
                case ListChangedType.PropertyDescriptorAdded:
                    break;
                case ListChangedType.PropertyDescriptorDeleted:

                    break;
                case ListChangedType.PropertyDescriptorChanged:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            base.OnListChanged(e);

        }





        protected override void InsertItem(int index, T item)
        {
            var eventArgs = new DataCommandCollectionEventArgs(ChangeType.Adding, item, null, index, false);
            EventHandler<DataCommandCollectionEventArgs> temp = this.Changing;
            if (temp != null)
            {
                temp(this, eventArgs);
            }
            if (eventArgs.Cancel)
                return;

            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, T item)
        {
            var eventArgs = new DataCommandCollectionEventArgs(ChangeType.Replacing, item, null, index, false);
            EventHandler<DataCommandCollectionEventArgs> temp = this.Changing;
            if (temp != null)
            {
                temp(this, eventArgs);
            }

            if (eventArgs.Cancel)
                return;

            base.SetItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            var item = this[index];
            var eventArgs = new DataCommandCollectionEventArgs(ChangeType.Removing, item, null, index, false);
            EventHandler<DataCommandCollectionEventArgs> temp = this.Changing;
            if (temp != null)
            {
                temp(this, eventArgs);
            }

            if (eventArgs.Cancel)
                return;


            this.DataManager.Remove(index);
            base.RemoveItem(index);
        }

        protected override void ClearItems()
        {
            var eventArgs = new DataCommandCollectionEventArgs(ChangeType.BeforeClear, null, null, null, false);
            EventHandler<DataCommandCollectionEventArgs> temp = this.Changing;
            if (temp != null)
            {
                temp(this, eventArgs);
            }

            if (eventArgs.Cancel)
                return;


            base.ClearItems();
        }

        protected override void OnAddingNew(AddingNewEventArgs e)
        {
            base.OnAddingNew(e);
            this.newItemAdded = true;
        }


        public void Refresh(IList<T> items)
        {
            this.Clear();
            this.SaveChanges();
            this.DataManager.ClearCommands();
            newItemAdded = false;
            this.AddRange(items);
        }

        public void AddRange(IList<T> list)
        {
            this.DataManager.ClearCommands();
            foreach (var item in list)
            {
                this.DataManager.Track(item)
                    .ForUpdate()
                    .WithKeys(this.PrimaryKeys)
                    .From(this.TableName);
                newItemAdded = false;
                _cancelEvents = true;
                base.Add(item);
                this.OnAfterModelAddedForUpdate(new DataCommandCollectionEventArgs(ChangeType.Adding, item, null, 0, false));
                _cancelEvents = false;

            }
        }

        public void AddRangeForInsert(IList<T> list)
        {
            this.DataManager.ClearCommands();
            foreach (var item in list)
            {
               AddForInsert(item, false);
            }
        }
        /// <summary>
        /// Add existing database model to datamanger for update
        /// DO NOT USE FOR INSERT IF YOU DONT HAVE A ID
        /// </summary>
        /// <param name="model"></param>
        /// 
        public new void Add(T model)
        {
           AddForUpdate(model);
        }


        public new void Insert(int index, T model)
        {
            this.DataManager
            .Track(model)
            .ForUpdate()
            .WithKeys(this.PrimaryKeys)
            .From(this.TableName);

            newItemAdded = false;
            _cancelEvents = true;
            base.Insert(index, model);
            OnAfterModelAddedForUpdate(new DataCommandCollectionEventArgs(ChangeType.Adding, model, null, 0, false));
            _cancelEvents = false;
        }

        public void InsertForInsert(int index, T model)
        {
            this.DataManager
            .Track(model)
            .ForInsert()
            .WithKeys(this.PrimaryKeys)
            .From(this.TableName);

            newItemAdded = false;
            _cancelEvents = true;
            base.Insert(index, model);
            OnAfterModelAddedForUpdate(new DataCommandCollectionEventArgs(ChangeType.Adding, model, null, 0, false));
            _cancelEvents = false;
        }

        public void AddForInsert(T model, bool triggerEvents = true)
        {
            this.DataManager
              .Track(model)
              .ForInsert()
              .WithKeys(this.PrimaryKeys)
              .From(this.TableName);

            newItemAdded = false;
            _cancelEvents = true;
            base.Add(model);
            OnAfterModelAddedForInsert(new DataCommandCollectionEventArgs(ChangeType.Adding, model, null, 0, false));
            _cancelEvents = false;
        }

        public void AddForUpdate(T model)
        {
            this.DataManager
            .Track(model)
            .ForUpdate()
            .WithKeys(this.PrimaryKeys)
            .From(this.TableName);

            newItemAdded = false;
            _cancelEvents = true;
            base.Add(model);
            OnAfterModelAddedForUpdate(new DataCommandCollectionEventArgs(ChangeType.Adding, model, null, 0, false));
            _cancelEvents = false;
        }

        public virtual ResultList SaveChanges()
        {
         
            OnBeforeSave();
            var result = new AfterSaveEventArgs();
            var resultList= this.DataManager.SaveChanges();
            result.Results = resultList;
            OnAfterSave(result);
            return resultList;
        }

        public int GetChanges()
        {

            return 0;
        }

        /// <summary>
        /// Add new database model to datamanger for insert
        /// </summary>
        /// <returns></returns>
        public new T AddNew()
        {
            var newItem = base.AddNew();
            OnAfterModelAddedForInsert(new DataCommandCollectionEventArgs(ChangeType.Adding, newItem, null, 0, false));
            return newItem;
        }

    }
}