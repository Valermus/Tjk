using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TjkDesktop.Responses
{
    class ResponseHorseDetailSet
    {
        public Exception error { set; get; }
        public List<String> failedToRetrieveIds { set; get; }
    }
}
