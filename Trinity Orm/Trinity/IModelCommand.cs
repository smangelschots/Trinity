using System.Collections.Generic;

namespace Trinity
{
    public interface IModelCommand<T>
        where T : class
    {

        bool TableMapFromDatabase { get; set; }

        IDataCommand<T> Track(T model);

        IDataCommand<T> Insert(T model);

        IDataCommand<T> Update(T model);



        ModelConfiguration<T> ModelConfiguration { get; set; }
        void Remove(int index);
        void Remove(IDataCommand command);
        void ClearCommands();

        IEnumerable<IDataCommand> GetCommands();

        IDataCommand GetCommand(int index);


        ResultList SaveChanges();


    }



}
