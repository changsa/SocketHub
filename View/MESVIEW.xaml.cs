using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using UtilLibrary;

namespace SOCKETHUB.View
{
    /// <summary>
    /// MESVIEW.xaml 的交互逻辑
    /// </summary>
    public partial class MESVIEW : UserControl
    {
        private const char _STX = (char)0x02;
        private const char _SOH = (char)0x01;
        private const char _ETX = (char)0x03;
        string _ip = null;
        int _port = 0;
        SocketManager _sm = null;

        int _subPanel = 1;
        
        bool isCheck_PING = true;
        bool isCheck_501 = true;
        bool isCheck_550 = true;
        bool isCheck_551 = true;
        bool isCheck_580 = true;
        bool isCheck_5011 = true;
        public MESVIEW()
        {
            InitializeComponent();
        }
        private void UserControl_Loaded_1(object sender, RoutedEventArgs e)
        {
            GetSystemIP();
            ViewSocketOff();
        }

        private void BT_START_Click(object sender, RoutedEventArgs e)
        {
            if (TB_SUBPANEL.Text.Trim().Length == 0)
            {
                MessageBox.Show("EMPTY");
                return;
            }

            if (TB_MESIP.Text.Trim().Length == 0)
            {
                MessageBox.Show("EMPTY");
                return;
            }
            if (TB_PORT.Text.Trim().Length == 0)
            {
                MessageBox.Show("EMPTY");
                return;
            }
            _ip = TB_MESIP.Text;
            _port = int.Parse(TB_PORT.Text.Trim());
            _subPanel = int.Parse(TB_SUBPANEL.Text.ToString());

            if (_sm == null)
            {
                _sm = new SocketManager(1024*30);
                _sm.OnReceiveMsg += SerOnReceiveMsg;
                _sm.OnConnected += SerOnConnected;
                _sm.OnDisConnected += SerOnDisConnected;
            }
            _sm.Start(_ip, _port);
            AppendSendText("MES已开启");
            ViewSocketOn();
        }

        private void BT_STOP_Click(object sender, RoutedEventArgs e)
        {
            _sm.Stop();
            AppendSendText("MES已关闭");
            ViewSocketOff();
        }

        private void SerOnReceiveMsg(byte[] buffer, string clientIP)
        {
            try
            {
                int index = 0;
                int headerStartNum = -1;
                int contStartNum = -1;
                int contEndNum = -1;
                int headerLengh;
                int messLengh;
                if (buffer.Count() > 0)
                {
                    foreach (byte C in buffer)
                    {
                        if (C == _STX)
                        {
                            headerStartNum = index;
                        }
                        if (C == _SOH)
                        {
                            contStartNum = index;
                        }
                        if (C == _ETX)
                        {
                            contEndNum = index;
                        }
                        index++;
                    }
                }
                if (headerStartNum != -1 && contStartNum != -1 && contEndNum != -1)
                {
                    headerLengh = contStartNum - headerStartNum - 1;
                    byte[] byteheader = new byte[headerLengh];
                    for (int i = 0; i < headerLengh; i++)
                    {
                        byteheader[i] = buffer[headerStartNum + 1 + i]; ;
                    }

                    int.TryParse(Encoding.ASCII.GetString(byteheader), out messLengh);

                    //List转换数组
                    byte[] byteMessContFromMES = new byte[messLengh];
                    Array.Copy(buffer, contStartNum + 1, byteMessContFromMES, 0, messLengh);

                    //数组转换String
                    string _string_MessCont = Encoding.ASCII.GetString(byteMessContFromMES);

                    if (_string_MessCont == "ERROR")
                    {
                        return ;
                    }
                    else if (_string_MessCont == "PING_REQ")
                    {
                        AppendReceiveText("PING_REQ");
                        if (isCheck_PING)
                        {
                            _sm.SendMsg(PackingSocketMsg("PING_RSP"), clientIP);
                            AppendSendText("PING_RSP_Send");
                        }
                        return ;
                    }
                    else
                    {
                        processReceiveMsg(_string_MessCont, clientIP);
                    }
                    return ;
                }
                else
                {
                    return ;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return ;
            }
        }

        private void processReceiveMsg(string XMLString, string ClientIP)
        {
            //Document Class
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(XMLString);
            XmlNode messageNode = doc.SelectSingleNode("message");
            XmlNode headerNode = messageNode.SelectSingleNode("header");
            XmlElement headerE = (XmlElement)headerNode;

            //AppendReceiveText("Msg" + headerE.GetAttribute("messageClass").ToString() + ":Transaction ID :" + headerE.GetAttribute("transactionID").Split('_')[1].ToString() + "_Receive");
            switch (headerE.GetAttribute("messageClass").ToString())
            {
                case "501":
                    if (isCheck_501)
                    {
                        string XML = "<message>" + headerE.OuterXml.ToString() + "<body> <result errorCode=\"1\" errorText=\"报错\" actionCode=\"0\"/></body></message>";
                        //string XML = "<message>" + headerE.OuterXml.ToString() + " <body> <result errorCode=\"120014\" errorText=\"???????????????!\" actionCode=\"0\"/> </body></message>";
                        _sm.SendMsg(PackingSocketMsg(XML), ClientIP);
                        AppendSendText("Msg501:Transaction ID :" + headerE.GetAttribute("transactionID").Split('_')[1].ToString() + "_Send");
                                            }
                    break;
                case "550":
                    if (isCheck_550)
                    {
                        string XML = "<message>" + headerE.OuterXml.ToString() + "<body> <result errorCode=\"0\" errorText=\"11111\" actionCode=\"0\"/></body></message>";
                        _sm.SendMsg(PackingSocketMsg(XML), ClientIP);
                        AppendSendText("Msg550:Transaction ID :" + headerE.GetAttribute("transactionID").Split('_')[1].ToString() + "_Send");
                    }
                    break;
                case "551":
                    if (isCheck_551)
                    {
                        string XML = "<message>" + headerE.OuterXml.ToString()  + "<body> <result errorCode=\"0\" errorText=\"11111\" actionCode=\"0\"/></body></message>";
                        _sm.SendMsg(PackingSocketMsg(XML), ClientIP);
                        AppendSendText("Msg551:Transaction ID :" + headerE.GetAttribute("transactionID").Split('_')[1].ToString() + "_Send");
                    }
                    break;
                case "580":
                    if (isCheck_580)
                    {
                        string XML = "<message>" + headerE.OuterXml.ToString() + "<body> <result errorCode=\"0\" errorText=\"11111\" actionCode=\"0\"/></body></message>";
                        _sm.SendMsg(PackingSocketMsg(XML), ClientIP);
                        //AppendSendText("Msg580:Transaction ID :" + headerE.GetAttribute("transactionID").Split('_')[1].ToString() + "_Send");
                    }
                    break;
                case "5011":
                    if (isCheck_5011)
                    {

                        XmlNode bodyNode = messageNode.SelectSingleNode("body");
                        XmlElement bodyE = (XmlElement)bodyNode;
                        XmlNode pcbNode = bodyE.SelectSingleNode("pcb");
                        XmlElement pcbE = (XmlElement)pcbNode;
                        string XML = "<message>" + headerE.OuterXml.ToString() + "<body> <result errorCode=\"0\" errorText=\"11111\" actionCode=\"0\"/><panel state=\"0\" pcbID=\"" + pcbE.GetAttribute("barcode") + "\" productName=\"BMW001\" productSide=\"1\" timestamp=\"2008-07-17T15:26:59+05:30\">";
                        for (int i = 0; i < _subPanel; i++)
                        {
                            XML += "<subPanel pos=\""+( i+1).ToString() +"\" state=\"0\"/>";
                        }
                        XML += "</panel></body></message>";
                        _sm.SendMsg(PackingSocketMsg(XML), ClientIP);

                        AppendSendText("Msg5011:Transaction ID :" + headerE.GetAttribute("transactionID").Split('_')[1].ToString() + "_Send");
                    }
                    break;
                default:
                   string _XML = "<message>" + headerE.OuterXml.ToString() + "<body> <result errorCode=\"0\" errorText=\"11111\" actionCode=\"0\"/></body></message>";
                    _sm.SendMsg(PackingSocketMsg(_XML), ClientIP);
                    break;
            }
        }

        private void SerOnConnected(string clientIP)
        {
            AppendReceiveText("OnConnect IP" + clientIP);
        }

        private void SerOnDisConnected(string clientIP)
        {
            AppendReceiveText("OnDisconnect IP" + clientIP);
        }

        private void GetSystemIP()
        {
            int IPlist_Index = 0;

            //获取说有网卡信息
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in nics)
            {
                //判断是否为以太网卡
                //Wireless80211         无线网卡    Ppp     宽带连接
                //Ethernet              以太网卡   
                //这里篇幅有限贴几个常用的，其他的返回值大家就自己百度吧！

                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    //获取以太网卡网络接口信息
                    IPInterfaceProperties ip = adapter.GetIPProperties();
                    //获取单播地址集
                    UnicastIPAddressInformationCollection ipCollection = ip.UnicastAddresses;
                    foreach (UnicastIPAddressInformation ipadd in ipCollection)
                    {
                        //InterNetwork    IPV4地址      InterNetworkV6        IPV6地址
                        //Max            MAX 位址
                        if (ipadd.Address.AddressFamily == AddressFamily.InterNetwork)
                        //判断是否为ipv4
                        {
                            TB_MESIP.Items.Add(ipadd.Address.ToString());//获取ip
                            TB_MESIP.SelectedIndex = IPlist_Index;
                            IPlist_Index++;
                        }
                    }
                }
                if (IPlist_Index == 0)
                {
                    //CB_IPLIST.SelectedIndex = 0;
                }
            }
        }

        public byte[] PackingSocketMsg(string MsgString)
        {
            List<byte> bytelistForPacking = new List<byte>();
            int length = System.Text.Encoding.GetEncoding("GB2312").GetByteCount(MsgString);
            bytelistForPacking.AddRange(Encoding.ASCII.GetBytes(_STX.ToString()));
            bytelistForPacking.AddRange(Encoding.ASCII.GetBytes(length.ToString()));
            bytelistForPacking.AddRange(Encoding.ASCII.GetBytes(_SOH.ToString()));
            bytelistForPacking.AddRange(Encoding.GetEncoding("GB2312").GetBytes(MsgString));
            bytelistForPacking.AddRange(Encoding.ASCII.GetBytes(_ETX.ToString()));
            byte[] sendMsg = new byte[bytelistForPacking.Count];
            bytelistForPacking.CopyTo(sendMsg);
            return sendMsg;
        }
        
        private void IntOnly_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            bool shiftKey = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;//判断shifu键是否按下
            if (shiftKey == true)                  //当按下shift
            {
                e.Handled = true;//不可输入
            }
            else                           //未按shift
            {
                if (!((e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || e.Key == Key.Delete || e.Key == Key.Back || e.Key == Key.Tab || e.Key == Key.Enter))
                {
                    e.Handled = true;//不可输入
                }
            }
        }

        private void IntOnlyDot_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            bool shiftKey = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;//判断shifu键是否按下
            if (shiftKey == true)                  //当按下shift
            {
                e.Handled = true;//不可输入
            }
            else                           //未按shift
            {
                if (!((e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || e.Key == Key.Delete || e.Key == Key.Back || e.Key == Key.Tab || e.Key == Key.Enter || e.Key == Key.OemPeriod))
                {
                    e.Handled = true;//不可输入
                }
            }
        }

        private void AppendReceiveText(string Msg)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {

                TB_LOG.AppendText(System.DateTime.Now.ToString("T") + "::[接受]::" + Msg + "\r");
                TB_LOG.ScrollToEnd();
                if (TB_LOG.LineCount > 200)
                {
                    TB_LOG.Clear();
                }
            }));
        }
        private void AppendSendText(string Msg)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                TB_LOG.AppendText(System.DateTime.Now.ToString("T") + "::[发送]::" + Msg + "\r");
                TB_LOG.ScrollToEnd();
                if (TB_LOG.LineCount > 200)
                {
                    TB_LOG.Clear();
                }
            }));
        }

        private void ViewSocketOn()
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                BT_START.IsEnabled = false;
                BT_STOP.IsEnabled = true;
            }));
        }
        private void ViewSocketOff()
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                BT_START.IsEnabled = true;
                BT_STOP.IsEnabled = false;
            }));
        }

        private void CB_PING_RSP_Checked(object sender, RoutedEventArgs e)
        {
            isCheck_PING = true;
        }

        private void CB_PING_RSP_Unchecked(object sender, RoutedEventArgs e)
        {
            isCheck_PING = false;
        }

        private void CB_501_Checked(object sender, RoutedEventArgs e)
        {
            isCheck_501 = true;
        }

        private void CB_501_Unchecked(object sender, RoutedEventArgs e)
        {
            isCheck_501 = false;
        }

        private void CB_550_Checked(object sender, RoutedEventArgs e)
        {
            isCheck_550 = true;
        }

        private void CB_550_Unchecked(object sender, RoutedEventArgs e)
        {
            isCheck_550 = false;
        }

        private void CB_551_Checked(object sender, RoutedEventArgs e)
        {
            isCheck_551 = true;
        }

        private void CB_551_Unchecked(object sender, RoutedEventArgs e)
        {
            isCheck_551 = false;
        }

        private void CB_5011_Checked(object sender, RoutedEventArgs e)
        {
            isCheck_5011 = true;
        }

        private void CB_5011_Unchecked(object sender, RoutedEventArgs e)
        {
            isCheck_5011 = false;
        }

        private void BT_CLEAN_Click(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                TB_LOG.Text = null;
            }));
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (TB_SUBPANEL.Text.Trim().Length == 0)
            {
                MessageBox.Show("EMPTY");
                return;
            }
            _subPanel = int.Parse(TB_SUBPANEL.Text.ToString());
        }

        private void CB_580_Checked(object sender, RoutedEventArgs e)
        {
            isCheck_580 = true;
        }
        private void CB_580_Unchecked(object sender, RoutedEventArgs e)
        {
            isCheck_580 = false;
        }
    }
}
