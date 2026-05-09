using ZSN.AI.BLL;
using ZSN.Utils.Core.Extensions;
using Microsoft.AspNetCore.Mvc;

using ZSN.Utils.Core.Helpers;
using ZSN.AgentBrook.Web.Manage.Attributes;
using Newtonsoft.Json;
using ZSN.AI.Service.Controllers;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Model.Enum;
using System.Reflection;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [AdminAttributes]
    public class PluginsController: AdminBaseController
    {
        public IActionResult index(int index = 1, int size = 10)
        {
            var lst = PluginsInfoBussiness.GetListByPage(size, index, "", out int pagetotal, out int total);
            ViewBag.Index = index;
            ViewBag.Size = size;
            ViewBag.Total = total;
            ViewBag.PluginsList = lst;
            return View();
        }

        [HttpPost]
        public JsonMsg<string> PluginsStatus(int mid, bool status)
        {
            var Plugins = PluginsInfoBussiness.GetModel(mid);
            Plugins.SystemStatus = status ? ZSN.AI.Entity.PluginsState.Normal : ZSN.AI.Entity.PluginsState.Disabled;

            PluginsInfoBussiness.Update(Plugins);
            return JsonMsg<string>.OK("更新成功");
        }

        public IActionResult Edit(int mid = 0)
        {
            var Plugins = mid == 0 ? new PluginsInfo() : PluginsInfoBussiness.GetModel(mid);
            var ModelList = LargeModelInfoBussiness.GetList(" SystemStatus = 0 ");

            ViewBag.PluginsTypeList = BaseDictionaryInfoBussiness.GetChildList("应用类型");
            ViewBag.SessionModelList = ModelList.FindAll(x => x.TypeCode == AIModelType.Chat);
            ViewBag.VectorModelList = ModelList.FindAll(x => x.TypeCode == AIModelType.Embedding);

            ViewBag.Plugins = Plugins;
            ViewBag.PreviewHost = ConfigHelper.GetString("previewHost");

            ViewBag.KnowledgeBases = KnowledgeBaseInfoBussiness.GetList(" SystemStatus = 0 ");
            return View();
        }
        [HttpPost]
        public JsonMsg<string> PluginsSave(PluginsInfo Plugins, string KnowledgeBases)
        {

            if (Plugins.PluginsID <= 0)
            {
                Plugins.CreateTime = DateTime.Now;
                PluginsInfoBussiness.Add(Plugins);
            }
            else
            {

                PluginsInfoBussiness.Update(Plugins);
            }
            return JsonMsg<string>.OK("保存成功");
        }

        public JsonMsg<string> PluginsDel(string mid)
        {
            PluginsInfoBussiness.DeleteList(mid);

            return JsonMsg<string>.OK("删除成功");
        }

        /// <summary>
        /// 获取方法参数信息
        /// </summary>
        [HttpPost]
        public JsonMsg<List<MethodParameterInfo>> GetMethodParameters(string @namespace, string className, string methodName)
        {
            try
            {
                // 从已加载的程序集中查找类型
                string fullTypeName = $"{@namespace}.{className}";
                Type type = GetTypeFromLoadedAssemblies(fullTypeName);

                if (type == null)
                {
                    return JsonMsg<List<MethodParameterInfo>>.Error(null,ErrorCode.Error);
                }

                // 获取方法信息
                MethodInfo methodInfo = type.GetMethod(methodName);
                if (methodInfo == null)
                {
                    return JsonMsg<List<MethodParameterInfo>>.Error(null, ErrorCode.Error);
                }

                // 获取方法参数
                ParameterInfo[] parameters = methodInfo.GetParameters();
                var paramList = new List<MethodParameterInfo>();

                foreach (var param in parameters)
                {
                    var paramInfo = new MethodParameterInfo
                    {
                        Name = param.Name,
                        Type = ConvertTypeToSimpleName(param.ParameterType),
                        DefaultValue = param.HasDefaultValue ? (param.DefaultValue?.ToString() ?? "") : ""
                    };
                    paramList.Add(paramInfo);
                }

                return JsonMsg<List<MethodParameterInfo>>.OK(paramList, "获取成功");
            }
            catch (Exception ex)
            {
                return JsonMsg<List<MethodParameterInfo>>.Error(null, ErrorCode.Error);
            }
        }

        /// <summary>
        /// 从已加载的程序集中查找类型
        /// </summary>
        private Type GetTypeFromLoadedAssemblies(string fullTypeName)
        {
            // 先尝试直接获取
            Type type = Type.GetType(fullTypeName);
            if (type != null)
            {
                return type;
            }

            // 遍历所有已加载的程序集
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    type = assembly.GetType(fullTypeName);
                    if (type != null)
                    {
                        return type;
                    }
                }
                catch
                {
                    continue;
                }
            }

            return null;
        }

        /// <summary>
        /// 将.NET类型转换为简化的类型名称
        /// </summary>
        private string ConvertTypeToSimpleName(Type type)
        {
            if (type == typeof(string))
                return "string";
            if (type == typeof(int) || type == typeof(Int32))
                return "int";
            if (type == typeof(long) || type == typeof(Int64))
                return "long";
            if (type == typeof(bool) || type == typeof(Boolean))
                return "bool";
            if (type == typeof(double) || type == typeof(Double))
                return "double";
            if (type == typeof(decimal) || type == typeof(Decimal))
                return "decimal";
            if (type == typeof(float) || type == typeof(Single))
                return "float";
            if (type == typeof(DateTime))
                return "datetime";
            if (type == typeof(object))
                return "object";

            // 其他类型返回完整类型名
            return type.Name;
        }
    }

    /// <summary>
    /// 方法参数信息
    /// </summary>
    public class MethodParameterInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string DefaultValue { get; set; }
    }
}
