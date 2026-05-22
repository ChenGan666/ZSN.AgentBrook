using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZSN.AI.Entity
{
    public enum ErrorCode
    {
        None = 10000,
        Timeout = 10001,
        Error = 40001,
        DataEmpty = 40002,
        ParameterError = 40003,
        ServerError = 40004,
        InvalidParameter = 40005,
        Endpoint404 = 40006,
        FileNotExist = 40007,
        DataNotExists = 40008,
        ParamsError = 40009,

        AccountError = 50001,
        AccountLock = 50002,
        VCodeError = 50003,
        VCodeDuplicateRequest = 50004,
        PasswordError = 50005,

        NoModel = 501001,
        NoInputs = 501002,
        Locked = 501003,

        DataFormatError = 60001,
        DataAlreadyExists = 60002,
        DataNoMemoryId = 6000101,//知识节点ID不能为空
        DataLongTermMemoryNull = 6000102,//知识节点数据不能为空
        DataRelationIdNotFound = 6000103,//知识节点关联ID不存在

        TaskStateError = 60003,
        TaskNotExists = 60004,

        WeixinMiniAppError = 70001,
        WeixinMiniAppRequestError = 70002,
        WeixinMiniAppMemberAcctokenError = 70003,

        TokenCheckError = 80001,
        MemberTokenCheckError = 80002,
        RefreshTokenError = 80003,
        RefreshTokenErrorNODeviceID = 80004,

        SignError = 90001,
        MemberSignError = 90002,
        TimestampError = 90003,
    }

    public enum SystemStatus
    {
        Normal = 0,
        Locked = -1,
        Unpublished = 0,
        Published = 1,
        Unaudited = 0,
        Audited = 1,
    }
    /// <summary>
    /// 返回消息
    /// </summary>
    public class JsonMsg<T> where T : class
    {
        /// <summary>
        /// 成功失败
        /// </summary>
        public bool Status { get; set; } = false;
        public bool Success { get; set; } = false;
        /// <summary>
        /// 状态码
        /// </summary>
        public int ErrorCode { get; set; }
        public string SessionID { get; set; } = string.Empty;

        /// <summary>
        /// 消息
        /// </summary>
        public string ErrorDesc { get; set; } = string.Empty;

        /// <summary>
        /// 内容
        /// </summary>
        public T Data { get; set; }


        public static JsonMsg<T> OK(T obj, string msg = "Success", string SessionID = "")
        {

            return new JsonMsg<T>() { Status = true, Success = true, ErrorCode = 0, ErrorDesc = msg, SessionID = SessionID, Data = obj };

        }

        public static JsonMsg<T> Error(T obj, ErrorCode errorCode)
        {
            return new JsonMsg<T>() { Status = false, Success = false, ErrorCode = (int)errorCode, ErrorDesc = errorCode.ToString(), Data = obj };
        }


    }
    public class PageData<T> where T : class
    {
        public T Data { get; set; }
        public int pagetotal { get; set; }
        public int total { get; set; }
    }

    /// <summary>
    /// API请求数据结构
    /// </summary>
    public class ApiRequest
    {
        public string AppID { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public string Sign { get; set; } = string.Empty;
    }

    /// <summary>
    /// API响应数据结构
    /// </summary>
    /// <typeparam name="T">业务数据类型</typeparam>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    /// <summary>
    /// 令牌响应类
    /// </summary>
    public class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public long Expirein { get; set; }
        public string MemberToken { get; set; } = string.Empty;
        public string MemberRefreshToken { get; set; } = string.Empty;
    }

    public class FileInfo
    {
        public string FileCode { get; set; }
        public string Url { get; set; }
    }
    
    public class SessionStatusInfo
    {
        public string ChatSessionID { get; set; } = string.Empty;
        public int SessionStatus { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string TopicSummary { get; set; } = string.Empty;
        public string AppID { get; set; } = string.Empty;
    }

    public class BaseInfo
    {
        public CompanyInfo CompanyInfo { get; set; } = new CompanyInfo();
        public List<AppInfo> AppList { get; set; } = new List<AppInfo>();
        public List<BaseDictionaryInfo> TagClassList { get; set; } = new List<BaseDictionaryInfo>();
        public List<SessionStatusInfo> SessionStatusList { get; set; } = new List<SessionStatusInfo>();
    }
}
