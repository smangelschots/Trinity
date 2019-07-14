using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trinity.Lob
{

    [TableConfiguration("Z_Application")]
    public class ApplicationModel
    {
        public Guid Id { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }




    }
}
