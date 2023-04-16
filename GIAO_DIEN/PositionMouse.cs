using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLORY_TO_GOD
{
    class PositionMouse
    {
        public int posx { get; set; }
        public int posy { get; set; }

        public PositionMouse(int x, int y)
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
