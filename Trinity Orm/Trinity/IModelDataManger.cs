namespace Trinity
{
    public interface IModelDataManger<T>
        where T : class
    {


        IDataCommand<T> Update(T model);

        IDataCommand<T> Insert(T model);

        IDataCommand<T> Track(T model);

       
    }
}