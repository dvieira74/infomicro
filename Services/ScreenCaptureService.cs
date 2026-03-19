using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Infomicro.Services
{
    public class ScreenCaptureService
    {
        private readonly ImageCodecInfo _jpegEncoder;
        private readonly EncoderParameters _encoderParameters;

        public ScreenCaptureService(long quality = 40L)
        {
            _jpegEncoder = GetEncoder(ImageFormat.Jpeg);
            _encoderParameters = new EncoderParameters(1);
            _encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
        }

        public byte[] CaptureScreenAndCompress()
        {
            try
            {
                var bounds = Screen.PrimaryScreen.Bounds;
                using (var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format16bppRgb555))
                {
                    using (var graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
                    }

                    using (var memoryStream = new MemoryStream())
                    {
                        bitmap.Save(memoryStream, _jpegEncoder, _encoderParameters);
                        return memoryStream.ToArray();
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                    return codec;
            }
            return null;
        }
    }
}
