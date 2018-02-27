namespace Trinity.MySql
{
    public class MySqlDataContext
    {
        private readonly string _connectionsString;
        private readonly string _providerName;

        public MySqlDataContext(string connectionsString, string providerName)
        {
            _connectionsString = connectionsString;
            _providerName = providerName;
        }

        public void GetTableMaps()
        {
        }
    }
}
