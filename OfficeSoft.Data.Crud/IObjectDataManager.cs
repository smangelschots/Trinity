using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace OfficeSoft.Data.Crud
{
    public interface IObjectDataManager
    {

        void SetData(IDataReader reader);
    }
}
