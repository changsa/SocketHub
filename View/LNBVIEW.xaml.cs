using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// LNBVIEW.xaml 的交互逻辑
    /// </summary>
    public partial class LNBVIEW : UserControl
    {
        private const char _EMPTY = (char)0x00;
        private const char _ENTER = (char)0x0D;
        string _ip = null;
        int _port = 0;
        SocketClientManager _scm = null;
        // Msg Ping
        Thread socketConnectedThread_REQSCAN;
        static AutoResetEvent _Event_SEND_REQSCAN = new AutoResetEvent(false);
        bool _SEND_REQSCAN_On = false;
        int _timer_REQSCAN = 0;
        int _times_Total_REQSCAN = 0;
        int _times_Send_REQSCAN = 0;
        int _times_Receive_REQSCAN = 0;
        public LNBVIEW()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded_1(object sender, RoutedEventArgs e)
        {
            ViewSocketOff();
        }

        private void BT_START_Click(object sender, RoutedEventArgs e)
        {
            _ip = TB_AIPIP.Text.Trim();
            _port = int.Parse(TB_PORT.Text.ToString());

            if (_scm == null)
            {
                _scm = new SocketClientManager(1024*8);
                _scm.OnReceiveMsg += ClientOnReceiveMsg;
                _scm.OnConnected += ClientOnConnected;
                _scm.OnFaildConnect += ClientOnFailedConnect;
                _scm.OnDisconnect += ClientOnServerDisconnect;
            }
            else
            {
                _scm.Stop();
            }
            _scm.Start(_ip, _port);
        }

        private void BT_STOP_Click(object sender, RoutedEventArgs e)
        {
            _scm.Stop();
            ViewSocketOff();
        }

        #region Msg PING
        private void TB_SEND_SCANREQ_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _timer_REQSCAN = int.Parse(TB_TIMER_SCANREQ.Text);
                _times_Total_REQSCAN = int.Parse(TB_TIMES_SCANREQ.Text);
                //Msg Ping
                if (socketConnectedThread_REQSCAN == null)
                {
                    socketConnectedThread_REQSCAN = new Thread(SENDPING);
                    socketConnectedThread_REQSCAN.Start();
                }
                _SEND_REQSCAN_On = true;
                _Event_SEND_REQSCAN.Set();
                _times_Send_REQSCAN = 0;
                _times_Receive_REQSCAN = 0;
            }
            catch
            {

            }
        }
        public void SENDPING()
        {
            while (true)
            {
                if (_SEND_REQSCAN_On)
                {
                    for (; _times_Send_REQSCAN < _times_Total_REQSCAN; )
                    {
                        if (_scm._isConnected)
                        {
                            _scm.SendMsg(PackingSocketMsg("LON"));
                            AppendSendText("BARCODE(" + (++_times_Send_REQSCAN).ToString() + "/" + _times_Total_REQSCAN.ToString() + "):LON_Send");
                        }
                        else
                        {
                            _times_Send_REQSCAN = _times_Total_REQSCAN;
                        }
                        if (_times_Send_REQSCAN < _times_Total_REQSCAN)
                        {
                            Thread.Sleep(_timer_REQSCAN);
                        }
                    }
                    _SEND_REQSCAN_On = false;
                }
                else
                {
                    _Event_SEND_REQSCAN.WaitOne();
                }
            }
        }
        private void TB_STOP_SCANREQ_Click(object sender, RoutedEventArgs e)
        {
            _times_Total_REQSCAN = 0;
            _times_Send_REQSCAN = 0;
            _times_Receive_REQSCAN = 0;
            TB_TIMES_SCANREQ.Text = "1";
        }
        #endregion

        private void BT_CLEAN_Click(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                TB_LOG.Text = null;
            }));
        }

        public void ClientOnReceiveMsg(byte[] _byte)
        {
            try
            {
                int i = 0;
                foreach (char _char in _byte)
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
                string ReceiveStr = Encoding.ASCII.GetString(_byte, 0, i);
                //接受到PCB Scanner的消息，
                if (i != 0)
                {
                    LNBRecePCBID _LNBRecePCBID = new LNBRecePCBID();
                    _LNBRecePCBID.PCBID = ReceiveStr.Replace("\r", "");
                    StaticObj.StrList_LNBRecePCBID.Add(_LNBRecePCBID);
                    AppendReceiveText("BARCODE(" + (++_times_Receive_REQSCAN).ToString() + "/" + _times_Total_REQSCAN.ToString() + "):" + ReceiveStr.Replace("\r","") + "_Received");
                    _scm.SendMsg(PackingSocketMsg("LOFF"));
                    AppendSendText("BARCODE(" + _times_Send_REQSCAN.ToString() + "/" + _times_Total_REQSCAN.ToString() + "):LOFF_Send");
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        TB_TIMES_SCANREQ.Text = (_times_Total_REQSCAN - _times_Send_REQSCAN).ToString();
                    }));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }
        }

        public void ClientOnConnected()
        {
            AppendReceiveText("AIP已连接");
            ViewSocketOn();
        }

        public void ClientOnFailedConnect()
        {
            AppendReceiveText("无法连接AIP");
            ViewSocketOff();
        }

        public void ClientOnServerDisconnect()
        {
            AppendReceiveText("AIP断开连接");
            BT_STOP_Click(null, null);
            Thread.Sleep(1000);
            if (_scm == null)
            {
                _scm = new SocketClientManager(1024*8);
                _scm.OnReceiveMsg += ClientOnReceiveMsg;
                _scm.OnConnected += ClientOnConnected;
                _scm.OnFaildConnect += ClientOnFailedConnect;
                _scm.OnDisconnect += ClientOnServerDisconnect;
            }
            else
            {
                _scm.Stop();
            }
            _scm.Start(_ip, _port);
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

        private void ViewSocketOn()
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                BT_START.IsEnabled = false;
                BT_STOP.IsEnabled = true;
                GP_MSG.IsEnabled = true;
            }));
        }
        private void ViewSocketOff()
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                BT_START.IsEnabled = true;
                BT_STOP.IsEnabled = false;
                GP_MSG.IsEnabled = false;
            }));
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

        public byte[] PackingSocketMsg(string MsgString)
        {
            List<byte> bytelistForPacking = new List<byte>();
            bytelistForPacking.AddRange(Encoding.ASCII.GetBytes(MsgString));
            bytelistForPacking.AddRange(Encoding.ASCII.GetBytes(_ENTER.ToString()));
            byte[] sendMsg = new byte[bytelistForPacking.Count];
            bytelistForPacking.CopyTo(sendMsg);
            return sendMsg;
        }

    }
}
