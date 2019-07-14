using System.Collections.Generic;

namespace Trinity
{
    public class DataContextBase
    {
        public static Dictionary<string, TableMap> TableMaps { get; set; }

        public static List<IDataManager> DataManagers { get; set; }

        public static void AddDataManger(IDataManager radDataManger)
        {
            DataManagers.Add(radDataManger);
        }

        public static void CreateContext()
        {
            if (TableMaps == null)
            {
                TableMaps = new Dictionary<string, TableMap>();
            }
        }



        public TableMap GetTableMap(string name)
        {
            if (DataContextBase.TableMaps.ContainsKey(name))
            {
                return DataContextBase.TableMaps[name];
            }

            return null;
        }


        public void CreateTableMap(string name, TableMap tableMap)
        {
            var existingMap = GetTableMap(name);

            if(existingMap == null)
            {    DataContextBase.TableMaps.Add(name,tableMap);}
            else
            {

                DataContextBase.TableMaps[name] = MergeTableMap(existingMap, tableMap);


            }

        }


        public virtual void CreateLobConfig()
        {
            var application




        }





    






        public TableMap MergeTableMap(TableMap existingMap, TableMap tableMap)
        {
            return existingMap;


             
        }
    }
}
