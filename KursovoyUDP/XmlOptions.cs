using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Windows.Forms;

namespace KursovoyUDP
{
    public class XmlOptions
    {
        public const string filepath = "HostList.xml";
        XmlDocument xdata = null;

        public string GetFname()
        {
            return filepath;
        }


        /// <summary>
        /// СОЗДАНИЕ Xml
        /// </summary>
        public void GenerateXml()
        {
            //создание файла
            StreamWriter create_file = new StreamWriter(filepath);
            create_file.Close();
            create_file.Dispose();
            XmlDocument xml_document = new XmlDocument();
            XmlDeclaration xml_declaration = xml_document.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement body = xml_document.CreateElement("body");
            xml_document.AppendChild(body);
            xml_document.InsertBefore(xml_declaration, body);
            xml_document.Save(filepath);
        }

        public bool Isset()
        {
            FileInfo file = new FileInfo(filepath);
            return File.Exists(filepath);
        }
    }
}
