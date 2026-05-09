using System;
using System.Linq;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.Utils.Core.Extensions;
using ZSN.Utils.Core.Helpers;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;


namespace ZSN.AI.Service.Helpers
{
    public class SettingsService
    {
        public static ApisettingsInfo Current => GetSetting();
        public static ApisettingsInfo GetSetting()
        {
            ApisettingsInfo setting = GetSessionSetting();// HttpContextHelper.Current.Session.Get<APISettings>("CurrentSetting");
            if (setting != null)
                return setting;
            
            var pms = HttpContextHelper.Session.GetString("BodyParams");

            //HttpContextHelper.GetBodyParams(HttpContextHelper.Current);
            if (!pms.IsNullOrEmpty())
            {
                try
                {
                    try
                    {
                        var obj = (JObject)JsonConvert.DeserializeObject(pms);
                        if (obj.ContainsKey("AppID"))
                        {
                            var appID = obj["AppID"].ToString().SecureSQL();
                            setting = ApisettingsInfoBussiness.GetModelByAppID(appID);
                        }
                    }
                    catch (Exception e) {
                        var appID = HttpContextHelper.GetQuery("appID").SecureSQL();
                        if (appID.IsNullOrEmpty() == false)
                        {
                            setting = ApisettingsInfoBussiness.GetModelByAppID(appID);
                        }
                        else
                        {
                            setting = null;
                        }
                    }

                    if (setting != null)
                    {
                        SetSetting(setting);
                    }

                }
                catch (Exception ex)
                {
                    NLogHelper.WriteException(ErrorCode.Error.ToString(), ex);
                }

            }
            
            return setting;
        }

        public static MemberSettings GetMemberSetting(string MemberID,int MemberOtherAuthID, string AccessToken,bool cache = true)
        {
            if (!MemberID.IsNullOrEmpty())
            {
                MemberSettings setting = GetSessionMemberSetting();// HttpContextHelper.Current.Session.Get<MemberSettings>("MemberSetting");
                if (setting != null && cache)
                    return setting;

                try
                {
                    setting = new MemberSettings();
                    setting.FullMember = new FullMemberInfo();
                    setting.FullMember.Member = MemberInfoBussiness.GetModel(MemberID);
                    setting.MemberOtherAuth = MemberOtherAuthInfoBussiness.GetModel(MemberOtherAuthID);
                    setting.MemberAuth = MemberAuthInfoBussiness.GetModelByAccessToken(AccessToken);
                    setting.StaffInfo = StaffInfoBussiness.GetModel(MemberID);
                    setting.UserInfo = setting.StaffInfo != null ? UserInfoBussiness.GetModel(setting.StaffInfo.UserID) : null;


                    if (setting.FullMember.Member != null && setting.MemberAuth != null)
                    {
                        if (setting.FullMember.Member.MIcon.IsNullOrEmpty() == false)
                        {
                            setting.FullMember.Member.MIcon = string.Format(ConfigHelper.GetString("previewHost"), setting.FullMember.Member.MIcon);
                        }

                        SetMemberSetting(setting);
                        return setting;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    NLogHelper.WriteException(ErrorCode.Error.ToString(), ex);
                }
                return setting;
            }
            else
            {
                return null;
            }
        }

        public static void SetSetting(ApisettingsInfo setting)
        {
            HttpContextHelper.Current.Session.Set<ApisettingsInfo>("CurrentSetting", setting);
        }

        public static ApisettingsInfo GetSessionSetting() {
            return HttpContextHelper.Current.Session.Get<ApisettingsInfo>("CurrentSetting");
        }

        public static void SetMemberSetting(MemberSettings setting)
        {
            HttpContextHelper.Current.Session.Set<MemberSettings>("MemberSetting", setting);
        }

        public static MemberSettings GetSessionMemberSetting()
        {
            return HttpContextHelper.Current.Session.Get<MemberSettings>("MemberSetting");
        }

        public static void ClearSetting()
        {
            HttpContextHelper.Current.Session.Remove("CurrentSetting");
            //HttpContextHelper.Current.Session.Remove("BodyParams");
        }

        public static void ClearMemberSetting()
        {
            HttpContextHelper.Current.Session.Remove("MemberSetting");
        }


    }
}
