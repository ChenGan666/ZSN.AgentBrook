using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using SI = SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace ZSN.Utils.Core.PIC
{
    /// <summary>
	/// Thumbnail 的摘要说明。
	/// </summary>
	public class Thumbnail
    {
        private System.Drawing.Image srcImage;
        private string srcFileName;

        /// <summary>
        /// 创建
        /// </summary>
        /// <param name="FileName">原始图片路径</param>
        public bool SetImage(string FileName)
        {
            srcFileName = Path.GetFullPath(FileName);
            try
            {
                srcImage = System.Drawing.Image.FromFile(srcFileName);
            }
            catch
            {
                return false;
            }
            return true;

        }

        /// <summary>
        /// 回调
        /// </summary>
        /// <returns></returns>
        public bool ThumbnailCallback()
        {
            return false;
        }

        /// <summary>
        /// 生成缩略图,返回缩略图的Image对象
        /// </summary>
        /// <param name="Width">缩略图宽度</param>
        /// <param name="Height">缩略图高度</param>
        /// <returns>缩略图的Image对象</returns>
        public System.Drawing.Image GetImage(int Width, int Height)
        {
            System.Drawing.Image img;
            System.Drawing.Image.GetThumbnailImageAbort callb = new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback);
            img = srcImage.GetThumbnailImage(Width, Height, callb, IntPtr.Zero);
            return img;
        }

        public static void ImageChange(SI.Image _sourceImg, out SI.Image _OutImg, int w, int h)
        {
            w = w > 0 ? w : _sourceImg.Width;
            h = h > 0 ? h : _sourceImg.Height;

            _OutImg = _sourceImg.Clone(x => x.AutoOrient().Resize(new ResizeOptions
            {
                Size = new SI.Size(w, h),
                Mode = ResizeMode.Max
            }));
        }

        /// <summary>
        /// Convert Image to Byte[]
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static byte[] ImageToBytes(SI.Image image)
        {
            if (image != null)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    image.Save(ms, new JpegEncoder());
                    byte[] buffer = new byte[ms.Length];
                    ms.Seek(0, SeekOrigin.Begin);
                    ms.Read(buffer, 0, buffer.Length);
                    return buffer;
                }
            }
            else
            {
                return new byte[0];
            }
        }
        /// <summary>
        /// 保存缩略图
        /// </summary>
        /// <param name="Width"></param>
        /// <param name="Height"></param>
        public void SaveThumbnailImage(int Width, int Height)
        {
            switch (Path.GetExtension(srcFileName).ToLower())
            {
                case ".png":
                    SaveImage(Width, Height, ImageFormat.Png);
                    break;
                case ".gif":
                    SaveImage(Width, Height, ImageFormat.Gif);
                    break;
                default:
                    SaveImage(Width, Height, ImageFormat.Jpeg);
                    break;
            }
        }

        /// <summary>
        /// 生成缩略图并保存
        /// </summary>
        /// <param name="Width">缩略图的宽度</param>
        /// <param name="Height">缩略图的高度</param>
        /// <param name="imgformat">保存的图像格式</param>
        /// <returns>缩略图的Image对象</returns>
        public void SaveImage(int Width, int Height, ImageFormat imgformat)
        {
            if ((srcImage.Width > Width) || (srcImage.Height > Height))
            {

                System.Drawing.Image img;
                System.Drawing.Image.GetThumbnailImageAbort callb = new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback);
                img = srcImage.GetThumbnailImage(Width, Height, callb, IntPtr.Zero);
                srcImage.Dispose();
                img.Save(srcFileName, imgformat);
                img.Dispose();
            }
        }

        #region Helper

        /// <summary>
        /// 保存图片
        /// </summary>
        /// <param name="image">Image 对象</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="ici">指定格式的编解码参数</param>
        private static void SaveImage(System.Drawing.Image image, string savePath, ImageCodecInfo ici)
        {
            //设置 原图片 对象的 EncoderParameters 对象
            EncoderParameters parameters = new EncoderParameters(1);
            parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, ((long)100));
            image.Save(savePath, ici, parameters);
            parameters.Dispose();
        }

        /// <summary>
        /// 获取图像编码解码器的所有相关信息
        /// </summary>
        /// <param name="mimeType">包含编码解码器的多用途网际邮件扩充协议 (MIME) 类型的字符串</param>
        /// <returns>返回图像编码解码器的所有相关信息</returns>
        private static ImageCodecInfo GetCodecInfo(string mimeType)
        {
            ImageCodecInfo[] CodecInfo = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo ici in CodecInfo)
            {
                if (ici.MimeType == mimeType) return ici;
            }
            return null;
        }

        /// <summary>
        /// 计算新尺寸
        /// </summary>
        /// <param name="width">原始宽度</param>
        /// <param name="height">原始高度</param>
        /// <param name="maxWidth">最大新宽度</param>
        /// <param name="maxHeight">最大新高度</param>
        /// <returns></returns>
        private static System.Drawing.Size ResizeImage(int width, int height, int maxWidth, int maxHeight)
        {
            decimal MAX_WIDTH = (decimal)maxWidth;
            decimal MAX_HEIGHT = (decimal)maxHeight;
            decimal ASPECT_RATIO = MAX_WIDTH / MAX_HEIGHT;

            int newWidth, newHeight;

            decimal originalWidth = (decimal)width;
            decimal originalHeight = (decimal)height;

            if (originalWidth > MAX_WIDTH || originalHeight > MAX_HEIGHT)
            {
                decimal factor;
                // determine the largest factor 
                if (originalWidth / originalHeight > ASPECT_RATIO)
                {
                    factor = originalWidth / MAX_WIDTH;
                    newWidth = Convert.ToInt32(originalWidth / factor);
                    newHeight = Convert.ToInt32(originalHeight / factor);
                }
                else
                {
                    factor = originalHeight / MAX_HEIGHT;
                    newWidth = Convert.ToInt32(originalWidth / factor);
                    newHeight = Convert.ToInt32(originalHeight / factor);
                }
            }
            else
            {
                newWidth = width;
                newHeight = height;
            }

            return new Size(newWidth, newHeight);

        }

        /// <summary>
        /// 得到图片格式
        /// </summary>
        /// <param name="name">文件名称</param>
        /// <returns></returns>
        public static ImageFormat GetFormat(string name)
        {
            string ext = name.Substring(name.LastIndexOf(".") + 1);
            switch (ext.ToLower())
            {
                case "jpg":
                case "jpeg":
                    return ImageFormat.Jpeg;
                case "bmp":
                    return ImageFormat.Bmp;
                case "png":
                    return ImageFormat.Png;
                case "gif":
                    return ImageFormat.Gif;
                default:
                    return ImageFormat.Jpeg;
            }
        }
        #endregion

        /// <summary>
        /// 制作小正方形
        /// </summary>
        /// <param name="fileName">原图的文件路径</param>
        /// <param name="newFileName">新地址</param>
        /// <param name="newSize">长度或宽度</param>
        public static void MakeSquareImage(string fileName, string newFileName, int newSize)
        {
            System.Drawing.Image image = System.Drawing.Image.FromFile(fileName);

            int i = 0;
            int width = image.Width;
            int height = image.Height;
            if (width > height)
            {
                i = height;
            }
            else
            {
                i = width;
            }
            Bitmap b = new Bitmap(newSize, newSize);

            try
            {
                Graphics g = Graphics.FromImage(b);
                g.InterpolationMode = InterpolationMode.High;
                g.SmoothingMode = SmoothingMode.HighQuality;

                //清除整个绘图面并以透明背景色填充
                g.Clear(Color.Transparent);
                if (width < height)
                {
                    g.DrawImage(image, new Rectangle(0, 0, newSize, newSize), new Rectangle(0, (height - width) / 2, width, width), GraphicsUnit.Pixel);
                }
                else
                {
                    g.DrawImage(image, new Rectangle(0, 0, newSize, newSize), new Rectangle((width - height) / 2, 0, height, height), GraphicsUnit.Pixel);
                }

                SaveImage(b, newFileName, GetCodecInfo("image/" + GetFormat(fileName).ToString().ToLower()));
            }
            finally
            {
                image.Dispose();
                b.Dispose();
            }

        }


        /// <summary>
        /// 制作缩略图
        /// </summary>
        /// <param name="fileName">原图路径</param>
        /// <param name="newFileName">新图路径</param>
        /// <param name="maxWidth">最大宽度</param>
        /// <param name="maxHeight">最大高度</param>
        public static void MakeThumbnailImage(string fileName, string newFileName, int maxWidth, int maxHeight)
        {
            System.Drawing.Image original = System.Drawing.Image.FromFile(fileName);

            Size _newSize = ResizeImage(original.Width, original.Height, maxWidth, maxHeight);
            //_image.Height = _newSize.Height;
            //_image.Width = _newSize.Width;
            System.Drawing.Image displayImage = new Bitmap(original, _newSize);
            //original.Dispose();

            try
            {
                displayImage.Save(newFileName, GetFormat(fileName));
            }
            finally
            {
                original.Dispose();
            }

        }

        /// <SUMMARY>
        /// 图片无损缩放
        /// </SUMMARY>
        /// <PARAM name="sourceFile">图片源路径</PARAM>
        /// <PARAM name="destFile">缩放后图片输出路径</PARAM>
        /// <PARAM name="destHeight">缩放后图片高度</PARAM>
        /// <PARAM name="destWidth">缩放后图片宽度</PARAM>
        /// <RETURNS></RETURNS>
        public static bool GetThumbnail(string sourceFile, string destFile, int destHeight, int destWidth)
        {
            System.Drawing.Image imgSource = System.Drawing.Image.FromFile(sourceFile);
            System.Drawing.Imaging.ImageFormat thisFormat = imgSource.RawFormat;
            int sW = 0, sH = 0;
            // 按比例缩放
            int sWidth = imgSource.Width;
            int sHeight = imgSource.Height;

            if (sHeight > destHeight || sWidth > destWidth)
            {
                if ((sWidth * destHeight) > (sHeight * destWidth))
                {
                    sW = destWidth;
                    sH = (destWidth * sHeight) / sWidth;
                }
                else
                {
                    sH = destHeight;
                    sW = (sWidth * destHeight) / sHeight;
                }
            }
            else
            {
                sW = sWidth;
                sH = sHeight;
            }

            Bitmap outBmp = new Bitmap(destWidth, destHeight);
            Graphics g = Graphics.FromImage(outBmp);
            g.Clear(Color.WhiteSmoke);

            // 设置画布的描绘质量
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            g.DrawImage(imgSource, new Rectangle((destWidth - sW) / 2, (destHeight - sH) / 2, sW, sH), 0, 0, imgSource.Width, imgSource.Height, GraphicsUnit.Pixel);
            g.Dispose();

            // 以下代码为保存图片时，设置压缩质量
            EncoderParameters encoderParams = new EncoderParameters();
            long[] quality = new long[1];
            quality[0] = 100;

            EncoderParameter encoderParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            encoderParams.Param[0] = encoderParam;

            try
            {
                //获得包含有关内置图像编码解码器的信息的ImageCodecInfo 对象。
                ImageCodecInfo[] arrayICI = ImageCodecInfo.GetImageEncoders();
                ImageCodecInfo jpegICI = null;
                for (int x = 0; x < arrayICI.Length; x++)
                {
                    if (arrayICI[x].FormatDescription.Equals("JPEG"))
                    {
                        jpegICI = arrayICI[x];//设置JPEG编码
                        break;
                    }
                }

                if (jpegICI != null)
                {
                    outBmp.Save(destFile, jpegICI, encoderParams);
                }
                else
                {
                    outBmp.Save(destFile, thisFormat);
                }

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                imgSource.Dispose();
                outBmp.Dispose();
            }
        }


        public static System.Drawing.Bitmap BuildBitmap(int width, int height, string strBmp)
        {
            System.Drawing.Bitmap tmpBmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            string[] StmpBmp = strBmp.Split(',');
            int pos = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tmpBmp.SetPixel(x, y, Color.FromArgb(int.Parse(StmpBmp[pos], System.Globalization.NumberStyles.HexNumber)));
                    pos++;
                }
            }
            return tmpBmp;
        }

        public static System.Drawing.Bitmap Base64StringToImage(string strBmp)
        {
            try
            {
                byte[] arr = Convert.FromBase64String(strBmp);
                MemoryStream ms = new MemoryStream(arr);
                Bitmap bmp = new Bitmap(ms);
                try
                {
                    return bmp;
                }
                finally
                {
                    arr = null;
                    ms.Dispose();
                    bmp.Dispose();
                }
            }
            catch
            {
                return null;
            }
        }

        public static System.Drawing.Bitmap BytesToImage(byte[] arr)
        {
            try
            {
                MemoryStream ms = new MemoryStream(arr);
                Bitmap bmp = new Bitmap(ms);

                return bmp;
            }
            catch
            {
                return null;
            }
        }

        public static void SaveBmp(System.Drawing.Bitmap bmp, string filePathandName)
        {
            try
            {
                bmp.Save(filePathandName, System.Drawing.Imaging.ImageFormat.Bmp);
                bmp.Dispose();
            }
            catch
            {
                bmp.Dispose();
            }
        }

        /// <summary>
        /// 进制转换
        /// </summary>
        public static string ConvertString(string value, int fromBase, int toBase)
        {

            int intValue = Convert.ToInt32(value, fromBase);

            return Convert.ToString(intValue, toBase);
        }

        public static Dictionary<string, string> ReadExif(System.Drawing.Image image)
        {
            var exif = new Dictionary<string, string>();
            var properties = image.PropertyItems;
            foreach (var property in properties)
            {
                switch (property.Id)
                {
                    case 0x010E:
                        exif["ImageTitle"] = ASCIIToString(property.Value);
                        break;
                    case 0x010F:
                        exif["Make"] = ASCIIToString(property.Value);
                        break;
                    case 0x0110:
                        exif["Model"] = ASCIIToString(property.Value);
                        break;
                    case 0x0112:
                        exif["Orientation"] = ShortToString(property.Value, 0);
                        break;
                    case 0x011A:
                        exif["XResolution"] = RationalToSingle(property.Value, 0);
                        break;
                    case 0x011B:
                        exif["YResolution"] = RationalToSingle(property.Value, 0);
                        break;
                    case 0x0128:
                        exif["ResolutionUnit"] = ShortToString(property.Value, 0);
                        break;
                    case 0x0131:
                        exif["Software"] = ASCIIToString(property.Value);
                        break;
                    case 0x0132:
                        exif["DateTime"] = ASCIIToString(property.Value);
                        break;
                    //GPS
                    case 0x0002:
                        exif["GPSLatitude"] = string.Format("{0}°{1}′{2}″",
                                                            RationalToSingle(property.Value, 0),
                                                            RationalToSingle(property.Value, 8),
                                                            RationalToSingle(property.Value, 16)
                                                            );
                        break;
                    case 0x0004:
                        exif["GPSLongitude"] = string.Format("{0}°{1}′{2}″",
                                                            RationalToSingle(property.Value, 0),
                                                            RationalToSingle(property.Value, 8),
                                                            RationalToSingle(property.Value, 16)
                                                            );
                        break;
                    case 0x0006:
                        exif["GPSAltitude"] = RationalToSingle(property.Value, 0);
                        break;
                }
            }
            return exif;
        }
        static string ByteToString(byte[] b, int startindex)
        {
            if (startindex + 1 <= b.Length)
                return ((char)b[startindex]).ToString();
            else
                return string.Empty;
        }

        static string ShortToString(byte[] b, int startindex)
        {
            if (startindex + 2 <= b.Length)
                return BitConverter.ToInt16(b, startindex).ToString();
            else
                return string.Empty;
        }

        static string RationalToSingle(byte[] b, int startindex)
        {
            if (startindex + 8 <= b.Length)
                return (BitConverter.ToSingle(b, startindex) / BitConverter.ToSingle(b, startindex + 4)).ToString();
            else
                return string.Empty;
        }

        static string ASCIIToString(byte[] b)
        {
            return Encoding.ASCII.GetString(b);
        }
    }

    /// <summary>
    /// 文件类型验证
    /// </summary>
    public enum FileExtension
    {
        JPG = 255216,
        GIF = 7173,
        PNG = 13780,
        SWF = 6787,
        RAR = 8297,
        ZIP = 8075,
        _7Z = 55122,
        BMP = 6677

        // 6787 swf

        // 7790 exe dll,

        // 8297 rar

        // 8075 zip

        // 55122 7z

        // 6063 xml

        // 6033 html

        // 239187 aspx

        // 117115 cs

        // 119105 js

        // 102100 txt

        // 255254 sql 

    }

    public class FileValidation
    {
        public static bool IsAllowedExtension(MemoryStream ms, FileExtension[] fileEx)
        {
            System.IO.BinaryReader br = new System.IO.BinaryReader(ms);
            string fileclass = "";
            byte buffer;
            try
            {
                buffer = br.ReadByte();
                fileclass = buffer.ToString();
                buffer = br.ReadByte();
                fileclass += buffer.ToString();
            }
            catch
            {
            }
            br.Close();
            ms.Close();
            foreach (FileExtension fe in fileEx)
            {
                if (Int32.Parse(fileclass) == (int)fe)
                    return true;
            }
            return false;
        }
        /*
        public static bool IsAllowedExtension(System.Web.UI.WebControls.FileUpload fu, FileExtension[] fileEx)
        {
            int fileLen = fu.PostedFile.ContentLength;
            byte[] imgArray = new byte[fileLen];
            fu.PostedFile.InputStream.Read(imgArray, 0, fileLen);
            MemoryStream ms = new MemoryStream(imgArray);
            System.IO.BinaryReader br = new System.IO.BinaryReader(ms);
            string fileclass = "";
            byte buffer;
            try
            {
                buffer = br.ReadByte();
                fileclass = buffer.ToString();
                buffer = br.ReadByte();
                fileclass += buffer.ToString();
            }
            catch
            {
            }
            br.Close();
            ms.Close();
            foreach (FileExtension fe in fileEx)
            {
                if (Int32.Parse(fileclass) == (int)fe)
                    return true;
            }
            return false;
        }
        public static bool IsAllowedExtension(HttpPostedFile pu, FileExtension[] fileEx)
        {
            int fileLen = pu.ContentLength;
            byte[] imgArray = new byte[fileLen];
            pu.InputStream.Read(imgArray, 0, fileLen);
            MemoryStream ms = new MemoryStream(imgArray);
            System.IO.BinaryReader br = new System.IO.BinaryReader(ms);
            string fileclass = "";
            byte buffer;
            try
            {
                buffer = br.ReadByte();
                fileclass = buffer.ToString();
                buffer = br.ReadByte();
                fileclass += buffer.ToString();
            }
            catch
            {
            }
            br.Close();
            ms.Close();
            foreach (FileExtension fe in fileEx)
            {
                if (Int32.Parse(fileclass) == (int)fe)
                    return true;
            }
            return false;
        }
        */
    }
}
