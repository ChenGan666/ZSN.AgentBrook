using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;



namespace Yannyo.Common
{
    class MessageHelper
    {
    }



    public static class EmailHelper
    {

        /// <summary>
        /// 发送邮件
        /// </summary>
        /// <param name="subject">邮件主题</param>
        /// <param name="msg">邮件内容</param>
        /// <param name="filePath">附件地址，如果不添加附件传null或""</param>
        /// <param name="senderEmail">发送人邮箱地址</param>
        /// <param name="senderPwd">发送人邮箱密码</param>
        /// <param name="recipientEmail">接收人邮箱</param>
        /// 

        //string subject, string msg,  string senderEmail, string senderPwd, params string[] recipientEmail
        public static void SendEmialMessage(string SmtpServiceAddress,  string senderEmail, string senderPwd, EmailMessage EmialMessage,  string recipientEmail)//邮件发送通过smtp协议方式发送邮件    
        {


            if (!CheckIsNotEmptyOrNull(SmtpServiceAddress, senderEmail, senderPwd) || recipientEmail == null || recipientEmail.Length == 0|| EmialMessage==null)
            {
                throw new Exception("输入信息无效");
            }

            SmtpClient client = new SmtpClient(SmtpServiceAddress); //"smtp.qq.com"

            client.EnableSsl = true;

            client.UseDefaultCredentials = false;

            client.Credentials = new System.Net.NetworkCredential(senderEmail, senderPwd);//"itgzvkrnhxgrbjhe"

            MailAddress from = new MailAddress(senderEmail, "", Encoding.UTF8);//初始化发件人  

            MailAddress  to = new MailAddress(recipientEmail, "", Encoding.UTF8);//初始化收件人  

            //设置邮件内容  
            MailMessage message = new MailMessage(from, to);

            message.Subject = EmialMessage.Subject;
            message.SubjectEncoding = EmialMessage.SubjectEncoding;
            message.IsBodyHtml = EmialMessage.IsBodyHtml;// true;// mail.IsBodyHtml;
            message.Body = EmialMessage.Body;//"<a href='www.baidu.com'>链接跳转<a/>";
            message.BodyEncoding = EmialMessage.BodyEncoding;
            //发送邮件  
            try
            {
                client.Send(message);
            }
            catch (InvalidOperationException iex)
            {
                throw new Exception(iex.Message);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
     

        /// <summary>
        /// 验证所有传入字符串不能为空或null
        /// </summary>
        /// <param name="ps">参数列表</param>
        /// <returns>都不为空或null返回true，否则返回false</returns>
        public static bool CheckIsNotEmptyOrNull(params string[] ps)
        {
            if (ps != null)
            {
                foreach (string item in ps)
                {
                    if (string.IsNullOrEmpty(item)) return false;
                }
                return true;
            }
            return false;
        }

       

    }



    public class EmailMessage
    {
        public EmailMessage()
        {

        }
        private string _subject;
        private Encoding _subjectEncoding;
        private bool _isBodyHtml;
        private string _body;
        private Encoding _bodyEncoding;



        public string Subject
        {
            set { _subject = value; }
            get { return _subject; }

        }
        public Encoding SubjectEncoding
        {
            set { _subjectEncoding = value; }
            get { return _subjectEncoding; }
        }
        public bool IsBodyHtml
        {
              set { _isBodyHtml = value; }
              get { return _isBodyHtml; }
         }
        public string Body
        {
            set { _body = value; }
            get { return _body; }
        }

        public Encoding BodyEncoding
        {
            set { _bodyEncoding = value; }
            get { return _bodyEncoding; }
        }
      


    }

}
