using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Trinity
{
    public interface IDataManager<T>  : IDataManager  where T   : IDataCommand
    {

        T Insert();

        T Update();

    }

    public interface IDataManager
    {
        IDbConnection Connection { get; }


        bool TableMapFromDatabase { get; set; }

        Dictionary<string, TableMap> TableMaps { get; set; }

        string ConnectionString { get; set; }
        string DatabaseName { get; }

        IDataCommand GetCommand(int index);
        void Remove(int index);

        void ClearCommands();
        ResultList SaveChanges();

        void Remove(IDataCommand command);
        ICommandResult ExecuteCommand(IDataCommand dataCommand);

        Task<ICommandResult> ExecuteCommandAsync(IDataCommand dataCommand);
    }

}