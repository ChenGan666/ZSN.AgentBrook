using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Service.Controllers;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AI.Service.Token
{
    public class MemberTokenHelper: CommonApiBaseController
    {
        public MemberTokenHelper()
        {
        }
        public static void Set(string MemberID, int MemberOtherAuthID, RedisHelper redis, out string MemberToken, out string RefreshToken)
        {
            MemberToken = GetMemberTokenByUserId(MemberID, MemberOtherAuthID);
            RefreshToken = GetMemberRefreshToken(MemberID, MemberToken);

            MemberAuthInfo memberAuthInfo = new MemberAuthInfo();
            memberAuthInfo.MemberID = MemberID;
            memberAuthInfo.AccessToken = MemberToken;
            memberAuthInfo.RefreshToken = RefreshToken;
            memberAuthInfo.MaAppendTime = DateTime.Now;
            memberAuthInfo.MaUpdateTime = DateTime.Now;

            redis?.StringSet(MemberID, JsonConvert.SerializeObject(memberAuthInfo), TimeSpan.FromMilliseconds(ConfigHelper.GetInt("SignInStepTimeOut", 1000)));

            MemberAuthInfo _MemberAuthInfo = MemberAuthInfoBussiness.GetModel(MemberID);
            if (_MemberAuthInfo != null)
            {
                memberAuthInfo.MemberAuthID = _MemberAuthInfo.MemberAuthID;
                MemberAuthInfoBussiness.Update(memberAuthInfo);
            }
            else
            {
                MemberAuthInfoBussiness.Add(memberAuthInfo);
            }
        }
    }
}
