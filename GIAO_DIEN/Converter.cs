using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenCvSharp;

namespace GLORY_TO_GOD
{
    class Converter
    {
        public static BitmapImage MatToBitmapImage(Mat image)
        {
            var Bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image);
            
                using (var memory = new MemoryStream())
                {
                    Bitmap.Save(memory, ImageFormat.Png);
                    memory.Position = 0;

                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    return bitmapImage;
                }
            
        }

        public static Mat bytesToMat(byte[] buffer)
        {
            Mat mat = Cv2.ImDecode(buffer, ImreadModes.Color);
            return mat; 
        }
        public static byte[] ImageSourceToBytes(ImageSource source)
        {
            var bitmapImage = source as BitmapImage;

            byte[] data;

            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            BitmapFrame bitmapFrame = BitmapFrame.Create(bitmapImage);

            encoder.Frames.Add(bitmapFrame);

            using (MemoryStream memory = new MemoryStream())
            {
                encoder.Save(memory);
                data = memory.ToArray();
            }

            return data;
        }

        //mang[object(x, y), object(x1,y1)]
        public static List<BacteriaCenter> StringToBacteriaCenters(string data)
        {
            List<BacteriaCenter> bacteriaCenters = new List<BacteriaCenter>();

            string[] splitedData = data.Split(',');

            //i =0
            //i = 2
            //i = 4
            for(long i =  0; i < splitedData.Length; i+= 2)
            {
                int x = int.Parse(splitedData[i]);
                int y = int.Parse(splitedData[i + 1]);

                var center = new BacteriaCenter(x, y);
                bacteriaCenters.Add(center);
            }

            return bacteriaCenters;
        }

    }
}
