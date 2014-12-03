﻿using System.Xml;

namespace ProtoScript.Bundle
{
	public class UsxDocument
	{
		private readonly XmlDocument m_document;

		public UsxDocument(XmlDocument document)
		{
			m_document = document;
		}

		public UsxDocument(string path)
		{
			m_document = new XmlDocument { PreserveWhitespace = true };
			m_document.Load(path);
		}

		//public XmlNode GetBook()
		//{
		//	return m_document.SelectSingleNode("//book");
		//}

		public string BookId
		{
			get
			{
				var book = m_document.SelectSingleNode("//book");
				return book.Attributes.GetNamedItem("code").Value;
			}
		}

		public XmlNodeList GetChaptersAndParas()
		{
			return m_document.SelectNodes("//para | //chapter");
		}
	}
}
