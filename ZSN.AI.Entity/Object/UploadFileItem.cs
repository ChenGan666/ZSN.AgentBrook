using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ZSN.AI.Entity.Object
{
    public enum UploadState
    {
        Success,
        Fail,
        Uploading
    }
    public partial class UploadFileItem
    {
        public string Id { get; set; }

        public string FileName { get; set; }

        public int Percent { get; set; }

        public string ObjectURL { get; set; }

        public string Url { get; set; }

        public string Response { get; set; }

        public UploadState State { get; set; }

        public long Size { get; set; }

        public string Ext { get; set; }

        public string Type { get; set; }

        public static string[] ImageExtensions { get; set; } = new string[11]
        {
        ".jpg", ".png", ".gif", ".ico", ".jfif", ".jpeg", ".bmp", ".tga", ".svg", ".tif",
        ".webp"
        };


        public TResponseModel GetResponse<TResponseModel>(JsonSerializerOptions options = null)
        {
            if (options == null)
            {
                options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
            }

            return JsonSerializer.Deserialize<TResponseModel>(Response, options);
        }

        public bool IsPicture()
        {
            if (string.IsNullOrEmpty(Ext))
            {
                int num = FileName.LastIndexOf('.');
                if (num < 0)
                {
                    return false;
                }

                string fileName = FileName;
                int num2 = num;
                Ext = fileName.Substring(num2, fileName.Length - num2);
            }

            return ImageExtensions.Any((string imageExt) => imageExt.Equals(Ext, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
