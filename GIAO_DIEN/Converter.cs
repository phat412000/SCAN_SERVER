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

namespace GIAO_DIEN
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


    }
}
