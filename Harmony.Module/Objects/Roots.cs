using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harmony.Module.Objects
{
    internal class PlayerRoot
    {
        public int StatusCode { get; set; }
        public Player[] Data { get; set; }
    }

    internal class ImpoundRoot
    {
        public int StatusCode { get; set; }
        public ImpoundLog[] Data { get; set; }
    }
}
