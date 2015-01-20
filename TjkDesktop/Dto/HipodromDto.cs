using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TjkDesktop.App_Data;

namespace TjkDesktop.Dto
{
    class HipodromDto
    {
        public int id { set; get; }
        public string name { set; get; }
        public string yarisProgramiUrl { set; get; }
        public string yarisSonuclariUrl { set; get; }
        public DateTime tarih { set; get; }
        public List<KosuDto> kosular { set; get; }

    }
}
