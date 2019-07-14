using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trinity.MsSql;

namespace Trinity.Test
{



    public class Country : ModelBase
    {
        private int _countryId;
        private string _name;

        public Country()
        {
            var config = new ModelConfiguration<Country>();
            config.SetModelConfiguration(this);
            config.SetRequired(m => m.Name, "test", ModelConfiguration.NotNullExpression);
            config.AfterModelPropertyValidate += (s, e) =>
            {
                
            };

            this.Configuration = config;
        }


        public int CountryId
        {
            get { return _countryId; }
            set
            {
                if (_countryId != value)
                {
                    _countryId = value;
                    SendPropertyChanged("CountryId");
                }
               
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    SendPropertyChanged("Name");
                }
            }
        }

        public override List<string> GetProperties()
        {
            throw new NotImplementedException();
        }
    }


    [TestClass]
    public class SqlUnitTest
    {




        private string _connectionstring =
            "Server=localhost;Database=Trinity;Trusted_Connection=yes; Application Name=Trinity;";



        public void GetTableMap()
        {



        }




        [TestMethod]
        public void CreateUpdateModelTest()
        {

            var context = new SqlServerDataContext();

            context.CreateTableMap("Country",new TableMap()
            {
                TableName = "Country",
                Catalog = ""
            });








        }






        [TestMethod]
        public void InsertTestMethod()
        {

            SqlServerDataContext.CreateContext();


            var db =
                new SqlServerDataManager<Country>(
                    _connectionstring);

            db.Insert(new Country()
            {
                CountryId =  1,
                Name = "Belgium"
            });

            db.Insert(new Country()
            {
                CountryId = 2,
                Name = "Netherlands"
            });

            var result = db.SaveChanges();

            Assert.IsFalse(result.HasErrors);
        }




        [TestMethod]
        public void UpdateTestMethod()
        {
            var db =
                new SqlServerDataManager<Country>(
                    _connectionstring);
            var item = db.Select().Where(m => m.CountryId == 1).FirstOrDefault();
            item.Name = "";
            db.Update(item);

            var result = db.SaveChanges();

            Assert.IsFalse(result.HasErrors,result.Error);
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
            Assert.IsFalse(result.HasErrors);
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
