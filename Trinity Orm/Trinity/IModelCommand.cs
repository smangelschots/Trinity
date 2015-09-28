namespace Trinity
{
    public interface IModelCommand<T>
        where T : class
    {
        IDataCommand<T> Track(T model);

        IDataCommand<T> Insert(T model);

        IDataCommand<T> Update(T model);


        ModelConfiguration<T> ModelConfiguration { get; set; }
        void Remove(int index);
        void ClearCommands();
        ResultList SaveChanges();
    }

   

}
