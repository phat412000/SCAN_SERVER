using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLORY_TO_GOD
{
    public class AppConstraint
    {
       
        public static string SEGMENT_OUTPUT_IMAGE = "segmentImage.jpg";
        public static string getAbsolutePath(string file)
        {
            return Directory.GetCurrentDirectory() + "\\" + file;
        }
    }
}
