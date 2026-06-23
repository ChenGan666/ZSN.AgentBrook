using System;
using System.Text;
using System.Xml;

namespace Yannyo.Common.Xml
{

    /// <summary>
    /// XmlNode访问器接口
    /// </summary>
    interface IXmlNodeVisitor
    {

        /// <summary>
        /// 设置当前访问器结点
        /// </summary>
        /// <param name="__xmlNode">当前要访问的节点信息</param>
        void SetNode(XmlNode __xmlNode);


        /// <summary>
        /// 返回当前访问器所用结点信息
        /// </summary>
        /// <param name="__nodeName"></param>
        /// <returns></returns>
        XmlNode GetNode(string __nodeName);


        /// <summary>
        /// 索引器属性
        /// </summary>
        /// <param name="__nodeName">节点名称</param>
        /// <returns></returns>
        string this[string __nodeName]{ get; set; } 
    }


    /// <summary>
    /// 属性节点Value访问器类
    /// </summary>
    public class XmlNodeAttributeValueVisitor : IXmlNodeVisitor
    {
        private XmlNode xmlNode;

        public XmlNodeAttributeValueVisitor()
        { }


        /// <summary>
        /// 定义索引器
        /// </summary>
        /// <param name="nodename"></param>
        /// <returns></returns>
        public string this[string __nodeName]
        {
            get
            {
                return xmlNode.Attributes[__nodeName].Value;
            }
            set
            {
                xmlNode.Attributes[__nodeName].Value = value;
            }
        }

        /// <summary>
        /// 设置当前访问器结点
        /// </summary>
        /// <param name="__xmlNode">当前要访问的节点信息</param>
        public void SetNode(XmlNode __xmlNode)
        {
            xmlNode = __xmlNode;
        }

        /// <summary>
        /// 返回当前访问器所用结点信息
        /// </summary>
        /// <param name="__nodeName">节点名称</param>
        /// <returns></returns>
        public XmlNode GetNode(string __nodeName)
        {
            return xmlNode.SelectSingleNode(__nodeName);
        }
    }



    /// <summary>
    /// Select节点InnerText访问器类
    /// </summary>
    public class XmlNodeInnerTextVisitor : IXmlNodeVisitor
    {
        private XmlNode xmlNode;

        public XmlNodeInnerTextVisitor()
        { }


        /// <summary>
        /// 定义索引器
        /// </summary>
        /// <param name="nodename"></param>
        /// <returns></returns>
        public string this[string __nodeName]
        {
            get
            {
                return xmlNode.SelectSingleNode(__nodeName).InnerText;
            }
            set
            {
                xmlNode.SelectSingleNode(__nodeName).InnerText = value;
            }
        }

        /// <summary>
        /// 设置当前访问器结点
        /// </summary>
        /// <param name="__xmlNode">当前要访问的节点信息</param>
        public void SetNode(XmlNode __xmlNode)
        {
            xmlNode = __xmlNode;
        }

        /// <summary>
        /// 返回当前访问器所用结点信息
        /// </summary>
        /// <param name="__nodeName">节点名称</param>
        /// <returns></returns>
        public XmlNode GetNode(string __nodeName)
        {
            return xmlNode.SelectSingleNode(__nodeName);
        }
    }
}
