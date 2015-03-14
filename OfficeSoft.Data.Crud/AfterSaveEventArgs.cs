using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OfficeSoft.Data.Crud
{
    public class AfterSaveEventArgs  : EventArgs
    {


        public ResultList Results { get; set; }

    }
}
