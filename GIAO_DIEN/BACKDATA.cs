using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIAO_DIEN
{
    class BACKDATA

    {
        public double values;
        public string labels;

        public BACKDATA(string mystackLabel, double mystackValue)
        {
            labels = mystackLabel;
            values = mystackValue;
        }

        public override string ToString()
        {
            return labels + " " + values;
        }
    }
}
