using System;
using System.Collections.Generic;
using System.Linq;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.Utils.Core.Extensions;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AI.Service.WebHelpers
{
    public class DictionarySessionHelper
    {
        public static List<BaseDictionaryInfo> DictionaryList
        {
            get
            {
                var lst = HttpContextHelper.Session.Get<List<BaseDictionaryInfo>>("DictionarySessionHelper");
                if (lst == null)
                    lst = InitDictionaryList();
                return lst;
            }
        }
        /// <summary>
        /// 获取所有字典
        /// </summary>
        /// <returns></returns>
        public static List<BaseDictionaryInfo> InitDictionaryList()
        {
            var list = BaseDictionaryInfoBussiness.GetList();
            foreach (var d in list)
            {
                d.ChildrenList = list.Where(t => t.Pid == d.DicId).ToList();
            }
            HttpContextHelper.Session.Set("DictionarySessionHelper", list);
            return list;
        }

        /// <summary>
        /// 获取某字典
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static BaseDictionaryInfo GetDicById(int id)
        {
            return DictionaryList.FirstOrDefault(t => t.DicId == id);
        }

        /// <summary>
        /// 获取某字典
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static BaseDictionaryInfo GetDicByName(string name)
        {
            return DictionaryList.FirstOrDefault(t => string.Equals(t.DicName.Trim(), name, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// 获取某字典
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public static BaseDictionaryInfo GetDicByTitle(string title)
        {
            return DictionaryList.FirstOrDefault(t => string.Equals(t.DicTitle.Trim(), title, StringComparison.CurrentCultureIgnoreCase));
        }

        public static BaseDictionaryInfo GetDicByValue(string value, int pid = 0)
        {
            var lst = GetDicById(pid)?.ChildrenList ?? new List<BaseDictionaryInfo>(0);
            foreach (var dic in lst)
            {
                var r = GetChildrenDicByValue(dic, value);
                if (r != null)
                    return r;
            }
            return null;
        }
        private static BaseDictionaryInfo GetChildrenDicByValue(BaseDictionaryInfo info, string value)
        {
            if (info == null)
                return null;
            if (info.DicValue.ToLower() == value.ToLower())
                return info;
            foreach (var dic in info.ChildrenList)
            {
                var rst = GetChildrenDicByValue(dic, value);
                if (rst != null)
                    return rst;
            }
            return null;
        }

        public static BaseDictionaryInfo GetDicByTitle(string title, int pid)
        {
            var lst = GetDicById(pid)?.ChildrenList ?? new List<BaseDictionaryInfo>(0);
            foreach (var dic in lst)
            {
                var r = GetChildrenDicByTitle(dic, title);
                if (r != null)
                    return r;
            }
            return null;
        }
        private static BaseDictionaryInfo GetChildrenDicByTitle(BaseDictionaryInfo info, string title)
        {
            if (info == null)
                return null;
            if (info.DicTitle.ToLower() == title.ToLower())
                return info;
            foreach (var dic in info.ChildrenList)
            {
                var rst = GetChildrenDicByTitle(dic, title);
                if (rst != null)
                    return rst;
            }
            return null;
        }

        public static List<BaseDictionaryInfo> GetAllChildrenDicById(int id)
        {
            var dic = DictionaryList.FirstOrDefault(t => t.DicId == id);
            var lst = new List<BaseDictionaryInfo>();
            if (dic == null)
                return lst;
            foreach (var info in dic.RealChildrenList)
            {
                lst.AddRange(GetAllChildrenDic(info));
            }
            return lst;
        }

        public static List<BaseDictionaryInfo> GetAllChildrenDic(BaseDictionaryInfo dic)
        {
            var lst = new List<BaseDictionaryInfo>();
            if (dic.RealChildrenList.Count == 0)
            {
                lst.Add(dic);
                return lst;
            }
            foreach (var info in dic.RealChildrenList)
            {
                lst.AddRange(GetAllChildrenDic(info));
            }
            return lst;
        }
   
    }
}