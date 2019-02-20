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
using UtilLibrary;

namespace SOCKETHUB.View
{
    /// <summary>
    /// PCBSCANNER.xaml 的交互逻辑
    /// </summary>
    public partial class PCBSCANNER : UserControl
    {
        private const char _EMPTY = (char)0x00;
        private const char _ENTER = (char)0x0D;
        string _ip = null;
        int _port = 0;
        SocketManager _sm = null;
        string _ModelString = "PCBIDABCDEFGHIJK18";
        int _Serial = 10000001;
        public PCBSCANNER()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded_1(object sender, RoutedEventArgs e)
        {
            GetSystemIP();
            TB_MODELSTRING_DATA.Text = _ModelString;
            TB_SERIALNUMBER_DATA.Text = _Serial.ToString();
            TB_MODELSTRING.Text = _ModelString;
            TB_SERIALNUMBER.Text = _Serial.ToString();
            ViewSocketOff();
        }

        private void BT_START_Click(object sender, RoutedEventArgs e)
        {
            if (TB_LocalIP.Text.Trim().Length == 0)
            {
                MessageBox.Show("EMPTY");
                return;
            }
            if (TB_PORT.Text.Trim().Length == 0)
            {
                MessageBox.Show("EMPTY");
                return;
            }
            _ip = TB_LocalIP.Text;
            _port = int.Parse(TB_PORT.Text.Trim());

            if (_sm == null)
            {
                _sm = new SocketManager(128);
                _sm.OnReceiveMsg += SerOnReceiveMsg;
                _sm.OnConnected += SerOnConnected;
                _sm.OnDisConnected += SerOnDisConnected;
            }
            _sm.Start(_ip, _port);
            AppendSendText("PCB Scanner已开启");
            ViewSocketOn();
        }

        private void BT_STOP_Click(object sender, RoutedEventArgs e)
        {
            _sm.Stop();
            AppendSendText("PCB Scanner已关闭");
            ViewSocketOff();
        }

        private void BT_UPDATE_Click(object sender, RoutedEventArgs e)
        {
            TB_MODELSTRING_DATA.Text = TB_MODELSTRING.Text;
            TB_SERIALNUMBER_DATA.Text = TB_SERIALNUMBER.Text;
            _ModelString = TB_MODELSTRING.Text;
            _Serial = int.Parse(TB_SERIALNUMBER.Text);
        }

        private void SerOnReceiveMsg(byte[] buffer, string clientIP)
        {
            int i = 0;
            foreach (char _char in buffer)
            {
                if (_char != _EMPTY)
                {
                    i++;
                }
                else
                {
                    break;
                }
            }
            if (i != 0)
            {
                string ReceiveStr = Encoding.ASCII.GetString(buffer, 0, i);
                AppendReceiveText(ReceiveStr.Replace("\r","") + "_Receive");

                if (ReceiveStr.IndexOf("LON") != -1)
                {
                    _sm.SendMsg(PackingSocketMsg(_ModelString + _Serial.ToString()), clientIP);
                    AppendSendText(_ModelString + _Serial.ToString() + "_Send");
                    _Serial++;
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        TB_SERIALNUMBER_DATA.Text = _Serial.ToString();
                    }));
                }
            }
        }

        private void SerOnConnected(string clientIP)
        {
            AppendReceiveText("LNB OnConnect IP" + clientIP);
        }
        private void SerOnDisConnected(string clientIP)
        {
            AppendReceiveText("LNB OnDisconnect IP" + clientIP);
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
                            TB_LocalIP.Items.Add(ipadd.Address.ToString());//获取ip
                            TB_LocalIP.SelectedIndex = IPlist_Index;
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
            bytelistForPacking.AddRange(Encoding.ASCII.GetBytes(MsgString));
            bytelistForPacking.AddRange(Encoding.ASCII.GetBytes(_ENTER.ToString()));
            byte[] sendMsg = new byte[bytelistForPacking.Count];
            bytelistForPacking.CopyTo(sendMsg);
            return sendMsg;
        }

        private void BT_CLEAN_Click(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                TB_LOG.Text = null;
            }));
        }
    }
}
