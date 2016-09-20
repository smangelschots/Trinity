using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trinity.Ole;

namespace Trinity.Test
{
    [TestClass]
    public class AccessUnitTest
    {
        [TestMethod]
        public void  AccessTestMethod()
        {
            var templateAccess = Path.Combine(@"C:\Temp", "adressen.accdb");
            var conn = string.Format("Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};", templateAccess);
            var dbAccess = new OleDataManager<AdressenAccess>(conn);

            dbAccess.Delete().All().From("Office_Address_List");
            var items1 = dbAccess.SaveChanges();
            for (int i = 0; i < 5; i++)
            {
                dbAccess.Insert(new AdressenAccess()
                {
                    Adres = $"Adres {i}",
                    Faxnummer = $"Faxnummer {i}",
                    Gemeente = $"Gemeente {i}",
                    ID = i,
                    Organisatie = $"Organisatie {i}",
                    TelefoonWerk = $"TelefoonWerk {i}"
                }).InTo("Office_Address_List");
            }

            var items = dbAccess.SaveChanges();

        }
    }

    public class AdressenAccess
    {
        public string Organisatie { get; set; }

        public string Adres { get; set; }

        public string Gemeente { get; set; }

        public string TelefoonWerk { get; set; }

        public string Faxnummer { get; set; }

        public int ID { get; set; }

    }
}
