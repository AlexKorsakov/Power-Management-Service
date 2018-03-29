using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace KursovoyUDP
{
    public partial class Form1 : Form
    {
        public string macaddress { get; set; }
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            macaddress = textBox1.Text;
            this.Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
