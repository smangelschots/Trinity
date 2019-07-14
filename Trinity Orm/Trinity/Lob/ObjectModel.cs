using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trinity.Lob
{

    [TableConfiguration("Z_Object")]
    public class ObjectModel
    {



        public Guid Id { get; set; }

        public Guid ApplicationId { get; set; }


        public string Name { get; set; }

        public string ObjectType { get; set; }



        public string Document { get; set; }


     






    }
}
