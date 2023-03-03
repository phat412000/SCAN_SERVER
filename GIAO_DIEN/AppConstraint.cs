using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIAO_DIEN
{
    public class AppConstraint
    {
        public static string THRESH_OUTPUT_IMAGE = "threshImage.jpg";

        public static string NO_IMAGE = "NO_IMAGE";

        public static string getAbsolutePath(string file)
        {
            return Directory.GetCurrentDirectory() + "\\" + file;
        }
    }
}
