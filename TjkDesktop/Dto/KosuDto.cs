using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TjkDesktop.Dto
{
    class KosuDto
    {
        public decimal kod { set; get; }
        public int hipodromId { set; get; }
        public decimal? no { set; get; }
        public DateTime tarih { set; get; }
        public string saat { set; get; }
        public string groupAdi { set; get; }
        public string sehir { set; get; }
        public string son800 { set; get; }
        public List<HorseDto> horseList { set; get; }

    }
}
