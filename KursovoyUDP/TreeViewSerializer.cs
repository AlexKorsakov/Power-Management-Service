using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;


namespace KursovoyUDP
{

    public class TreeViewSerializer : XmlOptions
    {
        public TreeView TreeView { get; set; }


        public TreeViewSerializer() { }

        public TreeViewSerializer(TreeView treeView)
        {
            this.TreeView = treeView;
        }

        public void Serialize(string fileName = XmlOptions.filepath)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName");
            if (this.TreeView == null)
                throw new NullReferenceException("TreeView can not be null");
            XmlDocument document = new XmlDocument();
            var declaration = document.CreateXmlDeclaration("1.0", "utf-8", null);
            document.AppendChild(declaration);
            var root = document.CreateElement("treeview");
            document.AppendChild(root);
            for (int i = 0; i < this.TreeView.Nodes.Count; i++)
                root.AppendChild(SerializeNode(this.TreeView.Nodes[i], root, document));
            document.Save(fileName);
        }

        public void Deserialize(string fileName = XmlOptions.filepath)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName");
            this.TreeView.Nodes.Clear();
            XmlDocument document = new XmlDocument();
            document.Load(fileName);
            var root = document.DocumentElement;
            for (int i = 0; i < root.ChildNodes.Count; i++)
                TreeView.Nodes.Add(DeseializeNode(root.ChildNodes[i]));
        }

        private XmlElement SerializeNode(TreeNode node, XmlElement root, XmlDocument document)
        {
            XmlElement elem = document.CreateElement("node");
            XmlAttribute attr = document.CreateAttribute("tag");
            XmlAttribute val = document.CreateAttribute("val");
            val.InnerText = node.Text;
            attr.InnerText = node.Tag == null ? "" : node.Tag.ToString();
            elem.Attributes.Append(attr);
            elem.Attributes.Append(val);
            for (int i = 0; i < node.Nodes.Count; i++)
                elem.AppendChild(SerializeNode(node.Nodes[i], elem, document));
            return elem;
        }

        private TreeNode DeseializeNode(XmlNode element)
        {
            TreeNode treeNode = new TreeNode();
            treeNode.Text = element.Attributes["val"].Value;
            treeNode.Tag = element.Attributes["tag"].Value;
            for (int i = 0; i < element.ChildNodes.Count; i++)
                treeNode.Nodes.Add(DeseializeNode(element.ChildNodes[i]));
            return treeNode;
        }
    }
}
