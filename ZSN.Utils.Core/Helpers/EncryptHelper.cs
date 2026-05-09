using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using ZSN.Utils.Core.Extensions;
using ZSN.Utils.Core.Utils;

namespace ZSN.Utils.Core.Helpers
{
    /// <summary>
    ///     加密帮助类
    /// </summary>
    public static class EncryptHelper
    {
        /// <summary>
        ///     DES加解密的默认密钥
        /// </summary>
        private static readonly string DefaultDesKey = "1q2w3e4r";

        // 默认3Des密钥
        private static readonly byte[] Default3DesKey =
        {
            0x0A, 0xE1, 0xD2, 0xC3, 0xB4, 0x67, 0x89, 0xAB, 0xCD, 0xEF,
            0x87, 0x78, 0x69, 0x5A, 0x4B, 0x3C, 0x2D, 0x1E, 0xA5, 0x96, 0x0F, 0x01, 0x23, 0x45
        };

        // 默认3Des向量
        private static readonly byte[] Default3DesIv = { 0x03, 0xA3, 0x45, 0xCD, 0xEF, 0x69, 0xC9, 0xFB };

        public static string SHA1(string source, Encoding encoding = null)
        {
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }
            SHA1 sha1 = System.Security.Cryptography.SHA1.Create();// new SHA1CryptoServiceProvider();
            var bytesSha1In = encoding.GetBytes(source);
            var bytesSha1Out = sha1.ComputeHash(bytesSha1In);
            var strSha1Out = BitConverter.ToString(bytesSha1Out);
            return strSha1Out.Replace("-", "").ToLower();
        }

        /// <summary>
        ///     GZip压缩
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static byte[] GZipCompress(byte[] source)
        {
            using (var ms = new MemoryStream())
            {
                var compressedzipStream = new GZipStream(ms, CompressionMode.Compress, true);
                compressedzipStream.Write(source, 0, source.Length);
                compressedzipStream.Close();
                return ms.ToArray();
            }
        }

        /// <summary>
        ///     GZip解压缩
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static byte[] GZipDecompress(byte[] source)
        {
            using (var ms = new MemoryStream(source))
            {
                var compressedzipStream = new GZipStream(ms, CompressionMode.Decompress);
                using (var outBuffer = new MemoryStream())
                {
                    var block = new byte[1024];
                    while (true)
                    {
                        var bytesRead = compressedzipStream.Read(block, 0, block.Length);
                        if (bytesRead <= 0)
                            break;
                        outBuffer.Write(block, 0, bytesRead);
                    }
                    compressedzipStream.Close();
                    return outBuffer.ToArray();
                }
            }
        }

        public static byte[] HmacSha1Sign(string text, string key)
        {
            var encode = Encoding.UTF8;
            var byteData = encode.GetBytes(text);
            var byteKey = encode.GetBytes(key);
            var hmac = new HMACSHA1(byteKey);
            var cs = new CryptoStream(Stream.Null, hmac, CryptoStreamMode.Write);
            cs.Write(byteData, 0, byteData.Length);
            cs.Close();
            return hmac.Hash;
        }

        #region  MD5加密

        /// <summary>
        ///     标准MD5加密
        /// </summary>
        /// <param name="source">待加密字符串</param>
        /// <param name="addKey">附加字符串</param>
        /// <param name="encoding">编码方式</param>
        /// <returns></returns>
        public static string MD5Encrypt(string source, string addKey, Encoding encoding)
        {
            if (addKey.Length > 0)
            {
                source = source + addKey;
            }
            var dataSource = encoding.GetBytes(source);
            return MD5Encrypt(dataSource);
        }

        /// <summary>
        ///     计算MD5
        /// </summary>
        /// <param name="dataSource">字节码</param>
        /// <returns></returns>
        public static string MD5Encrypt(byte[] dataSource)
        {
            MD5 MD5 = MD5.Create();// new MD5CryptoServiceProvider();
            var newSource = MD5.ComputeHash(dataSource);
            string byte2String = null;
            for (var i = 0; i < newSource.Length; i++)
            {
                var thisByte = newSource[i].ToString("x");
                if (thisByte.Length == 1) thisByte = "0" + thisByte;
                byte2String += thisByte;
            }
            return byte2String;
        }

        /// <summary>
        ///     标准MD5加密
        /// </summary>
        /// <param name="source">待加密字符串</param>
        /// <param name="encoding">编码方式</param>
        /// <returns></returns>
        public static string MD5Encrypt(string source, string encoding)
        {
            return MD5Encrypt(source, string.Empty, Encoding.GetEncoding(encoding));
        }

        /// <summary>
        ///     标准MD5加密，默认是utf-8
        /// </summary>
        /// <param name="source">被加密的字符串</param>
        /// <returns></returns>
        public static string MD5Encrypt(string source)
        {
            return MD5Encrypt(source, string.Empty, Encoding.UTF8);
        }

        /// <summary>
        ///     MD5 16位加密 加密后密码为小写
        /// </summary>
        /// <param name="ConvertString"></param>
        /// <returns></returns>
        public static string MD5EncryptFor16(string ConvertString)
        {
            var md5 = MD5.Create();// new MD5CryptoServiceProvider();
            var t2 = BitConverter.ToString(md5.ComputeHash(Encoding.Default.GetBytes(ConvertString)), 4, 8);
            t2 = t2.Replace("-", "");
            t2 = t2.ToLower();
            return t2;
        }

        /// <summary>
        /// 文件MD5值
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string GetMD5HashFromFile(string filePath)
        {
            FileStream file = new FileStream(filePath, FileMode.Open);
            MD5 md5 = MD5.Create();// new MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(file);
            file.Close();
            
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }

        #endregion

        #region  DES 加解密

        /// <summary>
        ///     Desc加密 Encoding.Default
        /// </summary>
        /// <param name="source">待加密字符</param>
        /// <param name="key">密钥</param>
        /// <returns>string</returns>
        public static string DESEncrypt(string source, string key)
        {
            if (string.IsNullOrEmpty(source))
                return null;
            var des =  new DESCryptoServiceProvider();
            des.Mode = CipherMode.CBC;
            des.Padding = PaddingMode.PKCS7;
            // 把字符串放到byte数组中  
            var inputByteArray = Encoding.Default.GetBytes(source);

            // 建立加密对象的密钥和偏移量  
            // 原文使用ASCIIEncoding.ASCII方法的GetBytes方法  
            // 使得输入密码必须输入英文文本  
            //            des.Key = UTF8Encoding.UTF8.GetBytes(key);
            //            des.IV  = UTF8Encoding.UTF8.GetBytes(key);
            des.Key = Encoding.Default.GetBytes(key);
            des.IV = Encoding.Default.GetBytes(key);

            var ms = new MemoryStream();
            var cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write);

            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();

            var ret = new StringBuilder();
            foreach (var b in ms.ToArray())
            {
                ret.AppendFormat("{0:X2}", b);
            }

            ret.ToString();
            return ret.ToString();
        }

        /// <summary>
        ///     使用默认key 做 DES加密 Encoding.Default
        /// </summary>
        /// <param name="source">明文</param>
        /// <returns>密文</returns>
        public static string DESEncrypt(string source)
        {
            return DESEncrypt(source, DefaultDesKey);
        }

        /// <summary>
        ///     使用默认key 做 DES解密 Encoding.Default
        /// </summary>
        /// <param name="source">密文</param>
        /// <returns>明文</returns>
        public static string DESDecrypt(string source)
        {
            return DESDecrypt(source, DefaultDesKey);
        }

        /// <summary>
        ///     DES解密 Encoding.Default
        /// </summary>
        /// <param name="source">密文</param>
        /// <param name="key">密钥</param>
        /// <returns>明文</returns>
        public static string DESDecrypt(string source, string key)
        {
            if (string.IsNullOrEmpty(source))
                return null;

            var des = new DESCryptoServiceProvider();
            des.Mode = CipherMode.CBC;
            des.Padding = PaddingMode.PKCS7;
            //将字符串转为字节数组  
            var inputByteArray = new byte[source.Length / 2];
            for (var x = 0; x < source.Length / 2; x++)
            {
                var i = Convert.ToInt32(source.Substring(x * 2, 2), 16);
                inputByteArray[x] = (byte)i;
            }

            //建立加密对象的密钥和偏移量，此值重要，不能修改  
            des.Key = Encoding.UTF8.GetBytes(key);
            des.IV = Encoding.UTF8.GetBytes(key);

            var ms = new MemoryStream();
            var cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();

            //建立StringBuild对象，CreateDecrypt使用的是流对象，必须把解密后的文本变成流对象  
            //var ret = new StringBuilder();

            return Encoding.Default.GetString(ms.ToArray());
        }

        #endregion

        #region 生成随机3DES密钥

        /// <summary>
        ///     生成随机3DES密钥
        /// </summary>
        /// <returns>字节数组表示的密钥</returns>
        public static byte[] GenerateByteArray3DESKey()
        {
            byte[] buf = null;
            var dsp = new TripleDESCryptoServiceProvider();
            dsp.GenerateKey();
            buf = dsp.Key;
            return buf;
        }

        /// <summary>
        ///     生成随机3DES密钥
        /// </summary>
        /// <returns>十六进制字符串表示的密钥</returns>
        public static string GenerateHexString3DESKey()
        {
            var byteArray = GenerateByteArray3DESKey();
            return byteArray.ByteArrayToHexString();
        }

        #endregion

        #region 生成随机3DES加密向量

        /// <summary>
        ///     生成随机3DES加密向量
        /// </summary>
        /// <returns>字节数组表示的加密向量</returns>
        public static byte[] GenerateByteArray3DESIV()
        {
            byte[] buf = null;
            var dsp = new TripleDESCryptoServiceProvider();
            dsp.GenerateIV();
            buf = dsp.IV;
            return buf;
        }

        /// <summary>
        ///     生成随机3DES加密向量
        /// </summary>
        /// <returns>十六进制字符串表示的加密向量</returns>
        public static string GenerateHexString3DESIV()
        {
            var byteArray = GenerateByteArray3DESIV();
            return byteArray.ByteArrayToHexString();
        }

        /// <summary>
        ///     取密钥前8字节作为向量
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static byte[] GetDefaultIVFromKey(byte[] key)
        {
            var iv = new byte[8];
            if (key.Length < 8)
                iv = Default3DesIv;
            else
            {
                for (var i = 0; i < 8; i++)
                {
                    //iv[i] = key[key.Length - 8 + i]; // 后8位
                    iv[i] = key[i]; // 前8位
                }
            }
            return iv;
        }

        #endregion

        #region TripleDesEncrypt 3Des加密

        /// <summary>
        ///     3Des加密，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="iv">向量字节数组</param>
        /// <param name="source">源字节数组</param>
        /// <returns>加密后的字节数组</returns>
        public static byte[] TripleDesEncrypt(byte[] key, byte[] iv, byte[] source)
        {
            var dsp = new TripleDESCryptoServiceProvider();
            dsp.Mode = CipherMode.CBC; // 默认值
            dsp.Padding = PaddingMode.PKCS7; // 默认值
            using (var mStream = new MemoryStream())
            {
                var cStream = new CryptoStream(mStream, dsp.CreateEncryptor(key, iv), CryptoStreamMode.Write);
                cStream.Write(source, 0, source.Length);
                cStream.FlushFinalBlock();
                var result = mStream.ToArray();
                cStream.Close();
                mStream.Close();
                return result;
            }
        }

        /// <summary>
        ///     3Des加密，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="iv">向量字节数组</param>
        /// <param name="source">源字节数组</param>
        /// <returns>加密后的Base64编码字符串</returns>
        public static string TripleDesEncryptToBase64(byte[] key, byte[] iv, byte[] source)
        {
            var result = TripleDesEncrypt(key, iv, source);
            return Convert.ToBase64String(result);
        }

        /// <summary>
        ///     3Des加密，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="iv">向量字节数组</param>
        /// <param name="source">源字节数组</param>
        /// <returns>加密后的十六进制字符串</returns>
        public static string TripleDesEncryptToHex(byte[] key, byte[] iv, byte[] source)
        {
            var result = TripleDesEncrypt(key, iv, source);
            return result.ByteArrayToHexString();
        }

        /// <summary>
        ///     3Des加密，密钥长度必需是24字节
        /// </summary>
        /// <param name="hexStringKey">十六进制密钥字符串</param>
        /// <param name="hexStringIV">十六进制向量字符串</param>
        /// <param name="source">源字节数组</param>
        /// <returns>加密后的字节数组</returns>
        public static byte[] TripleDesEncrypt(string hexStringKey, string hexStringIV, byte[] source)
        {
            var key = hexStringKey.HexStringToByteArray();
            var iv = hexStringIV.HexStringToByteArray();
            return TripleDesEncrypt(key, iv, source);
        }

        /// <summary>
        ///     3Des加密，密钥长度必需是24字节
        /// </summary>
        /// <param name="hexStringKey">十六进制密钥字符串</param>
        /// <param name="hexStringIV">十六进制向量字符串</param>
        /// <param name="source">源字节数组</param>
        /// <returns>加密后的Base64编码字符串</returns>
        public static string TripleDesEncryptToBase64(string hexStringKey, string hexStringIV, byte[] source)
        {
            var result = TripleDesEncrypt(hexStringKey, hexStringIV, source);
            return Convert.ToBase64String(result);
        }

        /// <summary>
        ///     3Des加密，密钥长度必需是24字节
        /// </summary>
        /// <param name="hexStringKey">十六进制密钥字符串</param>
        /// <param name="hexStringIV">十六进制向量字符串</param>
        /// <param name="source">源字节数组</param>
        /// <returns>加密后的十六进制字符串</returns>
        public static string TripleDesEncryptToHex(string hexStringKey, string hexStringIV, byte[] source)
        {
            var result = TripleDesEncrypt(hexStringKey, hexStringIV, source);
            return result.ByteArrayToHexString();
        }

        /// <summary>
        ///     3Des加密，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="iv">向量字节数组</param>
        /// <param name="source">源字符串</param>
        /// <returns>加密后的字节数组</returns>
        public static byte[] TripleDesEncrypt(byte[] key, byte[] iv, string source)
        {
            var encryptSource = Encoding.UTF8.GetBytes(source);
            return TripleDesEncrypt(key, iv, encryptSource);
        }

        /// <summary>
        ///     3Des加密，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="iv">向量字节数组</param>
        /// <param name="source">源字符串</param>
        /// <returns>加密后的Base64编码字符串</returns>
        public static string TripleDesEncryptToBase64(byte[] key, byte[] iv, string source)
        {
            var result = TripleDesEncrypt(key, iv, source);
            return Convert.ToBase64String(result);
        }

        /// <summary>
        ///     3Des加密，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="iv">向量字节数组</param>
        /// <param name="source">源字符串</param>
        /// <returns>加密后的十六进制字符串</returns>
        public static string TripleDesEncryptToHex(byte[] key, byte[] iv, string source)
        {
            var result = TripleDesEncrypt(key, iv, source);
            return result.ByteArrayToHexString();
        }

        /// <summary>
        ///     3Des加密，密钥长度必需是24字节
        /// </summary>
        /// <param name="hexStringKey">十六进制密钥字符串</param>
        /// <param name="hexStringIV">十六进制向量字符串</param>
        /// <param name="source">源字符串</param>
        /// <returns>加密后的字节数组</returns>
        public static byte[] TripleDesEncrypt(string hexStringKey, string hexStringIV, string source)
        {
            var key = hexStringKey.HexStringToByteArray();
            var iv = hexStringIV.HexStringToByteArray();
            return TripleDesEncrypt(key, iv, source);
        }

        /// <summary>
        ///     3Des加密，密钥长度必需是24字节
        /// </summary>
        /// <param name="hexStringKey">十六进制密钥字符串</param>
        /// <param name="hexStringIV">十六进制向量字符串</param>
        /// <param name="source">源字符串</param>
        /// <returns>加密后的Base64编码字符串</returns>
        public static string TripleDesEncryptToBase64(string hexStringKey, string hexStringIV, string source)
        {
            var result = TripleDesEncrypt(hexStringKey, hexStringIV, source);
            return Convert.ToBase64String(result);
        }

        /// <summary>
        ///     3Des加密，密钥长度必需是24字节
        /// </summary>
        /// <param name="hexStringKey">十六进制密钥字符串</param>
        /// <param name="hexStringIV">十六进制向量字符串</param>
        /// <param name="source">源字符串</param>
        /// <returns>加密后的十六进制字符串</returns>
        public static string TripleDesEncryptToHex(string hexStringKey, string hexStringIV, string source)
        {
            var result = TripleDesEncrypt(hexStringKey, hexStringIV, source);
            return result.ByteArrayToHexString();
        }

        /// <summary>
        ///     3Des加密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="source">源字节数组</param>
        /// <returns>加密后的字节数组</returns>
        public static byte[] TripleDesEncrypt(byte[] key, byte[] source)
        {
            var iv = GetDefaultIVFromKey(key);
            return TripleDesEncrypt(key, iv, source);
        }

        /// <summary>
        ///     3Des加密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="source">源字节数组</param>
        /// <returns>加密后的Base64编码字符串</returns>
        public static string TripleDesEncryptToBase64(byte[] key, byte[] source)
        {
            var result = TripleDesEncrypt(key, source);
            return Convert.ToBase64String(result);
        }

        /// <summary>
        ///     3Des加密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="source">源字节数组</param>
        /// <returns>加密后的十六进制字符串</returns>
        public static string TripleDesEncryptToHex(byte[] key, byte[] source)
        {
            var result = TripleDesEncrypt(key, source);
            return result.ByteArrayToHexString();
        }

        /// <summary>
        ///     3Des加密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="hexStringKey">十六进制密钥字符串</param>
        /// <param name="source">源字节数组</param>
        /// <returns>加密后的字节数组</returns>
        public static byte[] TripleDesEncrypt(string hexStringKey, byte[] source)
        {
            var key = hexStringKey.HexStringToByteArray();
            return TripleDesEncrypt(key, source);
        }

        /// <summary>
        ///     3Des加密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="hexStringKey">十六进制密钥字符串</param>
        /// <param name="source">源字节数组</param>
        /// <returns>加密后的Base64编码字符串</returns>
        public static string TripleDesEncryptToBase64(string hexStringKey, byte[] source)
        {
            var result = TripleDesEncrypt(hexStringKey, source);
            return Convert.ToBase64String(result);
        }

        /// <summary>
        ///     3Des加密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="hexStringKey">十六进制密钥字符串</param>
        /// <param name="source">源字节数组</param>
        /// <returns>加密后的十六进制字符串</returns>
        public static string TripleDesEncryptToHex(string hexStringKey, byte[] source)
        {
            var result = TripleDesEncrypt(hexStringKey, source);
            return result.ByteArrayToHexString();
        }

        /// <summary>
        ///     3Des加密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="source">源字符串</param>
        /// <returns>加密后的字节数组</returns>
        public static byte[] TripleDesEncrypt(byte[] key, string source)
        {
            var encryptSource = Encoding.UTF8.GetBytes(source);
            return TripleDesEncrypt(key, encryptSource);
        }

        /// <summary>
        ///     3Des加密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="source">源字符串</param>
        /// <returns>加密后的Base64编码字符串</returns>
        public static string TripleDesEncryptToBase64(byte[] key, string source)
        {
            var result = TripleDesEncrypt(key, source);
            return Convert.ToBase64String(result);
        }

        /// <summary>
        ///     3Des加密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="source">源字符串</param>
        /// <returns>加密后的十六进制字符串</returns>
        public static string TripleDesEncryptToHex(byte[] key, string source)
        {
            var result = TripleDesEncrypt(key, source);
            return result.ByteArrayToHexString();
        }

        /// <summary>
        ///     3Des加密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="hexStringKey">十六进制密钥字符串</param>
        /// <param name="source">源字符串</param>
        /// <returns>加密后的字节数组</returns>
        public static byte[] TripleDesEncrypt(string hexStringKey, string source)
        {
            var key = hexStringKey.HexStringToByteArray();
            return TripleDesEncrypt(key, source);
        }

        /// <summary>
        ///     3Des加密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="hexStringKey">十六进制密钥字符串</param>
        /// <param name="source">源字符串</param>
        /// <returns>加密后的Base64编码字符串</returns>
        public static string TripleDesEncryptToBase64(string hexStringKey, string source)
        {
            var result = TripleDesEncrypt(hexStringKey, source);
            return Convert.ToBase64String(result);
        }

        /// <summary>
        ///     3Des加密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="hexStringKey">十六进制密钥字符串</param>
        /// <param name="source">源字符串</param>
        /// <returns>加密后的十六进制字符串</returns>
        public static string TripleDesEncryptToHex(string hexStringKey, string source)
        {
            var result = TripleDesEncrypt(hexStringKey, source);
            return result.ByteArrayToHexString();
        }

        /// <summary>
        ///     3Des加密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="source">源字节数组</param>
        /// <returns>加密后的字节数组</returns>
        public static byte[] TripleDesEncrypt(byte[] source)
        {
            var iv = GetDefaultIVFromKey(Default3DesKey);
            return TripleDesEncrypt(Default3DesKey, iv, source);
        }

        /// <summary>
        ///     3Des加密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="source">源字节数组</param>
        /// <returns>加密后的Base64编码字符串</returns>
        public static string TripleDesEncryptToBase64(byte[] source)
        {
            var result = TripleDesEncrypt(source);
            return Convert.ToBase64String(result);
        }

        /// <summary>
        ///     3Des加密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="source">源字节数组</param>
        /// <returns>加密后的十六进制字符串</returns>
        public static string TripleDesEncryptToHex(byte[] source)
        {
            var result = TripleDesEncrypt(source);
            return result.ByteArrayToHexString();
        }

        /// <summary>
        ///     3Des加密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <returns>加密后的字节数组</returns>
        public static byte[] TripleDesEncrypt(string source)
        {
            var encryptSource = Encoding.UTF8.GetBytes(source);
            return TripleDesEncrypt(encryptSource);
        }

        /// <summary>
        ///     3Des加密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <returns>加密后的Base64编码字符串</returns>
        public static string TripleDesEncryptToBase64(string source)
        {
            var result = TripleDesEncrypt(source);
            return Convert.ToBase64String(result);
        }

        /// <summary>
        ///     3Des加密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <returns>加密后的十六进制字符串</returns>
        public static string TripleDesEncryptToHex(string source)
        {
            var result = TripleDesEncrypt(source);
            return result.ByteArrayToHexString();
        }

        #endregion

        #region TripleDesDecrypt 3Des解密

        /// <summary>
        ///     3Des解密，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="iv">向量字节数组</param>
        /// <param name="source">加密后的字节数组</param>
        /// <param name="dataLen">解密后的数据长度</param>
        /// <returns>解密后的字节数组</returns>
        public static byte[] TripleDesDecrypt(byte[] key, byte[] iv, byte[] source, out int dataLen)
        {
            var dsp = new TripleDESCryptoServiceProvider();
            dsp.Mode = CipherMode.CBC; // 默认值
            dsp.Padding = PaddingMode.PKCS7; // 默认值
            using (var mStream = new MemoryStream(source))
            {
                var cStream = new CryptoStream(mStream, dsp.CreateDecryptor(key, iv), CryptoStreamMode.Read);
                var result = new byte[source.Length];
                //byte[] result = new byte[cStream.Length]; // cStream.Length 不可读取
                dataLen = cStream.Read(result, 0, result.Length);
                cStream.Close();
                mStream.Close();
                return result;
            }
        }

        /// <summary>
        ///     3Des解密，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="iv">向量字节数组</param>
        /// <param name="source">加密后的字节数组</param>
        /// <returns>解密后的字节数组</returns>
        public static byte[] TripleDesDecrypt(byte[] key, byte[] iv, byte[] source)
        {
            var dataLen = 0;

            var result = TripleDesDecrypt(key, iv, source, out dataLen);

            if (result.Length != dataLen)
            {
                // 如果数组长度不是解密后的实际长度，需要截断多余的数据，用来解决Gzip的"Magic byte doesn't match"的问题
                var resultToReturn = new byte[dataLen];
                Array.Copy(result, resultToReturn, dataLen);
                return resultToReturn;
            }
            return result;
        }

        /// <summary>
        ///     3Des解密，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="iv">向量字节数组</param>
        /// <param name="base64Source">加密后的字节数组</param>
        /// <returns>解密后的字节数组</returns>
        public static byte[] TripleDesDecryptFromBase64(byte[] key, byte[] iv, string base64Source)
        {
            var encryptSource = Convert.FromBase64String(base64Source);
            return TripleDesDecrypt(key, iv, encryptSource);
        }

        /// <summary>
        ///     3Des解密，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="iv">向量字节数组</param>
        /// <param name="source">加密后的十六进制字符串</param>
        /// <returns>解密后的字节数组</returns>
        public static byte[] TripleDesDecryptFromHex(byte[] key, byte[] iv, string source)
        {
            var encryptSource = source.HexStringToByteArray();
            return TripleDesDecrypt(key, iv, encryptSource);
        }

        /// <summary>
        ///     3Des解密，密钥长度必需是24字节
        /// </summary>
        /// <param name="hexStringKey">十六进制密钥字符串</param>
        /// <param name="hexStringIV">十六进制向量字符串</param>
        /// <param name="source">加密后的字节数组</param>
        /// <returns>解密后的字节数组</returns>
        public static byte[] TripleDesDecrypt(string hexStringKey, string hexStringIV, byte[] source)
        {
            var key = hexStringKey.HexStringToByteArray();
            var iv = hexStringIV.HexStringToByteArray();
            return TripleDesDecrypt(key, iv, source);
        }

        /// <summary>
        ///     3Des解密，密钥长度必需是24字节
        /// </summary>
        /// <param name="hexStringKey">十六进制密钥字符串</param>
        /// <param name="hexStringIV">十六进制向量字符串</param>
        /// <param name="base64Source">加密后的字节数组</param>
        /// <returns>解密后的字节数组</returns>
        public static byte[] TripleDesDecryptFromBase64(string hexStringKey, string hexStringIV, string base64Source)
        {
            var encryptSource = Convert.FromBase64String(base64Source);
            var key = hexStringKey.HexStringToByteArray();
            var iv = hexStringIV.HexStringToByteArray();
            return TripleDesDecrypt(key, iv, encryptSource);
        }

        /// <summary>
        ///     3Des解密，密钥长度必需是24字节
        /// </summary>
        /// <param name="hexStringKey">十六进制密钥字符串</param>
        /// <param name="hexStringIV">十六进制向量字符串</param>
        /// <param name="source">加密后的十六进制字符串</param>
        /// <returns>解密后的字节数组</returns>
        public static byte[] TripleDesDecryptFromHex(string hexStringKey, string hexStringIV, string source)
        {
            var encryptSource = source.HexStringToByteArray();
            var key = hexStringKey.HexStringToByteArray();
            var iv = hexStringIV.HexStringToByteArray();
            return TripleDesDecrypt(key, iv, encryptSource);
        }

        /// <summary>
        ///     3Des解密，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="iv">向量字节数组</param>
        /// <param name="encryptSource">加密后的字节数组</param>
        /// <returns>源字符串</returns>
        public static string TripleDesDecryptToString(byte[] key, byte[] iv, byte[] encryptSource)
        {
            int dataLen;
            var result = TripleDesDecrypt(key, iv, encryptSource, out dataLen);
            return Encoding.UTF8.GetString(result, 0, dataLen).TrimEnd('\0');
        }

        /// <summary>
        ///     3Des解密，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="iv">向量字节数组</param>
        /// <param name="source">加密后的Base64编码字符串</param>
        /// <returns>源字符串</returns>
        public static string TripleDesDecryptFromBase64ToString(byte[] key, byte[] iv, string source)
        {
            var encryptSource = Convert.FromBase64String(source);
            return TripleDesDecryptToString(key, iv, encryptSource);
        }

        /// <summary>
        ///     3Des解密，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="iv">向量字节数组</param>
        /// <param name="source">加密后的十六进制字符串</param>
        /// <returns>源字符串</returns>
        public static string TripleDesDecryptFromHexToString(byte[] key, byte[] iv, string source)
        {
            var encryptSource = source.HexStringToByteArray();
            return TripleDesDecryptToString(key, iv, encryptSource);
        }

        /// <summary>
        ///     3Des解密，密钥长度必需是24字节
        /// </summary>
        /// <param name="hexStringKey">十六进制密钥字符串</param>
        /// <param name="hexStringIV">十六进制向量字符串</param>
        /// <param name="encryptSource">加密后的字节数组</param>
        /// <returns>源字符串</returns>
        public static string TripleDesDecryptToString(string hexStringKey, string hexStringIV, byte[] encryptSource)
        {
            var key = hexStringKey.HexStringToByteArray();
            var iv = hexStringIV.HexStringToByteArray();
            return TripleDesDecryptToString(key, iv, encryptSource);
        }

        /// <summary>
        ///     3Des解密，密钥长度必需是24字节
        /// </summary>
        /// <param name="hexStringKey">十六进制密钥字符串</param>
        /// <param name="hexStringIV">十六进制向量字符串</param>
        /// <param name="source">加密后的Base64编码字符串</param>
        /// <returns>源字符串</returns>
        public static string TripleDesDecryptFromBase64ToString(string hexStringKey, string hexStringIV, string source)
        {
            var encryptSource = Convert.FromBase64String(source);
            return TripleDesDecryptToString(hexStringKey, hexStringIV, encryptSource);
        }

        /// <summary>
        ///     3Des解密，密钥长度必需是24字节
        /// </summary>
        /// <param name="hexStringKey">十六进制密钥字符串</param>
        /// <param name="hexStringIV">十六进制向量字符串</param>
        /// <param name="source">加密后的十六进制字符串</param>
        /// <returns>源字符串</returns>
        public static string TripleDesDecryptFromHexToString(string hexStringKey, string hexStringIV, string source)
        {
            var encryptSource = source.HexStringToByteArray();
            return TripleDesDecryptToString(hexStringKey, hexStringIV, encryptSource);
        }

        /// <summary>
        ///     3Des解密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="source">加密后的字节数组</param>
        /// <returns>解密后的字节数组</returns>
        public static byte[] TripleDesDecrypt(byte[] key, byte[] source)
        {
            var iv = GetDefaultIVFromKey(key);
            return TripleDesDecrypt(key, iv, source);
        }

        /// <summary>
        ///     3Des解密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="base64Source">加密后的字节数组</param>
        /// <returns>解密后的字节数组</returns>
        public static byte[] TripleDesDecryptFromBase64(byte[] key, string base64Source)
        {
            var encryptSource = Convert.FromBase64String(base64Source);
            return TripleDesDecrypt(key, encryptSource);
        }

        /// <summary>
        ///     3Des解密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="source">加密后的十六进制字符串</param>
        /// <returns>解密后的字节数组</returns>
        public static byte[] TripleDesDecryptFromHex(byte[] key, string source)
        {
            var encryptSource = source.HexStringToByteArray();
            return TripleDesDecrypt(key, encryptSource);
        }

        /// <summary>
        ///     3Des解密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="hexStringKey">十六进制密钥字符串</param>
        /// <param name="source">加密后的字节数组</param>
        /// <returns>解密后的字节数组</returns>
        public static byte[] TripleDesDecrypt(string hexStringKey, byte[] source)
        {
            var key = hexStringKey.HexStringToByteArray();
            return TripleDesDecrypt(key, source);
        }

        /// <summary>
        ///     3Des解密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="hexStringKey">十六进制密钥字符串</param>
        /// <param name="base64Source">加密后的字节数组</param>
        /// <returns>解密后的字节数组</returns>
        public static byte[] TripleDesDecryptFromBase64(string hexStringKey, string base64Source)
        {
            var encryptSource = Convert.FromBase64String(base64Source);
            var key = hexStringKey.HexStringToByteArray();
            return TripleDesDecrypt(key, encryptSource);
        }

        /// <summary>
        ///     3Des解密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="hexStringKey">十六进制密钥字符串</param>
        /// <param name="source">加密后的十六进制字符串</param>
        /// <returns>解密后的字节数组</returns>
        public static byte[] TripleDesDecryptFromHex(string hexStringKey, string source)
        {
            var encryptSource = source.HexStringToByteArray();
            var key = hexStringKey.HexStringToByteArray();
            return TripleDesDecrypt(key, encryptSource);
        }

        /// <summary>
        ///     3Des解密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="encryptSource">加密后的字节数组</param>
        /// <returns>源字符串</returns>
        public static string TripleDesDecryptToString(byte[] key, byte[] encryptSource)
        {
            var result = TripleDesDecrypt(key, encryptSource);
            return Encoding.UTF8.GetString(result).TrimEnd('\0');
        }

        /// <summary>
        ///     3Des解密，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="source">加密后的Base64编码字符串</param>
        /// <returns>源字符串</returns>
        public static string TripleDesDecryptFromBase64ToString(byte[] key, string source)
        {
            var encryptSource = Convert.FromBase64String(source);
            return TripleDesDecryptToString(key, encryptSource);
        }

        /// <summary>
        ///     3Des解密，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="source">加密后的十六进制字符串</param>
        /// <returns>源字符串</returns>
        public static string TripleDesDecryptFromHexToString(byte[] key, string source)
        {
            var encryptSource = source.HexStringToByteArray();
            return TripleDesDecryptToString(key, encryptSource);
        }

        /// <summary>
        ///     3Des解密，密钥长度必需是24字节
        /// </summary>
        /// <param name="hexStringKey">十六进制密钥字符串</param>
        /// <param name="encryptSource">加密后的字节数组</param>
        /// <returns>源字符串</returns>
        public static string TripleDesDecryptToString(string hexStringKey, byte[] encryptSource)
        {
            var key = hexStringKey.HexStringToByteArray();
            return TripleDesDecryptToString(key, encryptSource);
        }

        /// <summary>
        ///     3Des解密，密钥长度必需是24字节
        /// </summary>
        /// <param name="hexStringKey">十六进制密钥字符串</param>
        /// <param name="source">加密后的Base64编码字符串</param>
        /// <returns>源字符串</returns>
        public static string TripleDesDecryptFromBase64ToString(string hexStringKey, string source)
        {
            var encryptSource = Convert.FromBase64String(source);
            return TripleDesDecryptToString(hexStringKey, encryptSource);
        }

        /// <summary>
        ///     3Des解密，密钥长度必需是24字节
        /// </summary>
        /// <param name="hexStringKey">十六进制密钥字符串</param>
        /// <param name="source">加密后的十六进制字符串</param>
        /// <returns>源字符串</returns>
        public static string TripleDesDecryptFromHexToString(string hexStringKey, string source)
        {
            var encryptSource = source.HexStringToByteArray();
            return TripleDesDecryptToString(hexStringKey, encryptSource);
        }

        /// <summary>
        ///     3Des解密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="source">加密后的字节数组</param>
        /// <returns>解密后的字节数组</returns>
        public static byte[] TripleDesDecrypt(byte[] source)
        {
            var iv = GetDefaultIVFromKey(Default3DesKey);
            return TripleDesDecrypt(Default3DesKey, iv, source);
        }

        /// <summary>
        ///     3Des解密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="base64Source">加密后的字节数组</param>
        /// <returns>解密后的字节数组</returns>
        public static byte[] TripleDesDecryptFromBase64(string base64Source)
        {
            var encryptSource = Convert.FromBase64String(base64Source);
            return TripleDesDecrypt(encryptSource);
        }

        /// <summary>
        ///     3Des解密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="source">加密后的十六进制字符串</param>
        /// <returns>解密后的字节数组</returns>
        public static byte[] TripleDesDecryptFromHex(string source)
        {
            var encryptSource = source.HexStringToByteArray();
            return TripleDesDecrypt(encryptSource);
        }

        /// <summary>
        ///     3Des解密(默认向量)，密钥长度必需是24字节
        /// </summary>
        /// <param name="encryptSource">加密后的字节数组</param>
        /// <returns>源字符串</returns>
        public static string TripleDesDecryptToString(byte[] encryptSource)
        {
            var result = TripleDesDecrypt(encryptSource);
            return Encoding.UTF8.GetString(result).TrimEnd('\0');
        }

        /// <summary>
        ///     3Des解密，密钥长度必需是24字节
        /// </summary>
        /// <param name="source">加密后的Base64编码字符串</param>
        /// <returns>源字符串</returns>
        public static string TripleDesDecryptFromBase64ToString(string source)
        {
            var encryptSource = Convert.FromBase64String(source);
            return TripleDesDecryptToString(encryptSource);
        }

        /// <summary>
        ///     3Des解密，密钥长度必需是24字节
        /// </summary>
        /// <param name="source">加密后的十六进制字符串</param>
        /// <returns>源字符串</returns>
        public static string TripleDesDecryptFromHexToString(string source)
        {
            var encryptSource = source.HexStringToByteArray();
            return TripleDesDecryptToString(encryptSource);
        }

        #endregion

        #region AES加密

        /// <summary>
        /// 获取密钥
        /// </summary>
        private static string DefaultAESKey
        {
            get { return @")O[NB]6,YF}+efcaj{+oESb9d8>Z'e9M"; }
        }

        /// <summary>
        /// 默认密钥向量
        /// </summary>
        private static byte[] DefaultAESIV = { 0x41, 0x72, 0x65, 0x79, 0x6F, 0x75, 0x6D, 0x79, 0x53, 0x6E, 0x6F, 0x77, 0x6D, 0x61, 0x6E, 0x3F };

        /// <summary>
        /// AES 加密
        /// </summary>
        /// <param name="encryptString"></param>
        /// <param name="encryptKey"></param>
        /// <returns></returns>
        public static string AESEncrypt(string encryptString)
        {
            return AESEncrypt(encryptString, DefaultAESKey);
        }

        /// <summary>
        /// AES 加密
        /// </summary>
        /// <param name="encryptString"></param>
        /// <param name="encryptKey"></param>
        /// <returns></returns>
        public static string AESEncrypt(string encryptString, string encryptKey)
        {
            encryptKey = StringUtil.GetSubString(encryptKey, 16, "");
            encryptKey = encryptKey.PadRight(16, ' ');

            RijndaelManaged rijndaelProvider = new RijndaelManaged();
            rijndaelProvider.Mode = CipherMode.CBC;
            rijndaelProvider.Padding = PaddingMode.PKCS7;
            rijndaelProvider.Key = Encoding.UTF8.GetBytes(encryptKey.Substring(0, 16));
            rijndaelProvider.IV = Encoding.UTF8.GetBytes(encryptKey.Substring(0, 16)); //DefaultAESIV;
            ICryptoTransform rijndaelEncrypt = rijndaelProvider.CreateEncryptor();

            byte[] inputData = Encoding.UTF8.GetBytes(encryptString);
            byte[] encryptedData = rijndaelEncrypt.TransformFinalBlock(inputData, 0, inputData.Length);

            return Convert.ToBase64String(encryptedData);
        }

        /// <summary>
        /// AES 解密
        /// </summary>
        /// <param name="decryptString"></param>
        /// <returns></returns>
        public static string AESDecrypt(string decryptString)
        {
            return AESDecrypt(decryptString, DefaultAESKey);
        }

        /// <summary>
        /// AES 解密
        /// </summary>
        /// <param name="decryptString"></param>
        /// <param name="decryptKey"></param>
        /// <returns></returns>
        public static string AESDecrypt(string decryptString, string decryptKey)
        {
            try
            {
                decryptKey = StringUtil.GetSubString(decryptKey, 16, "");
                decryptKey = decryptKey.PadRight(16, ' ');

                RijndaelManaged rijndaelProvider = new RijndaelManaged();
                rijndaelProvider.Mode = CipherMode.CBC;
                rijndaelProvider.Padding = PaddingMode.PKCS7;
                rijndaelProvider.Key = Encoding.UTF8.GetBytes(decryptKey);
                rijndaelProvider.IV = Encoding.UTF8.GetBytes(decryptKey); //DefaultAESIV;
                ICryptoTransform rijndaelDecrypt = rijndaelProvider.CreateDecryptor();

                byte[] inputData = Convert.FromBase64String(decryptString);
                byte[] decryptedData = rijndaelDecrypt.TransformFinalBlock(inputData, 0, inputData.Length);

                return Encoding.UTF8.GetString(decryptedData);
            }
            catch
            {
                return "";
            }

        }

        public static string AESDecrypt(string decryptString, string decryptKey, string iv)
        {
            try
            {
                //decryptKey = StringUtil.GetSubString(decryptKey, 16, "");
                //decryptKey = decryptKey.PadRight(16, ' ');

                RijndaelManaged rijndaelProvider = new RijndaelManaged();
                rijndaelProvider.Mode = CipherMode.CBC;
                rijndaelProvider.Padding = PaddingMode.PKCS7;
                rijndaelProvider.Key = Encoding.UTF8.GetBytes(decryptKey);
                rijndaelProvider.IV = Encoding.UTF8.GetBytes(iv); //DefaultAESIV;
                ICryptoTransform rijndaelDecrypt = rijndaelProvider.CreateDecryptor();

                byte[] inputData = Convert.FromBase64String(decryptString);
                byte[] decryptedData = rijndaelDecrypt.TransformFinalBlock(inputData, 0, inputData.Length);

                return Encoding.UTF8.GetString(decryptedData);
            }
            catch
            {
                return "";
            }

        }

        #endregion

        #region RSA 
        /// <summary>
        /// generate RSA secret key
        /// </summary>
        /// <param name="keySize">the size of the key,must from 384 bits to 16384 bits in increments of 8 </param>
        /// <returns></returns>
        public static RSASecretKey GenerateRSASecretKey(int keySize)
        {
            RSASecretKey rsaKey = new RSASecretKey();
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(keySize))
            {
                rsaKey.PrivateKey = rsa.ToXmlString(true);
                rsaKey.PublicKey = rsa.ToXmlString(false);
            }
            return rsaKey;
        }

        public static string RSAEncrypt(string xmlPublicKey, string content)
        {
            string encryptedContent = string.Empty;
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(xmlPublicKey);
                byte[] encryptedData = rsa.Encrypt(Encoding.Default.GetBytes(content), false);
                encryptedContent = Convert.ToBase64String(encryptedData);
            }
            return encryptedContent;
        }

        public static string RSADecrypt(string xmlPrivateKey, string content)
        {
            string decryptedContent = string.Empty;
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(xmlPrivateKey);
                byte[] decryptedData = rsa.Decrypt(Convert.FromBase64String(content), false);
                decryptedContent = Encoding.GetEncoding("gb2312").GetString(decryptedData);
            }
            return decryptedContent;
        }

        /// <summary>
        /// 私钥生成签名
        /// </summary>
        /// <param name="xmlPrivateKey"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string RSASign(string xmlPrivateKey, string content)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(xmlPrivateKey);

                var paras = rsa.ExportParameters(true);
                rsa.ImportParameters(paras);
                using (var sha256 = new SHA256CryptoServiceProvider())
                {
                    var signData = rsa.SignData(Encoding.UTF8.GetBytes(content), sha256);
                    return Convert.ToBase64String(signData);
                }
            }
        }
        /// <summary>
        /// 公钥验签
        /// </summary>
        /// <param name="xmlPublicKey"></param>
        /// <param name="sign"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static bool RSAVerifySign(string xmlPublicKey,string sign, string content)
        {
            try
            {
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(xmlPublicKey);

                    byte[] signature = Convert.FromBase64String(sign);
                    SHA256Managed sha256 = new SHA256Managed();
                    RSAPKCS1SignatureDeformatter df = new RSAPKCS1SignatureDeformatter(rsa);
                    df.SetHashAlgorithm("SHA256");
                    byte[] compareByte = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));

                    return df.VerifySignature(compareByte, signature);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion
    }
    #region RSA 
    public struct  RSASecretKey
    {
        public RSASecretKey(string privateKey, string publicKey)
        {
            PrivateKey = privateKey;
            PublicKey = publicKey;
        }
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
        public override string ToString()
        {
            return string.Format(
                "PrivateKey: {0}\r\nPublicKey: {1}", PrivateKey, PublicKey);
        }
    }
    #endregion
}
