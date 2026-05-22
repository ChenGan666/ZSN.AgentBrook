using System;
using System.Collections.Generic;
using System.Linq;
using ZSN.AI.BLL;
using ZSN.AI.Entity;

namespace ZSN.AI.Service.WebHelpers
{
    public class DictionaryHelper
    {
        private static List<BaseDictionaryInfo> _dictionaryList;

        public static List<BaseDictionaryInfo> DictionaryList
        {
            get
            {
                if (_dictionaryList == null)
                    InitDictionaryList();
                return _dictionaryList;
            }
        }
        /// <summary>
        /// 获取所有字典
        /// </summary>
        /// <returns></returns>
        public static void InitDictionaryList()
        {
            var list = BaseDictionaryInfoBussiness.GetList();
            foreach (var d in list)
            {
                d.ChildrenList = list.Where(t => t.Pid == d.DicId).ToList();
            }
            _dictionaryList = list;
        }

        /// <summary>
        /// 获取某字典
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static BaseDictionaryInfo GetDicById(int id)
        {
            return DictionaryList.First(t => t.DicId == id);
        }

        /// <summary>
        /// 获取某字典
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static BaseDictionaryInfo GetDicByName(string name)
        {
            return DictionaryList.First(t => string.Equals(t.DicName, name, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// 获取某字典
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public static BaseDictionaryInfo GetDicByTitle(string title)
        {
            return DictionaryList.First(t => string.Equals(t.DicTitle, title, StringComparison.CurrentCultureIgnoreCase));
        }
    }
}