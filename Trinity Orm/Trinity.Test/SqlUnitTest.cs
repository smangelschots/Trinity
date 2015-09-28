using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trinity.MsSql;

namespace Trinity.Test
{



    public class Country
    {
        public int CountryId { get; set; }
        public string Name { get; set; }

    }


    [TestClass]
    public class SqlUnitTest
    {

        private static string username = "admin";
        private static string password = "Service01";


        private string _connectionstring =
            $"Data Source=localhost;Initial Catalog=TrinityTest;User Id={username};Password={password}";



        [TestMethod]
        public void InsertTestMethod()
        {



            var db =
                new SqlServerDataManager<Country>(
                    _connectionstring);

            db.Insert(new Country()
            {
                Name = "Belgium"
            });

            db.Insert(new Country()
            {
                Name = "Netherlands"
            });

            var result = db.SaveChanges();

            Assert.IsFalse(result.HasErrors());
        }


        [TestMethod]
        public void UpdateTestMethod()
        {
            var db =
                new SqlServerDataManager<Country>(
                    _connectionstring);


            var item = db.Select().Where(m => m.Name == "Belgium").FirstOrDefault();

            item.Name = "Belgium - BE";

            db.Update(item);
            var result = db.SaveChanges();

            Assert.IsFalse(result.HasErrors());
        }

        [TestMethod]
        public void DeleteTestMethod()
        {
            var db =
                new SqlServerDataManager<Country>(
                    _connectionstring);

            db.Delete().Where(m => m.Name == "Belgium");
            db.Delete().Where(m => m.Name == "Netherlands");
            var result = db.SaveChanges();
            Assert.IsFalse(result.HasErrors());
        }



        [TestMethod]
        public void SelectTestMethod()
        {
            //This creates as select statement 
            var db =
                new SqlServerDataManager<Country>(
                    _connectionstring);

            var items = db.Select().ExecuteToList();

            Assert.IsTrue(items.Count > 0);

            var item = db.Select().Where(m => m.Name == "Belgium - BE").FirstOrDefault();

            Assert.IsTrue(item != null);

            var itemsAnd = db.Select().Where(m => m.Name == "Belgium").And(m => m.Name == "Netherlands").OrderBy("Name").ExecuteToList();

            Assert.IsTrue(itemsAnd.Count > 0);


            var itemsor = db.Select().Where(m => m.Name == "Belgium").Or(m => m.Name == "Netherlands").ExecuteToList();

            Assert.IsTrue(itemsor.Count > 0);

        }
    }
}
