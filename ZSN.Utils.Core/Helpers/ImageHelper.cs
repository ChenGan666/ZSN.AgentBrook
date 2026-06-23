using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace ZSN.Utils.Core.Helpers
{
    public static class ImageHelper
    {
        /// <summary>
        ///     图像文件的类型
        /// </summary>
        public enum ImageType
        {
            None = 0,
            BMP = 0x4D42,
            JPG = 0xD8FF,
            GIF = 0x4947,
            PCX = 0x050A,
            PNG = 0x5089,
            PSD = 0x4238,
            RAS = 0xA659,
            SGI = 0xDA01,
            TIFF = 0x4949
        }

        /// <summary>
        ///     生成缩略图的模式
        /// </summary>
        public enum ThumbnailMode
        {
            HW = 0, // 指定高宽缩放（可能变形） 
            W = 1, // 指定宽，高按比例    
            H = 2, // 指定高，宽按比例
            Cut = 3 // 指定高宽裁减（不变形） 
        }

        private static SortedDictionary<int, ImageType> _imageTag;

        public static string Png32ToJPEG(string imagePath, int jpgQuality)
        {
            if (File.Exists(imagePath))
            {
                var jpgPath = imagePath.ToLower().Replace(".png", ".jpg");
                using (var bitmap = Image.FromFile(imagePath))
                {
                    // 以下代码为保存图片时，设置压缩质量
                    var encoderParams = new EncoderParameters();
                    var quality = new long[1];
                    quality[0] = jpgQuality;
                    var encoderParam = new EncoderParameter(Encoder.Quality, quality);
                    encoderParams.Param[0] = encoderParam;
                    //获得包含有关内置图像编码解码器的信息的ImageCodecInfo 对象。
                    var arrayICI = ImageCodecInfo.GetImageEncoders();
                    ImageCodecInfo jpegICI = null;
                    for (var i = 0; i < arrayICI.Length; i++)
                    {
                        if (arrayICI[i].FormatDescription.Equals("JPEG"))
                        {
                            jpegICI = arrayICI[i]; //设置JPEG编码
                            break;
                        }
                    }
                    if (jpegICI != null)
                    {
                        bitmap.Save(jpgPath, jpegICI, encoderParams);
                    }
                    else
                    {
                        //以jpg格式保存缩略图
                        bitmap.Save(jpgPath, ImageFormat.Jpeg);
                    }
                }
                return jpgPath;
            }
            return imagePath;
        }

        /**/

        /// <summary>
        ///     生成缩略图
        /// </summary>
        /// <param name="originalImagePath">源图路径（物理路径）</param>
        /// <param name="thumbnailPath">缩略图路径（物理路径）</param>
        /// <param name="width">缩略图宽度</param>
        /// <param name="height">缩略图高度</param>
        /// <param name="mode">生成缩略图的方式</param>
        public static void MakeThumbnail(string originalImagePath, string thumbnailPath, int width, int height,
            ThumbnailMode mode)
        {
            MakeThumbnail(originalImagePath, thumbnailPath, width, height, mode, ImageFormat.Jpeg);
        }

        /**/

        /// <summary>
        ///     生成缩略图
        /// </summary>
        /// <param name="originalImagePath">源图路径（物理路径）</param>
        /// <param name="thumbnailPath">缩略图路径（物理路径）</param>
        /// <param name="width">缩略图宽度</param>
        /// <param name="height">缩略图高度</param>
        /// <param name="mode">生成缩略图的方式</param>
        /// <param name="format">生成缩略图的格式</param>
        public static void MakeThumbnail(string originalImagePath, string thumbnailPath, int width, int height,
            ThumbnailMode mode, ImageFormat format)
        {
            var originalImage = Image.FromFile(originalImagePath);

            var towidth = width;
            var toheight = height;

            var x = 0;
            var y = 0;
            var ow = originalImage.Width;
            var oh = originalImage.Height;

            switch (mode)
            {
                case ThumbnailMode.HW: // 指定高宽缩放（可能变形）                
                    break;
                case ThumbnailMode.W: // 指定宽，高按比例                    
                    toheight = originalImage.Height * width / originalImage.Width;
                    break;
                case ThumbnailMode.H: // 指定高，宽按比例
                    towidth = originalImage.Width * height / originalImage.Height;
                    break;
                case ThumbnailMode.Cut: // 指定高宽裁减（不变形）                
                    if (originalImage.Width / (double)originalImage.Height > towidth / (double)toheight)
                    {
                        oh = originalImage.Height;
                        ow = originalImage.Height * towidth / toheight;
                        y = 0;
                        x = (originalImage.Width - ow) / 2;
                    }
                    else
                    {
                        ow = originalImage.Width;
                        oh = originalImage.Width * height / towidth;
                        x = 0;
                        y = (originalImage.Height - oh) / 2;
                    }
                    break;
                default:
                    break;
            }

            //新建一个bmp图片
            Image bitmap = new Bitmap(towidth, toheight);

            //新建一个画板
            var g = Graphics.FromImage(bitmap);

            g.CompositingQuality = CompositingQuality.HighQuality;

            //设置高质量插值法
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            //设置高质量,低速度呈现平滑程度
            g.SmoothingMode = SmoothingMode.HighQuality;

            //清空画布并以透明背景色填充
            g.Clear(Color.Transparent);

            //在指定位置并且按指定大小绘制原图片的指定部分
            g.DrawImage(originalImage, new Rectangle(0, 0, towidth, toheight),
                new Rectangle(x, y, ow, oh),
                GraphicsUnit.Pixel);

            try
            {
                if (format == ImageFormat.Jpeg)
                {
                    // 以下代码为保存图片时，设置压缩质量
                    var encoderParams = new EncoderParameters();
                    var quality = new long[1];
                    quality[0] = 85;
                    var encoderParam = new EncoderParameter(Encoder.Quality, quality);
                    encoderParams.Param[0] = encoderParam;
                    //获得包含有关内置图像编码解码器的信息的ImageCodecInfo 对象。
                    var arrayICI = ImageCodecInfo.GetImageEncoders();
                    ImageCodecInfo jpegICI = null;
                    for (var i = 0; i < arrayICI.Length; i++)
                    {
                        if (arrayICI[i].FormatDescription.Equals("JPEG"))
                        {
                            jpegICI = arrayICI[i]; //设置JPEG编码
                            break;
                        }
                    }
                    if (jpegICI != null)
                    {
                        bitmap.Save(thumbnailPath, jpegICI, encoderParams);
                    }
                    else
                    {
                        //以jpg格式保存缩略图
                        bitmap.Save(thumbnailPath, ImageFormat.Jpeg);
                    }
                }
                else
                {
                    bitmap.Save(thumbnailPath, format);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                originalImage.Dispose();
                bitmap.Dispose();
                g.Dispose();
            }
        }

        /// <summary>
        ///     生成指定宽高的图片
        /// </summary>
        /// <param name="originalImage"></param>
        /// <param name="toWidth"></param>
        /// <param name="toHeight"></param>
        /// <returns></returns>
        public static Image MakeThumbnail(Image originalImage, int toWidth, int toHeight)
        {
            if (toWidth <= 0 && toHeight <= 0)
            {
                return originalImage;
            }
            if (toWidth > 0 && toHeight > 0)
            {
                if (originalImage.Width < toWidth && originalImage.Height < toHeight)
                    return originalImage;
            }
            else if (toWidth <= 0 && toHeight > 0) //指定高度，宽度按比例
            {
                if (originalImage.Height < toHeight)
                    return originalImage;
                toWidth = originalImage.Width * toHeight / originalImage.Height;
            }
            else if (toHeight <= 0 && toWidth > 0) //指定宽度，高度按比例
            {
                if (originalImage.Width < toWidth)
                    return originalImage;
                toHeight = originalImage.Height * toWidth / originalImage.Width;
            }
            Image toBitmap = new Bitmap(toWidth, toHeight);
            using (var g = Graphics.FromImage(toBitmap))
            {
                g.InterpolationMode = InterpolationMode.High;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.Clear(Color.Transparent);
                g.DrawImage(originalImage,
                    new Rectangle(0, 0, toWidth, toHeight),
                    new Rectangle(0, 0, originalImage.Width, originalImage.Height),
                    GraphicsUnit.Pixel);
                originalImage.Dispose();
                return toBitmap;
            }
        }

        /// <summary>
        ///     获取缩略图字节流
        /// </summary>
        /// <param name="originalImageData"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="mode"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static byte[] GetThumbnailData(byte[] originalImageData, int width, int height, ThumbnailMode mode,
            ImageFormat format)
        {
            var image = Image.FromStream(new MemoryStream(originalImageData));
            var num = width;
            var num2 = height;
            var x = 0;
            var y = 0;
            var num3 = image.Width;
            var num4 = image.Height;
            if (mode != ThumbnailMode.HW)
            {
                if (mode != ThumbnailMode.W)
                {
                    if (mode != ThumbnailMode.H)
                    {
                        if (mode == ThumbnailMode.Cut)
                        {
                            var flag = image.Width / (double)image.Height > num / (double)num2;
                            if (flag)
                            {
                                num4 = image.Height;
                                num3 = image.Height * num / num2;
                                y = 0;
                                x = (image.Width - num3) / 2;
                            }
                            else
                            {
                                num3 = image.Width;
                                num4 = image.Width * height / num;
                                x = 0;
                                y = (image.Height - num4) / 2;
                            }
                        }
                    }
                    else
                    {
                        num = image.Width * height / image.Height;
                    }
                }
                else
                {
                    num2 = image.Height * width / image.Width;
                }
            }
            Image image2 = new Bitmap(num, num2);
            var graphics = Graphics.FromImage(image2);
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.Clear(Color.Transparent);
            graphics.DrawImage(image, new Rectangle(0, 0, num, num2), new Rectangle(x, y, num3, num4),
                GraphicsUnit.Pixel);
            byte[] result;
            try
            {
                var memoryStream = new MemoryStream();
                var flag2 = format == ImageFormat.Jpeg;
                if (flag2)
                {
                    var encoderParameters = new EncoderParameters();
                    long[] value =
                    {
                        85L
                    };
                    var encoderParameter = new EncoderParameter(Encoder.Quality, value);
                    encoderParameters.Param[0] = encoderParameter;
                    var imageEncoders = ImageCodecInfo.GetImageEncoders();
                    ImageCodecInfo imageCodecInfo = null;
                    for (var i = 0; i < imageEncoders.Length; i++)
                    {
                        var flag3 = imageEncoders[i].FormatDescription.Equals("JPEG");
                        if (flag3)
                        {
                            imageCodecInfo = imageEncoders[i];
                            break;
                        }
                    }
                    var flag4 = imageCodecInfo != null;
                    if (flag4)
                    {
                        image2.Save(memoryStream, imageCodecInfo, encoderParameters);
                    }
                    else
                    {
                        image2.Save(memoryStream, ImageFormat.Jpeg);
                    }
                }
                else
                {
                    image2.Save(memoryStream, format);
                }
                memoryStream.Seek(0L, SeekOrigin.Begin);
                var array = new byte[memoryStream.Length];
                memoryStream.Read(array, 0, array.Length);
                result = array;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                image.Dispose();
                image2.Dispose();
                graphics.Dispose();
            }
            return result;
        }

        public static void SaveJPEG(Image image, string savePath, int quality = 100)
        {
            // 以下代码为保存图片时，设置压缩质量
            var encoderParams = new EncoderParameters();
            var qualitys = new long[1];
            qualitys[0] = quality;
            var encoderParam = new EncoderParameter(Encoder.Quality, qualitys);
            encoderParams.Param[0] = encoderParam;
            // 获得包含有关内置图像编码解码器的信息的ImageCodecInfo 对象。
            var arrayICI = ImageCodecInfo.GetImageEncoders();
            ImageCodecInfo jpegICI = null;
            for (var i = 0; i < arrayICI.Length; i++)
            {
                if (arrayICI[i].FormatDescription.Equals("JPEG"))
                {
                    jpegICI = arrayICI[i]; // 设置JPEG编码
                    break;
                }
            }
            if (jpegICI != null)
            {
                image.Save(savePath, jpegICI, encoderParams);
            }
            else
            {
                // 以jpg格式保存缩略图
                image.Save(savePath, ImageFormat.Jpeg);
            }
        }

        // 将Image转换为byte[]
        public static byte[] ImageToBytes(Image image)
        {
            byte[] bt = null;
            if (!image.Equals(null))
            {
                using (var mostream = new MemoryStream())
                {
                    var bmp = new Bitmap(image);
                    bmp.Save(mostream, ImageFormat.Bmp); // 将图像以指定的格式存入缓存内存流
                    bt = new byte[mostream.Length];
                    mostream.Position = 0; // 设置留的初始位置
                    mostream.Read(bt, 0, Convert.ToInt32(bt.Length));
                }
            }
            return bt;
        }

        /// <summary>
        ///     通过文件的前两个自己判断图像类型
        /// </summary>
        /// <param name="buf">至少2个字节</param>
        /// <returns></returns>
        public static ImageType CheckImageType(byte[] buf)
        {
            if (_imageTag == null)
            {
                _imageTag = new SortedDictionary<int, ImageType>();

                foreach (int value in Enum.GetValues(typeof(ImageType)))
                {
                    var strName = Enum.GetName(typeof(ImageType), value);
                    _imageTag.Add(value, (ImageType)value);
                }
            }

            if (buf == null || buf.Length < 2)
            {
                return ImageType.None;
            }

            var key = (buf[1] << 8) + buf[0];
            ImageType s;
            if (_imageTag.TryGetValue(key, out s))
            {
                return s;
            }
            return ImageType.None;
        }
    }
}