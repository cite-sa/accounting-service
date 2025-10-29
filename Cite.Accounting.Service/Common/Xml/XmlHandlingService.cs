using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Cite.Accounting.Service.Common.Xml
{
	public class XmlHandlingService
	{
		public String ToXml(Object item)
		{
			if (item == null) return null;
			XmlSerializer serializer = new XmlSerializer(item.GetType());
			return this.ToXml(serializer, item);
		}

		public String ToXml(XmlSerializer serializer, Object item)
		{
			if (item == null) return null;

			StringBuilder sb = new StringBuilder();
			using (TextWriter writer = new StringWriter(sb))
			{
				serializer.Serialize(writer, item);
			}
			return sb.ToString();
		}

		public String ToXmlSafe(Object item)
		{
			try
			{
				return this.ToXml(item);
			}
			catch (System.Exception)
			{
				return null;
			}
		}

		public String ToXmlSafe(XmlSerializer serializer, Object item)
		{
			try
			{
				return this.ToXml(serializer, item);
			}
			catch (System.Exception)
			{
				return null;
			}
		}

		public T FromXml<T>(String xml)
		{
			XmlSerializer serializer = new XmlSerializer(typeof(T));
			return this.FromXml<T>(serializer, xml);
		}

		public T FromXml<T>(XmlSerializer serializer, String xml)
		{
			Object obj;
			using (TextReader reader = new StringReader(xml))
			{
				obj = serializer.Deserialize(reader);
			}
			if (obj == null) return default(T);
			return (T)obj;
		}

		public T FromXmlSafe<T>(String xml)
		{
			try
			{
				return string.IsNullOrWhiteSpace(xml) ? default(T) : this.FromXml<T>(xml);
			}
			catch (System.Exception)
			{
				return default(T);
			}
		}

		public T FromXmlSafe<T>(XmlSerializer serializer, String xml)
		{
			try
			{
				return string.IsNullOrWhiteSpace(xml) ? default(T) : this.FromXml<T>(serializer, xml);
			}
			catch (System.Exception)
			{
				return default(T);
			}
		}
	}
}
