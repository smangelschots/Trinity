namespace OfficeSoft.Data.Crud
{
    using System.Collections.Generic;
    using System.Linq;

    public class TableMap
    {
        public TableMap()
        {
            this.ColumnMaps = new List<IColumnMap>();
            this.KeyMaps = new List<KeyMap>();
        }

        public string Schema { get; set; }
        public string TableName { get; set; }
        public string TableType { get; set; }
        public string Catalog { get; set; }
        public List<IColumnMap> ColumnMaps { get; set; }
        public List<KeyMap> KeyMaps { get; set; }
        public int Id { get; set; }

        public List<string> GetPrimaryKeys()
        {

            var items = new List<string>();

            foreach (var item in this.KeyMaps)
            {
                if (item.KeyType == KeyMapType.PrimaryKey)
                {
                    items.Add(item.PropertyName);
                }


            }
            return items;
        }
    }
}