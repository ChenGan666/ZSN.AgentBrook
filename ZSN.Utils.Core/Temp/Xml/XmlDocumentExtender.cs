using System;
using System.IO;
using System.Data;
using System.Text;
using System.Xml;
using System.Text.RegularExpressions;

namespace Yannyo.Common.Xml
{
    /// <summary>
    /// XmlDocument扩展类
    /// 
    /// 目的:用于优化和减少代码书写量
    /// 备注:Element  译为:元素  
    ///      Document 译为:文档 
    ///      Node     译为:节点
    /// </summary>
    public class XmlDocumentExtender : XmlDocument
    {

        public XmlDocumentExtender()
            : base()
        {
        }


        #region 扩展的构造函数

#if NET1        
        public XmlDocumentExtender(XmlNameTable nt)
            : base(new XmlImplementation())
        {
        }
#else
        public XmlDocumentExtender(XmlNameTable nt)
            : base(new XmlImplementation(nt))
        {
        }
#endif

        #endregion


        /// <summary>
        /// 加载的文件名(含路径)
        /// </summary>
        /// <param name="filename"></param>
        public override void Load(string filename)
        {
            if (Yannyo.Common.Utils.FileExists(filename))
            {
                base.Load(filename);
            }
            else 
            {
                throw new Exception("文件: " + filename + " 不存在!"); 
            }
        }

        
        /// <summary>
        /// 在指定的Xml元素下,添加子Xml元素
        /// </summary>
        /// <param name="xmlElement">被追加子元素的Xml元素</param>
        /// <param name="childElementName">要添加的Xml元素名称</param>
        /// <param name="childElementValue">要添加的Xml元素值</param>
        /// <returns></returns>
        public bool AppendChildElementByNameValue(ref XmlElement xmlElement, string childElementName, object childElementValue)
        {
            return AppendChildElementByNameValue(ref xmlElement, childElementName, childElementValue, false);
        }

        
        /// <summary>
        /// 在指定的Xml元素下,添加子Xml元素
        /// </summary>
        /// <param name="xmlElement">被追加子元素的Xml元素</param>
        /// <param name="childElementName">要添加的Xml元素名称</param>
        /// <param name="childElementValue">要添加的Xml元素值</param>
        /// <param name="IsCDataSection">是否是CDataSection类型的子元素</param>
        /// <returns></returns>
        public bool AppendChildElementByNameValue(ref XmlElement xmlElement, string childElementName, object childElementValue, bool IsCDataSection)
        {
            if ((xmlElement != null) && (xmlElement.OwnerDocument != null))
            {
                //是否是CData类型Xml元素
                if (IsCDataSection)
                {
                    XmlCDataSection tempdata = xmlElement.OwnerDocument.CreateCDataSection(childElementName);
                    tempdata.InnerText = FiltrateControlCharacter(childElementValue.ToString());
                    XmlElement childXmlElement = xmlElement.OwnerDocument.CreateElement(childElementName);
                    childXmlElement.AppendChild(tempdata);
                    xmlElement.AppendChild(childXmlElement);
                }
                else
                {
                    XmlElement childXmlElement = xmlElement.OwnerDocument.CreateElement(childElementName);
                    childXmlElement.InnerText = FiltrateControlCharacter(childElementValue.ToString());
                    xmlElement.AppendChild(childXmlElement);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 在指定的Xml结点下,添加子Xml元素
        /// </summary>
        /// <param name="xmlNode">被追加子元素的Xml节点</param>
        /// <param name="childElementName">要添加的Xml元素名称</param>
        /// <param name="childElementValue">要添加的Xml元素值</param>
        /// <returns></returns>
        public bool AppendChildElementByNameValue(ref XmlNode xmlNode, string childElementName, object childElementValue)
        {
            return AppendChildElementByNameValue(ref xmlNode, childElementName, childElementValue, false);
        }


        /// <summary>
        /// 在指定的Xml结点下,添加子Xml元素
        /// </summary>
        /// <param name="xmlNode">被追加子元素的Xml节点</param>
        /// <param name="childElementName">要添加的Xml元素名称</param>
        /// <param name="childElementValue">要添加的Xml元素值</param>
        /// <param name="IsCDataSection">是否是CDataSection类型的子元素</param>
        /// <returns></returns>
        public bool AppendChildElementByNameValue(ref XmlNode xmlNode, string childElementName, object childElementValue, bool IsCDataSection)
        {
            if ((xmlNode != null) && (xmlNode.OwnerDocument != null))
            {
                //是否是CData类型Xml结点
                if (IsCDataSection)
                {
                    XmlCDataSection tempdata = xmlNode.OwnerDocument.CreateCDataSection(childElementName);
                    tempdata.InnerText = FiltrateControlCharacter(childElementValue.ToString());
                    XmlElement childXmlElement = xmlNode.OwnerDocument.CreateElement(childElementName);
                    childXmlElement.AppendChild(tempdata);
                    xmlNode.AppendChild(childXmlElement);
                }
                else
                {
                    XmlElement childXmlElement = xmlNode.OwnerDocument.CreateElement(childElementName);
                    childXmlElement.InnerText = FiltrateControlCharacter(childElementValue.ToString());
                    xmlNode.AppendChild(childXmlElement);
                }
                return true;
            }
            else
            {
                return false;
            }
        }
         
        /// <summary>
        /// 通过数据行向当前XML元素下追加子元素
        /// </summary>
        /// <param name="xmlElement">被追加子元素的Xml元素</param>
        /// <param name="dcc">当前数据表中的列集合</param>
        /// <param name="dr">当前行数据</param>
        /// <returns></returns>
        public bool AppendChildElementByDataRow(ref XmlElement xmlElement, DataColumnCollection dcc, DataRow dr)
        {
            return AppendChildElementByDataRow(ref xmlElement, dcc, dr, null);
        }

        /// <summary>
        /// 通过数据行向当前XML元素下追加子元素
        /// </summary>
        /// <param name="xmlElement">被追加子元素的Xml元素</param>
        /// <param name="dcc">当前数据表中的列集合</param>
        /// <param name="dr">当前行数据</param>
        /// <param name="removecols">不会被追加的列名</param>
        /// <returns></returns>
        public bool AppendChildElementByDataRow(ref XmlElement xmlElement, DataColumnCollection dcc, DataRow dr, string removecols)
        {
            if((xmlElement != null)&&(xmlElement.OwnerDocument != null))
            {
                foreach (DataColumn dc in dcc)
                {
                    if ((removecols == null) ||
                        (removecols == "") ||
                        (("," + removecols + ",").ToLower().IndexOf("," + dc.Caption.ToLower() + ",") < 0))
                    {
                        XmlElement tempElement = xmlElement.OwnerDocument.CreateElement(dc.Caption);
                        tempElement.InnerText = FiltrateControlCharacter(dr[dc.Caption].ToString().Trim());
                        xmlElement.AppendChild(tempElement);
                    }
                }
                return true;
            }
            else 
            {
                return false;
            }
        }

        /// <summary>
        /// 实始化节点, 当节点存在则清除当前路径下的所有子结点, 如不存在则直接创建该结点
        /// </summary>
        /// <param name="xmlpath"></param>
        /// <returns></returns>
        public XmlNode InitializeNode(string xmlpath)
        {
            XmlNode xmlNode = this.SelectSingleNode(xmlpath);
            if (xmlNode != null)
            {
                xmlNode.RemoveAll();
            }
            else 
            {
                xmlNode = CreateNode(xmlpath);
            }
            return xmlNode;
        }


        
        
        /// <summary>
        /// 删除指定路径下面的所有子结点和自身
        /// </summary>
        /// <param name="xmlpath">指定路径</param>
        public void RemoveNodeAndChildNode(string xmlpath)
        {
            XmlNodeList xmlNodeList = this.SelectNodes(xmlpath);
            if (xmlNodeList.Count > 0)
            {
                foreach (XmlNode xn in xmlNodeList)
                {
                    xn.RemoveAll();
                    xn.ParentNode.RemoveChild(xn);
                }
            }
        }

        /// <summary>
        /// 创建指定路径下的节点
        /// </summary>
        /// <param name="xmlpath">节点路径</param>
        /// <returns></returns>
        public XmlNode CreateNode(string xmlpath)
        {

            string[] xpathArray = xmlpath.Split('/');
            string root = "";
            XmlNode parentNode = this;
            //建立相关节点
            for (int i = 1; i < xpathArray.Length; i++)
            {
                XmlNode node = this.SelectSingleNode(root + "/" + xpathArray[i]);
                // 如果当前路径不存在则建立,否则设置当前路径到它的子路径上
                if (node == null)
                {
                    XmlElement newElement = this.CreateElement(xpathArray[i]);
                    parentNode.AppendChild(newElement);
                }
                //设置低一级的路径
                root = root + "/" + xpathArray[i];
                parentNode = this.SelectSingleNode(root);
            }
   
            return parentNode;
        }

        /// <summary>
        /// 得到指定路径的节点值
        /// </summary>
        /// <param name="xmlnode">要查找节点</param>
        /// <param name="path">指定路径</param>
        /// <returns></returns>
        public string GetSingleNodeValue(XmlNode xmlnode, string path)
        {
            if (xmlnode == null)
            {
                return null;
            }

            if (xmlnode.SelectSingleNode(path) != null)
            {
                if (xmlnode.SelectSingleNode(path).LastChild != null)
                {
                    return xmlnode.SelectSingleNode(path).LastChild.Value;
                }
                else
                {
                    return "";
                }
            }
            return null;
        }

        /// <summary>
        /// 过滤控制字符,包括0x00 - 0x08,0x0b - 0x0c,0x0e - 0x1f
        /// </summary>
        /// <param name="content">要过滤的内容</param>
        /// <returns>过滤后的内容</returns>
        private string FiltrateControlCharacter(string content)
        {
            return Regex.Replace(content, "[\x00-\x08|\x0b-\x0c|\x0e-\x1f]", "");
        }
    }
      
}
