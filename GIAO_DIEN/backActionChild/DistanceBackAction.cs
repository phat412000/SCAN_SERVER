using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIAO_DIEN.backActionChild
{
    class DistanceBackAction : BackAction
    {
        public double value { get; set; }

        public override string ToString()
        {
            return $"Distance {value}";
        }
    }
}
