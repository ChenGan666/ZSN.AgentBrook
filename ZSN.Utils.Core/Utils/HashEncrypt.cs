using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using ZSN.Utils.Core.Helpers;
using ZSN.Utils.Core.Extensions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ZSN.Utils.Core.Utils
{
    public class HashEncrypt
    {
        private bool isCaseSensitive = true;
        private bool isReturnNum;
        public HashEncrypt()
        {
        }

        public HashEncrypt(bool IsCaseSensitive, bool IsReturnNum)
        {
            this.isReturnNum = IsReturnNum;
            this.isCaseSensitive = IsCaseSensitive;
        }

        private string getstrIN(string strIN)
        {
            if (strIN.Length == 0)
                strIN = "~NULL~";
            if (!this.isCaseSensitive)
                strIN = strIN.ToUpper();
            return strIN;
        }

        public string MD5Encrypt(string strIN)
        {
            if (strIN.IsNullOrEmpty())
                return string.Empty;

            using (var md5 = MD5.Create())
            {
                byte[] bytes = md5.ComputeHash(this.GetKeyByteArray(this.getstrIN(strIN)));
                return this.GetStringValue(bytes);
            }

        }

        public string GetMD5HashFromFile(string fileFullPath)
        {
            try
            {
                FileStream file = new FileStream(fileFullPath, FileMode.Open);
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("GetMD5HashFromFile() fail,error:" + ex.Message);
            }
        }

        public string SHA1Encrypt(string strIN)
        {
            if (strIN.IsNullOrEmpty())
                return string.Empty;
            SHA1CryptoServiceProvider cryptoServiceProvider = new SHA1CryptoServiceProvider();
            byte[] hash = cryptoServiceProvider.ComputeHash(this.GetKeyByteArray(strIN));
            cryptoServiceProvider.Clear();
            return this.GetStringValue(hash);
        }

        public string SHA256Encrypt(string strIN)
        {
            if (strIN.IsNullOrEmpty())
                return string.Empty;
            SHA256Managed shA256Managed = new SHA256Managed();
            byte[] hash = shA256Managed.ComputeHash(this.GetKeyByteArray(strIN));
            shA256Managed.Clear();
            return this.GetStringValue(hash);
        }

        public string SHA512Encrypt(string strIN)
        {
            if (strIN.IsNullOrEmpty())
                return string.Empty;
            SHA512Managed shA512Managed = new SHA512Managed();
            byte[] hash = shA512Managed.ComputeHash(this.GetKeyByteArray(strIN));
            shA512Managed.Clear();
            return this.GetStringValue(hash);
        }

        private string GetStringValue(byte[] Byte)
        {
            string str = "";
            if (!this.isReturnNum)
            {
                str = new ASCIIEncoding().GetString(Byte);
            }
            else
            {
                for (int index = 0; index < Byte.Length; ++index)
                    str += Byte[index].ToString();
            }
            return str;
        }

        private byte[] GetKeyByteArray(string strKey)
        {
            ASCIIEncoding asciiEncoding = new ASCIIEncoding();
            byte[] numArray = new byte[strKey.Length - 1];
            string s = strKey;
            return asciiEncoding.GetBytes(s);
        }

        public string MD5System(string strIN)
        {
            if (!strIN.IsNullOrEmpty())
                return EncryptHelper.MD5Encrypt(strIN);
            return "";
        }
    }
}
