using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySql.Data.MySqlClient;
using Trinity.MySql;

namespace Trinity.Test
{
    [TestClass]
    public class MySqlUnitTest
    {
        public string connectionString = "Server=localhost;Database=test;Uid=root;Pwd=Service01;";

        private MySqlDataContext context ;

        [TestMethod]
        public void SelectTest()
        {

            var manager = new MySqlDataManager<Adress>(connectionString);
            var items = manager.Select().All().ExecuteToList();

        }




        [TestMethod]
        public void Insert()
        {
            
        }


        [TestMethod]
        public void StandardTest()
        {
            MySqlConnection conn;

            conn = new MySqlConnection(connectionString);

            conn.Open();

        }


        public class Adress
        {
            public int Id { get; set; }
            public string Name { get; set; }


        }
    }
}
