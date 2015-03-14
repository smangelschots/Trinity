namespace OfficeSoft.Data.Crud
{
    public class DataBindingList<T> : DataCommandCollection<T> 
        where T : class
    {

        public DataBindingList(string connectionString)
            : base(new SqlServerDataManager<T>(connectionString))
        {

        }

        public DataBindingList(string connectionString, string tableName, string[] primaryKeys):base(new SqlServerDataManager<T>(connectionString)) 
        {
            this.TableName = tableName;
            this.PrimaryKeys = primaryKeys;
        }

    }
}
