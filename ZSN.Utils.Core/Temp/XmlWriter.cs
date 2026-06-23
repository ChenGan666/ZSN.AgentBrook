using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace Yannyo.Common
{
    /// <summary>
    /// XMLComponent 类
    /// </summary>
	public abstract class XMLComponent
	{
		
		private DataTable sourceDT = null;
        /// <summary>
        /// 源数据表
        /// </summary>
		public DataTable SourceDataTable
		{
			set{sourceDT = value;}
			get{return sourceDT;}
		}

		
		private string fileOutputPath = @"";
        /// <summary>
        /// 文件输出路径
        /// </summary>
		public string FileOutPath
		{
			set
			{   //保证路径字符串变量的合法性
				if (value.LastIndexOf("\\") != (value.Length-1))
					fileOutputPath = value + "\\";
			}
			get{return fileOutputPath;}
		}

		
		private string fileEncode = "utf-8";
        /// <summary>
        /// 文件编码
        /// </summary>
		public string FileEncode
		{
			set{fileEncode = value;}
			get{return fileEncode;}
		}

		
		private int indentation = 6;
        /// <summary>
        /// 文件缩进
        /// </summary>
		public int Indentation
		{
			set{indentation = value;}
			get{return indentation;}
		}


		private string version = "2.0";
        /// <summary>
        /// 版本信息
        /// </summary>
		public string Version
		{
			set{version = value;}
			get{return version;}
		}

		private string startElement = "channel";
        /// <summary>
        /// 开始元素
        /// </summary>
		public string StartElement
		{
			set{startElement = value;}
			get{return startElement;}
		}

		private string xslLink = null;
        /// <summary>
        /// XSL链接
        /// </summary>
		public string XslLink
		{
			set{xslLink = value;}
			get{return xslLink;}
		}

		private string fileName = "MyFile.xml";
        /// <summary>
        /// 文件名
        /// </summary>
		public string FileName
		{
			set{fileName = value;}
			get{return fileName;}
		}


		private string parentField = "Item";
        /// <summary>
        /// 表中指向父记录的字段名称
        /// </summary>
		public string ParentField
		{
			set{parentField = value;}
			get{return parentField;}
		}


        private string key = "ItemID";
        /// <summary>
        /// 表中一个主键的值
        /// </summary>
		public string Key
		{
			set{key = value;}
			get{return key;}
		}

	
		/// <summary>
        /// 写入文件
		/// </summary>
		/// <returns></returns>
		public abstract string WriteFile();

		/// <summary>
        /// 写入StringBuilder对象
		/// </summary>
		/// <returns></returns>
		public abstract StringBuilder WriteStringBuilder();

      	/// <summary>
      	/// 文档对象
      	/// </summary>
		public XmlDocument xmlDoc_Metone = new XmlDocument();


		#region 构XML树
        /// <summary>
        /// 构XML树
        /// </summary>
        /// <param name="tempXmlElement"></param>
        /// <param name="location"></param>
		protected void BulidXmlTree(XmlElement tempXmlElement,string location)
		{                                 

			DataRow tempRow = this.SourceDataTable.Select(this.Key + "=" + location)[0];
			//生成Tree节点
			XmlElement treeElement = xmlDoc_Metone.CreateElement(this.ParentField);
			tempXmlElement.AppendChild(treeElement);
		
			
			foreach(DataColumn c in this.SourceDataTable.Columns)  //依次找出当前记录的所有列属性
			{
				if ((c.Caption.ToString().ToLower() != this.ParentField.ToLower())) 
					this.AppendChildElement(c.Caption.ToString().Trim().ToLower(),tempRow[c.Caption.Trim()].ToString().Trim(),treeElement);
			}
			
            
			foreach (DataRow dr in this.SourceDataTable.Select(this.ParentField + "=" + location))
			{
				if(this.SourceDataTable.Select("item=" + dr[this.Key].ToString()).Length >= 0)
				{
					this.BulidXmlTree(treeElement,dr[this.Key].ToString().Trim());
				}
				else continue;
			} 
			           
		}
		#endregion

 

		#region 追加子节点
		/// <summary>
		/// 追加子节点
		/// </summary>
		/// <param name="strName">节点名字</param>
		/// <param name="strInnerText">节点内容</param>
		/// <param name="parentElement">父节点</param>
		/// <param name="xmlDocument">XmlDocument对象</param>
		protected void AppendChildElement(string strName , string strInnerText , XmlElement parentElement, XmlDocument xmlDocument )
		{
			XmlElement xmlElement = xmlDocument.CreateElement(strName) ;
			xmlElement.InnerText = strInnerText ;
			parentElement.AppendChild(xmlElement);
		}

 

		/// <summary>
		/// 使用默认的Xml文档
		/// </summary>
		/// <param name="strName"></param>
		/// <param name="strInnerText"></param>
		/// <param name="parentElement"></param>
		protected void AppendChildElement(string strName , string strInnerText , XmlElement parentElement )
		{
			AppendChildElement(strName,strInnerText,parentElement,xmlDoc_Metone);
		}
		#endregion


		
		#region 创建存储生成XML的文件夹
        /// <summary>
        /// 创建存储生成XML的文件夹
        /// </summary>
		public void CreatePath()
		{   
			if (this.FileOutPath != null)
			{
				string path = this.FileOutPath;  //;Server.MapPath("");
				if (!Directory.Exists(path))
				{
					Utils.CreateDir(path);
				}
			}
			else
			{
				string path = @"C:\";   //;Server.MapPath("");
				string NowString = DateTime.Now.ToString("yyyy-M").Trim();
				if (!Directory.Exists(path + NowString))
				{
					Utils.CreateDir(path + "\\" + NowString);
				}
			}
		}

		#endregion
	}


	/// <summary>
    /// 无递归直接生成XML
	/// </summary>
	class ConcreteComponent : XMLComponent
	{
		private string strName;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="s"></param>
		public ConcreteComponent(string s)
		{
			strName = s;
		}


		/// <summary>
        /// 写入StringBuilder对象
		/// </summary>
		/// <returns></returns>
		public override StringBuilder WriteStringBuilder()
		{
			//string xmlData = string.Format("<?xml version='1.0' encoding='{0}'?><?xml-stylesheet type=\"text/xsl\" href=\"{1}\"?><{3} version='{2}'></{3}>",this.FileEncode,this.XslLink,this.Version,this.SourceDataTable.TableName);
			string xmlData = string.Format("<?xml version='1.0' encoding='{0}'?><{3} ></{3}>",this.FileEncode,this.XslLink,this.Version,this.SourceDataTable.TableName);
			
			this.xmlDoc_Metone.Load(new StringReader(xmlData));
			//写入channel
			foreach(DataRow r in this.SourceDataTable.Rows)   //依次取出所有行
			{
				//普通方式生成XML
				XmlElement treeContentElement = this.xmlDoc_Metone.CreateElement(this.StartElement);
				xmlDoc_Metone.DocumentElement.AppendChild(treeContentElement);
						
				foreach(DataColumn c in this.SourceDataTable.Columns)  //依次找出当前记录的所有列属性
				{
					this.AppendChildElement(c.Caption.ToString().ToLower(),r[c].ToString().Trim(),treeContentElement);
				}
				
			}
		     
			return new StringBuilder().Append(xmlDoc_Metone.InnerXml);
		}
		
		/// <summary>
		/// 写入文件
		/// </summary>
		/// <returns></returns>
		public override string WriteFile()
		{
			
			if (this.SourceDataTable != null)
			{
				
				DateTime filenamedate = DateTime.Now; 
		
				string filename = this.FileOutPath + this.FileName; 
				XmlTextWriter PicXmlWriter = null;
				Encoding encode = Encoding.GetEncoding(this.FileEncode);
				CreatePath();
				PicXmlWriter = new XmlTextWriter (filename,encode);
			
				try
				{
				
					PicXmlWriter.Formatting = Formatting.Indented;
					PicXmlWriter.Indentation = this.Indentation;
					PicXmlWriter.Namespaces = false;
					PicXmlWriter.WriteStartDocument();
					//PicXmlWriter.WriteDocType("文档类型", null, ".xml", null);
					//PicXmlWriter.WriteComment("按在数据库中记录的ID进行记录读写");
					PicXmlWriter.WriteProcessingInstruction("xml-stylesheet","type='text/xsl' href='" + this.XslLink + "'") ;
				
					PicXmlWriter.WriteStartElement(this.SourceDataTable.TableName);
					PicXmlWriter.WriteAttributeString("", "version", null, this.Version);
			
					//写入channel
					foreach(DataRow r in this.SourceDataTable.Rows)   //依次取出所有行
					{
						PicXmlWriter.WriteStartElement("",this.StartElement,"");
						foreach(DataColumn c in this.SourceDataTable.Columns)  //依次找出当前记录的所有列属性
						{
							PicXmlWriter.WriteStartElement("",c.Caption.ToString().Trim().ToLower(),"");
							PicXmlWriter.WriteString(r[c].ToString().Trim());
							PicXmlWriter.WriteEndElement();
						}
						PicXmlWriter.WriteEndElement();
					}

					PicXmlWriter.WriteEndElement();
					PicXmlWriter.Flush();
					this.SourceDataTable.Dispose();
				}
				catch (Exception e)	{	Console.WriteLine ("异常：{0}", e.ToString()); }
				finally
				{
					Console.WriteLine("对文件 {0} 的处理已完成。");
					if (PicXmlWriter != null)
						PicXmlWriter.Close();
					
				}
				return filename;
			}	
			else
			{
				Console.WriteLine("对文件 {0} 的处理未完成。");
				return "";
			}
		}	
		
	}


	/// <summary>
    /// 无递归直接生成XML
	/// </summary>
	public class TreeNodeComponent : XMLComponent
	{
		private string strName;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="s"></param>
		public TreeNodeComponent(string s)
		{
			strName = s;
		}


        /// <summary>
        /// 写入StringBuilder对象
        /// </summary>
        /// <returns></returns>
		public override StringBuilder WriteStringBuilder()
		{
			//string xmlData = string.Format("<?xml version='1.0' encoding='{0}'?><?xml-stylesheet type=\"text/xsl\" href=\"{1}\"?><{3} version='{2}'></{3}>",this.FileEncode,this.XslLink,this.Version,this.SourceDataTable.TableName);
			string xmlData = string.Format("<?xml version='1.0' encoding='{0}'?><{3} ></{3}>",this.FileEncode,this.XslLink,this.Version,this.SourceDataTable.TableName);
			
			this.xmlDoc_Metone.Load(new StringReader(xmlData));
			//写入channel
			foreach(DataRow r in this.SourceDataTable.Rows)   //依次取出所有行
			{
				//普通方式生成XML
				XmlElement treeContentElement = this.xmlDoc_Metone.CreateElement(this.StartElement);
				xmlDoc_Metone.DocumentElement.AppendChild(treeContentElement);
						
				foreach(DataColumn c in this.SourceDataTable.Columns)  //依次找出当前记录的所有列属性
				{
					this.AppendChildElement(c.Caption.ToString().ToLower(),r[c].ToString().Trim(),treeContentElement);
				}
				
			}
		     
			return new StringBuilder().Append(xmlDoc_Metone.InnerXml);
		}
		
		/// <summary>
        /// 写入文件
		/// </summary>
		/// <returns></returns>
		public override string WriteFile()
		{
			
			if (this.SourceDataTable != null)
			{
				
				DateTime filenamedate = DateTime.Now; 
		
				string filename = this.FileOutPath + this.FileName; 
				XmlTextWriter PicXmlWriter = null;
				Encoding encode = Encoding.GetEncoding(this.FileEncode);
				CreatePath();
				PicXmlWriter = new XmlTextWriter (filename,encode);
			
				try
				{
				
					PicXmlWriter.Formatting = Formatting.Indented;
					PicXmlWriter.Indentation = this.Indentation;
					PicXmlWriter.Namespaces = false;
					PicXmlWriter.WriteStartDocument();
					//PicXmlWriter.WriteDocType("文档类型", null, ".xml", null);
					//PicXmlWriter.WriteComment("按在数据库中记录的ID进行记录读写");
				
					PicXmlWriter.WriteStartElement(this.SourceDataTable.TableName);
				
					string content = null;
						
					//写入channel
					foreach(DataRow r in this.SourceDataTable.Rows)   //依次取出所有行
					{
						
						content = "  Text=\"" + r[0].ToString().Trim() + "\"   ImageUrl=\"../../editor/images/smilies/" + r[1].ToString().Trim() + "\"";
						
						PicXmlWriter.WriteStartElement("",this.StartElement+content,"");
						
						PicXmlWriter.WriteEndElement();
						content = null;
					}

					PicXmlWriter.WriteEndElement();
					PicXmlWriter.Flush();
					this.SourceDataTable.Dispose();
				}
				catch (Exception e)
				{
					Console.WriteLine ("异常：{0}", e.ToString());
				}
				finally
				{
					Console.WriteLine("对文件 {0} 的处理已完成。");
					if (PicXmlWriter != null)	
						PicXmlWriter.Close();
					
				}
				return filename;
			}	
			else
			{
				Console.WriteLine("对文件 {0} 的处理未完成。");
				return "";
			}
		}	
		
	}


	/// <summary>
    /// RSS生成
	/// </summary>
	public class RssXMLComponent : XMLComponent
	{
		private string strName;


        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="s"></param>
		public RssXMLComponent(string s)
		{
			strName = s;	
			FileEncode ="gb2312";
			Version = "2.0";
			StartElement = "channel";
		}

        /// <summary>
        /// 写入StringBuilder对象
        /// </summary>
        /// <returns></returns>
		public override StringBuilder WriteStringBuilder()
		{
			string xmlData = string.Format("<?xml version='1.0' encoding='{0}'?><?xml-stylesheet type=\"text/xsl\" href=\"{1}\"?><rss version='{2}'></rss>",this.FileEncode,this.XslLink,this.Version);
			this.xmlDoc_Metone.Load(new StringReader(xmlData));
			string Key = "-1";
			//写入channel
			foreach(DataRow r in this.SourceDataTable.Rows)   //依次取出所有行
			{
				if ((this.Key != null) && (this.ParentField != null)) //递归进行XML生成
				{
					if ((r[this.ParentField].ToString().Trim() == "")||(r[this.ParentField].ToString().Trim() == "0"))
					{
						XmlElement treeContentElement = this.xmlDoc_Metone.CreateElement(this.StartElement);
						xmlDoc_Metone.DocumentElement.AppendChild(treeContentElement);
						
						foreach(DataColumn c in this.SourceDataTable.Columns)  //依次找出当前记录的所有列属性
						{
							if ((c.Caption.ToString().ToLower() == this.ParentField.ToLower()))
							{
								Key = r[this.Key].ToString().Trim();
							}
							else
							{
								if ((r[this.ParentField].ToString().Trim() == "")||(r[this.ParentField].ToString().Trim() == "0"))
								{
									this.AppendChildElement(c.Caption.ToString().ToLower(),r[c].ToString().Trim(),treeContentElement);
								}
							}
						}
				    
						foreach(DataRow dr in this.SourceDataTable.Select(this.ParentField + "=" + Key))
						{
							if(this.SourceDataTable.Select(this.ParentField + "=" + dr[this.Key].ToString()).Length >= 0)
								this.BulidXmlTree(treeContentElement,dr["ItemID"].ToString().Trim());
							else
								continue;
						}
					}
				}
				else  //普通方式生成XML
				{
					
					XmlElement treeContentElement = this.xmlDoc_Metone.CreateElement(this.StartElement);
					xmlDoc_Metone.DocumentElement.AppendChild(treeContentElement);
						
					foreach(DataColumn c in this.SourceDataTable.Columns)  //依次找出当前记录的所有列属性
					{
						this.AppendChildElement(c.Caption.ToString().ToLower(),r[c].ToString().Trim(),treeContentElement);
					}
				}
			}
		     
			return new StringBuilder().Append(xmlDoc_Metone.InnerXml);
		}

		
        /// <summary>
        /// 写入文件
        /// </summary>
        /// <returns></returns>
		public override string WriteFile()
		{
				
			CreatePath();
			string xmlData = string.Format("<?xml version='1.0' encoding='{0}'?><?xml-stylesheet type=\"text/xsl\" href=\"{1}\"?><rss version='{2}'></rss>",this.FileEncode,this.XslLink,this.Version);
			this.xmlDoc_Metone.Load(new StringReader(xmlData));
			string Key = "-1";
			//写入channel
			foreach(DataRow r in this.SourceDataTable.Rows)   //依次取出所有行
			{
				if ((this.Key != null)&&(this.ParentField != null)) //递归进行XML生成
				{
					if ((r[this.ParentField].ToString().Trim() == "")||(r[this.ParentField].ToString().Trim() == "0"))
					{
						XmlElement treeContentElement = this.xmlDoc_Metone.CreateElement(this.StartElement);
						xmlDoc_Metone.DocumentElement.AppendChild(treeContentElement);
						
						foreach(DataColumn c in this.SourceDataTable.Columns)  //依次找出当前记录的所有列属性
						{
							if ((c.Caption.ToString().ToLower() == this.ParentField.ToLower())) 
								Key = r[this.Key].ToString().Trim();
							else
							{
								if ((r[this.ParentField].ToString().Trim() == "")||(r[this.ParentField].ToString().Trim() == "0"))
								{
									this.AppendChildElement(c.Caption.ToString().ToLower(),r[c].ToString().Trim(),treeContentElement);
								}
							}
						}
				    
						foreach(DataRow dr in this.SourceDataTable.Select(this.ParentField + "=" + Key))
						{
							if(this.SourceDataTable.Select(this.ParentField + "=" + dr[this.Key].ToString()).Length >= 0)
								this.BulidXmlTree(treeContentElement,dr["ItemID"].ToString().Trim());
							else	
								continue;
						}
					}
				}
				else  //普通方式生成XML
				{
					
					XmlElement treeContentElement = this.xmlDoc_Metone.CreateElement(this.StartElement);
					xmlDoc_Metone.DocumentElement.AppendChild(treeContentElement);
						
					foreach(DataColumn c in this.SourceDataTable.Columns)  //依次找出当前记录的所有列属性
					{
						this.AppendChildElement(c.Caption.ToString().ToLower(),r[c].ToString().Trim(),treeContentElement);
					}
				}
				
			}
			string fileName = this.FileOutPath+this.FileName; 
			xmlDoc_Metone.Save(fileName);
            
			return fileName;
				
		}

	}


	//装饰器类
	public class XMLDecorator : XMLComponent
	{
		protected XMLComponent ActualXMLComponent;

		private string strDecoratorName;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="s"></param>
		public XMLDecorator (string str)
		{
			strDecoratorName = str; 
		}


		public void SetXMLComponent(XMLComponent xc)
		{
			ActualXMLComponent = xc;
			//Console.WriteLine("FileEncode - {0}", xc.FileEncode);		
			GetSettingFromComponent( xc);
		}
        
		//将被装入的对象的默认设置为当前装饰者的初始值
		public void GetSettingFromComponent(XMLComponent xc)
		{
			this.FileEncode = xc.FileEncode;
			this.FileOutPath = xc.FileOutPath;
			this.Indentation = xc.Indentation;
			this.SourceDataTable = xc.SourceDataTable;
			this.StartElement = xc.StartElement;
			this.Version = xc.Version;
			this.XslLink = xc.XslLink;
			this.Key = xc.Key;
			this.ParentField = xc.ParentField;
		}

        /// <summary>
        /// 写入文件
        /// </summary>
        /// <returns></returns>
		public override string WriteFile()
		{
			if (ActualXMLComponent != null)
				ActualXMLComponent.WriteFile();		

			return null;
		}

		/// <summary>
        /// 写入StringBuilder对象
		/// </summary>
		/// <returns></returns>
		public override StringBuilder WriteStringBuilder()
		{
			if (ActualXMLComponent != null)
				return ActualXMLComponent.WriteStringBuilder();		

			return null;
		}
	}


	#region
	/*
	class ConcreteDecorator : XMLDecorator 
	{
		private string strDecoratorName;
		public ConcreteDecorator (string str)
		{
			// how decoration occurs is localized inside this decorator
			// For this demo, we simply print a decorator name
			strDecoratorName = str; 
		}
		public void Draw()
		{
			CustomDecoration();
			base.Draw();
		}
		void CustomDecoration()
		{
			Console.WriteLine("In ConcreteDecorator: decoration goes here");
			Console.WriteLine("{0}", strDecoratorName);
		}
	}
	*/
	#endregion

    #region 测试代码类(已注释)

    ///// <summary>
    ///// XmlWrite 测试代码的摘要说明。
    ///// </summary>
    //public class XmlWrite
    //{
    //    /* 表结构定义SQL语句
    //    CREATE TABLE [Rss_ChannelItem] (
    //    [ChannelID] [int] NOT NULL ,
    //                          [ItemID] [int] NOT NULL ,
    //                                             [ItemName] [varchar] (50) NULL ,
    //    [ItemDescription] [nvarchar] (4000) NULL ,
    //    [ItemNum] [int] NOT NULL ,
    //                        [Item] [int] NOT NULL ,
    //                                         [ItemLink] [varchar] (50) NULL 
    //                                                                                                 ) ON [PRIMARY]
    //    GO
    //    */


    //    XMLComponent Setup() 
    //    {
    //        ConcreteComponent c = new ConcreteComponent("This is the RSS component");

    //        DataSet ds = new DataSet();
    //        SqlConnection sc = new SqlConnection("server=192.168.2.198;database=RSS;uid=sa;pwd=;");
    //        new SqlDataAdapter("Select  *  From Rss_ChannelItem",sc).Fill(ds);
    //        ds.Tables[0].TableName = "xml";//不要使用数字来定义该项
    //        c.SourceDataTable = ds.Tables[0];
    //        c.FileName = "test.xml";
    //        c.FileOutPath = @"c:\";
    //        //c.Key=null;

    //        //c.FileEncode="2.0";
    //        XMLDecorator d = new XMLDecorator("This is a decorator for the component");

    //        d.SetXMLComponent(c);
    //        // d.FileEncode="2.0";
    //        return d;
    //    }
		

    //    public XmlWrite()
    //    {
    //        //
    //        // TODO: 在此处添加构造函数逻辑
    //        //
    //    }

		
    //    [STAThread]
    //    public static int Main(string[] args)
    //    {

    //        XmlWrite client = new XmlWrite();
    //        XMLComponent c = client.Setup();    

    //        // The code below will work equally well with the real component, 
    //        // or a decorator for the component

    //        //c.WriteFile();
    //        Console.WriteLine(c.FileEncode);
			
    //        /*
    //        c.FileOutPath=null;
    //        c.CreatePath();
    //        */

			
			
    //        Console.WriteLine(c.WriteFile());
    //        if (c.WriteStringBuilder() != null)
    //            Console.WriteLine(c.WriteStringBuilder().ToString());
		

    //        return 0;
    //    }

    //}

    #endregion
}


