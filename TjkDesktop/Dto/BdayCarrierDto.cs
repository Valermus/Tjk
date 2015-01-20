using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TjkDesktop.Dto
{
    class BdayCarrierDto
    {
        public DateTime birthDay { set; get; }
        public List<HorseInfoDto> horseInfoList { set; get; }
        public bool isFailed { set; get; }
    }
}
