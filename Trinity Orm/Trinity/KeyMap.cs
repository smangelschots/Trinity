namespace Trinity
{
    public class KeyMap
    {
        public string ParentColumnName { get; set; }
        public KeyMapType KeyType { get; set; }
        public string ReferenceTableName { get; set; }
        public string ReferenceColumnName { get; set; }

        public string PropertyName { get; set; }

        public bool IsMapped { get; set; }

        public KeyMapType GetSqlKeyType(string keyType)
        {
            if (keyType == "FK")
            {
                return KeyMapType.ForeignKey; 
            }
            return KeyMapType.PrimaryKey;
        }
    }
}