using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Globalization;
using System.Net.NetworkInformation;

namespace KursovoyUDP
{
    public partial class Client : Form
    {
        public static String host = Dns.GetHostName();
        TreeViewSerializer serializer;


        public Client()
        {
            InitializeComponent();
        }



        #region UDP-сервер

        public static Socket listeningSocket;
        bool stopReceive = false;
        Thread workReceive = null;
        ResponceParse parser;
        public IPHostEntry currentHost = System.Net.Dns.GetHostByName(host);

        //Прием UDP-сообщения
        public void ReceiveMessage()
        {
            listeningSocket.Bind(new IPEndPoint(IPAddress.Any, 2200));
            try
            {
                while (true)
                {
                    byte[] data = new byte[256]; // буфер для получаемых данных 256
                    IPEndPoint localIP = new IPEndPoint(IPAddress.Any, 0);
                    EndPoint remoteIp = (EndPoint)localIP;
                    listeningSocket.ReceiveFrom(data, ref remoteIp);
                    string message = Encoding.Unicode.GetString(data);
                    IPEndPoint remoteFullIp = remoteIp as IPEndPoint;

                    if (message != "")
                    {
                        parser = new ResponceParse(message, remoteFullIp.Address);
                        if (parser.type == "BROADCAST")
                        {
                            if (!ResponceParse.ContainsClient(remoteFullIp))
                            {
                                ResponceParse.Clients.Add(parser);
                                (this.Controls["dataGridView1"] as DataGridView).Invoke((MethodInvoker)(delegate ()
                               {
                                   FillDatagrid(parser);
                               }));
                            }
                        }
                        else if (parser.type == "INFO")
                        {
                            MessageBox.Show("Время работы для: " + parser.hostName + " составляет " + parser.uptime);
                        }
                    }
                    if (stopReceive) break;
                }
            }
            catch (Exception ex)
            {
                var w32ex = ex as Win32Exception;
                if (w32ex.ErrorCode != 10004)
                {
                    MessageBox.Show("Ошибка при приеме сообщения: " + ex.Message);
                }
            }
        }

        #endregion

        //--------------------------------------//

        #region UDP-клиент
        UdpClient udp = null;
        public static Socket sendingSocket;

        public void SendCommand(string command)
        {
            udp = new UdpClient();
            byte[] message = Encoding.Unicode.GetBytes(command);
            int sended = 0;
            try
            {
                while (sended < message.Length)
                    foreach (DataGridViewRow dgr in dataGridView1.SelectedRows)     //Всем выделенным хостам
                    {
                        sended = udp.Send(message, message.Length, ResponceParse.GetIp_fromName(dgr.Cells[0].Value.ToString()));
                    }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                udp = null;
            }
        }
        #endregion

        void StopUDPReceive()
        {
            stopReceive = true;
            if (listeningSocket != null) sendingSocket.Dispose();
            if (sendingSocket != null) listeningSocket.Dispose();
            if (workReceive != null)
            {
                workReceive.Join();
                workReceive = null;
            }
            sendingSocket = null;
            listeningSocket = null;
        }


        private void Client_Load(object sender, EventArgs e)
        {
            button1_Click(sender, e);
            maskedTextBox1.Visible = false;
            numericUpDown1.Visible = false;

            serializer = new TreeViewSerializer(treeView1);
            if (!serializer.Isset())
            {
                serializer.Serialize();
            }
            else
            {
                serializer.Deserialize();
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            button2.Enabled = true;
            if (listeningSocket == null && sendingSocket == null && workReceive == null)
            {
                listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                sendingSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                stopReceive = false;
                workReceive = new Thread(new ThreadStart(ReceiveMessage));  //Поток для прослушки по UDP
                workReceive.Start();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button1.Enabled = true;
            button2.Enabled = false;
            StopUDPReceive();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
                SendCommand("#INFO#");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            IfCommand("SUSPEND");
        }

        void IfCommand(string com)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                if (checkBox1.Checked && maskedTextBox1.Text != "")
                    com += "#INTIME#" + maskedTextBox1.Text;
                if (checkBox2.Checked && numericUpDown1.Value != 0)
                    com += "#DELAY#" + numericUpDown1.Value.ToString();
                SendCommand("#COM#" + com + "#");
            }
            else
                MessageBox.Show("Не выбран ни один элемент из списка");
        }

        /// <summary>
        /// Удаление выделенных элементов из datagridview и списка хостов
        /// </summary>
        void deleteSelectedInDGrid()
        {
            foreach (DataGridViewRow dgr in dataGridView1.SelectedRows)     //Всем выделенным хостам
            {
                ResponceParse.Remove(dgr.Cells[0].Value.ToString());         //Убрать хост из списка
                dataGridView1.Rows.Remove(dgr);
            }
        }

        private void спящийРежимToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IfCommand("SUSPEND");
        }

        private void выключениеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IfCommand("SHUTDOWN");
        }

        private void перезагрузкаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IfCommand("REBOOT");
        }

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IfCommand("EXIT");
        }

        private void гибернацияToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IfCommand("HIBERNATE");
        }

        void DisabledUndelayedControls()
        {
            button4.Enabled = false;
            button5.Enabled = false;
            button9.Enabled = false;
        }

        void EnabledUndelayedControls()
        {
            button4.Enabled = true;
            button5.Enabled = true;
            button9.Enabled = true;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                maskedTextBox1.Visible = true;
                DisabledUndelayedControls();
            }
            else
            {
                if (!checkBox2.Checked)
                    EnabledUndelayedControls();
                maskedTextBox1.Visible = false;
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                numericUpDown1.Visible = true;
                DisabledUndelayedControls();
            }
            else
            {
                if (!checkBox1.Checked)
                    EnabledUndelayedControls();
                numericUpDown1.Visible = false;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            ResponceParse.Clients.RemoveRange(0, ResponceParse.Clients.Count);
        }

        private void подключениеПоIPToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void Client_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopUDPReceive();
        }


        private void button9_Click(object sender, EventArgs e)
        {
            IfCommand("EXIT");
        }

        private void обзорToolStripMenuItem_Click(object sender, EventArgs e)
        {
            button1_Click(sender, e);
        }

        private void стопToolStripMenuItem_Click(object sender, EventArgs e)
        {
            button2_Click(sender, e);
        }

        private void очиститьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            button6_Click(sender, e);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            IfCommand("SHUTDOWN");
        }

        private void button8_Click(object sender, EventArgs e)
        {
            IfCommand("REBOOT");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            IfCommand("HIBERNATE");
        }

        private void wOLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form1 f = new Form1();
            this.AddOwnedForm(f);
            f.ShowDialog();
            //Process.Start("broadc", f.macaddress + " 255.255.255.255 67");
            try
            {
                SendWOL(f.macaddress);
            }
            catch (Exception ex)
            {

            }
        }


        public void SendWOL(string TargetMac)
        {
            PhysicalAddress target = PhysicalAddress.Parse(TargetMac.ToUpper());
            IPAddress senderAddress = currentHost.AddressList[3];

            byte[] payload = new byte[102]; // 6 bytes of ff, plus 16 repetitions of the 6-byte target
            byte[] targetMacBytes = target.GetAddressBytes();

            // Set first 6 bytes to ff
            for (int i = 0; i < 6; i++)
                payload[i] = 0xff;

            // Repeat the target mac 16 times
            for (int i = 6; i < 102; i += 6)
                targetMacBytes.CopyTo(payload, i);

            // Create a socket to send the packet, and send it
            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                sock.Bind(new IPEndPoint(senderAddress, 0));
                sock.SendTo(payload, new IPEndPoint(IPAddress.Broadcast, 7));
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
        }

        private void добавитьУзелToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow dgr in dataGridView1.SelectedRows)
                treeView1.Nodes[treeView1.SelectedNode.Index].Nodes.Add(dgr.Cells[0].Value.ToString(), dgr.Cells[0].Value.ToString());
            serializer.Serialize();
        }

        private void удалитьУзелToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeView1.Nodes.Remove(treeView1.SelectedNode);
            serializer.Serialize();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null)
                treeView1.Nodes.Add("new Node");
            else
            {
                treeView1.Nodes[treeView1.SelectedNode.Index].Nodes.Add("new Node");
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            treeView1.Nodes.Remove(treeView1.SelectedNode);
        }

        private void сохранитьToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            serializer.Serialize();
        }

        private void treeView1_DoubleClick(object sender, EventArgs e)
        {
            treeView1.SelectedNode.BeginEdit();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked)
            {
                //пересечение списков вссех машин
                isInIntersect();
            }
        }

        bool isInIntersect()
        {
            if (treeView1.SelectedNode != null && treeView1.SelectedNode.Nodes.Count > 0)
                foreach (TreeNode node in treeView1.SelectedNode.Nodes)
                {
                    var found = ResponceParse.Clients.FindAll(p => p.hostName == node.Text);
                    FillDatagrid((ResponceParse)found[0]);
                }
            return false;
        }

        void FillDatagrid(ResponceParse parser)
        {
            dataGridView1.Rows.Add();
            int i = this.dataGridView1.Rows.Count - 1;
            dataGridView1.Rows[i].Cells[0].Value = parser.hostName;
            dataGridView1.Rows[i].Cells[1].Value = parser.ip.ToString();
            dataGridView1.Rows[i].Cells[2].Value = parser.uptime;
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            checkBox3_CheckedChanged(sender, e);
        }
    }
}
