using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIAO_DIEN
{

    public delegate void FireEventForScanSuccess(object sender, ScanSuccess e);
    public class ScanSuccess : EventArgs
    {
        public string Barcode { get; set; }
        public string DateTime { get; set; }

    }
    //public static DateTime datetime;


}
