using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIAO_DIEN
{
    class PolyPosition
    {
        public double posx { get; set; }
        public double posy { get; set; }

        public PolyPosition(double x, double y)
        {
            posx = x;
            posy = y;
        }
        public override string ToString()
        {
            return "("+posx + "," + posy+")";
        }
    }
}
