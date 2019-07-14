using System;

namespace Trinity
{
    public class DataCommandCollectionEventArgs : EventArgs
    {
        public ChangeType ChangeType { get; set; }
        public object OldItem { get; set; }
        public int? Index { get; set; }
        public bool Cancel { get; set; }
        public object NewItem { get; private set; }

        public DataCommandCollectionEventArgs(ChangeType changeType, object newItem, object oldItem, int? index, bool cancel)
        {
            this.ChangeType = changeType;
            this.OldItem = oldItem;
            this.Index = index;
            this.Cancel = cancel;
            this.NewItem = newItem;
        }
    }
}