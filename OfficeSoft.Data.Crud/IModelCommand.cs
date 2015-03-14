using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OfficeSoft.Data.Crud
{
    public interface IModelCommand<T>
        where T : class
    {
        IDataCommand<T> Track(T model);

        IDataCommand<T> Insert(T model);

        IDataCommand<T> Update(T model);


        ModelConfiguration<T> ModelConfiguration { get; set; }
    }

   

}
