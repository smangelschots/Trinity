# Trinity ORM
Start V1 alfa


Trinity is a flexible data access tool.


## Installation

https://www.nuget.org/packages/Trinityorm/


## Features

The ORM can handle SQL server, OleDb (access) and MySql...

## Docs & Community

## Quick Start

### SqlServer

1. Create a database "trinitytest"
```bash
USE [TrinityTest]
GO

/****** Object:  Table [dbo].[Country]    Script Date: 14/03/2015 15:33:35 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Country](
	[CountryId] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NULL,
 CONSTRAINT [PK_Country] PRIMARY KEY CLUSTERED 
(
	[CountryId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
```

2. Create a unit test project
3. Add nuget package
```bash  
NuGet: PM> Install-Package Trinityorm 
```
4. copy code to testclass
```bash  
   public class Country
    {
        public int CountryId { get; set; }
        public string Name { get; set; }

    }


    [TestClass]
    public class SqlUnitTest
    {

        private string _connectionstring =
            "Data Source=localhost;Initial Catalog=TrinityTest;User Id=[usernam];Password=[password]";


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
        }

        [TestMethod]
        public void DelteTestMethod()
        {
            var db =
                new SqlServerDataManager<Country>(
                    _connectionstring);

            db.Delete().Where(m => m.Name == "Belgium");
            var result = db.SaveChanges();  
        }



        [TestMethod]
        public void SelectTestMethod()
        {
            //This creates as select statement 
            var db =
                new SqlServerDataManager<Country>(
                    _connectionstring);

            var items = db.Select().ExecuteToList();

            var item = db.Select().Where(m => m.Name == "Belgium - BE").FirstOrDefault();

            var itemsAnd = db.Select().Where(m => m.Name == "Belgium").And(m => m.Name == "Netherlands").OrderBy("Name").ExecuteToList();

            var itemsor = db.Select().Where(m => m.Name == "Belgium").Or(m => m.Name == "Netherlands").ExecuteToList();


        }
```








