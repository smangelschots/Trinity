namespace OfficeSoft.Data.Crud
{
    using System;

    public interface IMapper2 : IMapper
    {
        Func<object, object> GetFromDbConverter(Type DestType, Type SourceType);
    }
}