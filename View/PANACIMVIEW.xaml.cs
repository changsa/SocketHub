using SOCKETHUB.View.PanaCIMViewChild;
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
using System.Xml;
using UtilLibrary;

namespace SOCKETHUB.View
{
    /// <summary>
    /// PANACIMVIEW.xaml 的交互逻辑
    /// </summary>
    public partial class PANACIMVIEW : UserControl
    {
        //Const Data
        private const char _STX = (char)0x02;
        private const char _SOH = (char)0x01;
        private const char _ETX = (char)0x03;
        string _ip = null;
        int _port = 0;
        SocketClientManager _scm = null;

        // Msg Ping
        Thread socketConnectedThread_PING;
        static AutoResetEvent _Event_SEND_PING = new AutoResetEvent(false);
        bool _SEND_PING_On = false;
        int _timer_PING = 0;
        int _times_Total_PING = 0;
        int _times_Send_PING = 0;
        int _times_Receive_PING = 0;
        public static string MsgStr501 = null;

        //Msg 501
        Thread socketConnectedThread_501;
        static AutoResetEvent _Event_SEND_501 = new AutoResetEvent(false);
        bool _SEND_501_On = false;
        int _timer_501 = 0;
        int _times_Total_501 = 0;
        int _times_Send_501 = 0;
        int _times_Receive_501 = 0;
        int _Transaction_index_501 = 0;

        //Msg 550
        Thread socketConnectedThread_550;
        static AutoResetEvent _Event_SEND_550 = new AutoResetEvent(false);
        bool _SEND_550_On = false;
        int _timer_550 = 0;
        int _times_Total_550 = 0;
        int _times_Send_550 = 0;
        int _times_Receive_550 = 0;
        int _Transaction_index_550 = 0;
        public static string MsgStr550 = null;

        //Msg 551
        Thread socketConnectedThread_551;
        static AutoResetEvent _Event_SEND_551 = new AutoResetEvent(false);
        bool _SEND_551_On = false;
        int _timer_551 = 0;
        int _times_Total_551 = 0;
        int _times_Send_551 = 0;
        int _times_Receive_551 = 0;
        int _Transaction_index_551 = 0;
        public static string MsgStr551 = null;

        //Msg 580
        Thread socketConnectedThread_580;
        static AutoResetEvent _Event_SEND_580 = new AutoResetEvent(false);
        bool _SEND_580_On = false;
        int _timer_580 = 0;
        int _times_Total_580 = 0;
        int _times_Send_580 = 0;
        int _times_Receive_580 = 0;
        int _Transaction_index_580 = 0;
        public static string MsgStr580 = null;

        //Msg 551 Aoto
        Thread socketConnectedThread_551_Auto;
        static AutoResetEvent _Event_SEND_551_Auto = new AutoResetEvent(false);
        bool _SEND_551_Auto_On = false;
        int _timer_551_Auto = 0;
        List<string> StrList_551AutoTransID = new List<string>();

        //Msg Any
        Thread socketConnectedThread_Any;
        static AutoResetEvent _Event_SEND_Any = new AutoResetEvent(false);
        bool _SEND_Any_On = false;
        int _Transaction_index_Any = 0;
        public static string MsgStrAny = null;

        public PANACIMVIEW()
        {
            InitializeComponent();
        }


        private void UserControl_Loaded_1(object sender, RoutedEventArgs e)
        {
            BT_START.IsEnabled = true;
            BT_STOP.IsEnabled = false;
            GP_MSG.IsEnabled = false;
            GP_MSG_AUTO.IsEnabled = false;
            InitStaticMsgStr();
        }

        private void InitStaticMsgStr()
        {
            MsgStr501 = "<message> "
                        + " <header messageClass=\"501\" transactionID=\"501_0\" reply=\"1\">"
                        + "  <location routeID=\"1000\" routeName=\"Route-1\" equipmentID=\"1000\" equipmentName=\"S01-L01-NPM-M1\" zoneID=\"1001\" zonePos=\"1\" zoneName=\"ZONE_NPM01\" laneNo=\"1\" controllerGuid=\"\" />"
                        + " </header> "
                        + " <body> "
                        + "  <pcb barcode=\"1ABCDEABCDEABCDE123P00000000\" modelCode=\"1ABCDEABCDEABCDE123\" serialNo=\"P00000000\" pcbSide=\"1\" scannerMountSide=\"T\" /> "
                        + " </body> "
                        + "</message> ";

            MsgStr550 =   "<message> "
                        + " <header messageClass=\"550\" transactionID=\"550_0\" reply=\"1\" >"
                        + "  <location routeID=\"1001\" routeName=\"SMT-1\" equipmentID=\"1004\" equipmentName=\"S01-L01-NPM-M1\" zoneID=\"1000\" zonePos=\"1\" zoneName=\"S01-L01-NPM-M1\" laneNo=\"1\" controllerGuid=\"\" />"
                        + " </header> "
                        + " <body> "
                        + "  <panel state=\"0\" pcbID=\"PCBBAROCDE123467789\" productName=\"10001023909C0111-4\" productSide=\"1\" timestamp=\"2017-11-22T18:37:34+08:00\" stageNo=\"1\" />"
                        + "  <materials>  " 
                        + "   <material ref=\"1\" id=\"22101\" part=\"2.04.01.0023-00\" qty=\"2000\" p1=\"20021L\" p2=\"-1\" p3=\"21\" p4=\"0\" /> "
                        + "   <material ref=\"2\" id=\"21901\" part=\"2.05.00.0169-02\" qty=\"5000\" p1=\"20019L\" p2=\"-1\" p3=\"19\" p4=\"0\" /> "
                        + "   <material ref=\"3\" id=\"10012\" part=\"2.05.00.0169-02\" qty=\"3000\" p1=\"10010R\" p2=\"-1\" p3=\"10\" p4=\"1\" /> "
                        + "   <material ref=\"4\" id=\"10011\" part=\"2.05.00.0169-02\" qty=\"8000\" p1=\"10010L\" p2=\"-1\" p3=\"10\" p4=\"0\" /> "
                        + "   <material ref=\"5\" id=\"22401\" part=\"2.04.01.0647-00\" qty=\"1000\" p1=\"20024L\" p2=\"-1\" p3=\"24\" p4=\"0\" /> "
                        + "   <material ref=\"6\" id=\"22301\" part=\"2.04.01.0647-00\" qty=\"1000\" p1=\"20023L\" p2=\"-1\" p3=\"23\" p4=\"0\" /> "
                        + "   <material ref=\"7\" id=\"22201\" part=\"2.04.01.0023-00\" qty=\"5000\" p1=\"20022L\" p2=\"-1\" p3=\"22\" p4=\"0\" /> " 
                        + "  </materials> "
                        + "  <comps> "
                        + "   <comp pos=\"1\" ref=\"4\" refDes=\"L2\" />    <comp pos=\"1\" ref=\"4\" refDes=\"L3\" /> "
                        + "   <comp pos=\"2\" ref=\"3\" refDes=\"L4\" />    <comp pos=\"2\" ref=\"3\" refDes=\"L5\" /> "
                        + "   <comp pos=\"3\" ref=\"4\" refDes=\"L4\" />    <comp pos=\"3\" ref=\"4\" refDes=\"L5\" /> "
                        + "   <comp pos=\"4\" ref=\"4\" refDes=\"L2\" />    <comp pos=\"4\" ref=\"3\" refDes=\"L3\" /> "
                        + "   <comp pos=\"5\" ref=\"4\" refDes=\"L2\" />    <comp pos=\"5\" ref=\"4\" refDes=\"L3\" /> "
                        + "   <comp pos=\"6\" ref=\"3\" refDes=\"L4\" />    <comp pos=\"6\" ref=\"3\" refDes=\"L5\" /> "
                        + "   <comp pos=\"7\" ref=\"4\" refDes=\"L4\" />    <comp pos=\"7\" ref=\"4\" refDes=\"L5\" /> "
                        + "   <comp pos=\"8\" ref=\"3\" refDes=\"L4\" />    <comp pos=\"8\" ref=\"3\" refDes=\"L5\" /> "
                        + "   <comp pos=\"9\" ref=\"1\" refDes=\"R9\" />    <comp pos=\"9\" ref=\"2\" refDes=\"L1\" /> "
                        + "   <comp pos=\"10\" ref=\"2\" refDes=\"L2\" />   <comp pos=\"10\" ref=\"3\" refDes=\"L3\" /> "
                        + "   <comp pos=\"11\" ref=\"3\" refDes=\"L4\" />   <comp pos=\"11\" ref=\"3\" refDes=\"L5\" /> "
                        + "   <comp pos=\"12\" ref=\"3\" refDes=\"L4\" />   <comp pos=\"12\" ref=\"3\" refDes=\"L5\" /> "
                        + "  </comps>"
                        + "  <patterns>"
                        + "   <pattern pos=\"1\" name=\"11\" barcode=\"\" />   <pattern pos=\"2\" name=\"12\" barcode=\"\" />"
                        + "   <pattern pos=\"3\" name=\"12\" barcode=\"\" />   <pattern pos=\"4\" name=\"13\" barcode=\"\" /> "
                        + "   <pattern pos=\"5\" name=\"14\" barcode=\"\" />   <pattern pos=\"6\" name=\"15\" barcode=\"\" /> "
                        + "   <pattern pos=\"7\" name=\"16\" barcode=\"\" />   <pattern pos=\"8\" name=\"17\" barcode=\"\" /> "
                        + "   <pattern pos=\"9\" name=\"18\" barcode=\"\" />   <pattern pos=\"10\" name=\"19\" barcode=\"\" /> "
                        + "   <pattern pos=\"11\" name=\"22\" barcode=\"\"/>   <pattern pos=\"12\" name=\"22\" barcode=\"\"/>"
                        + "  </patterns> "
                        + " </body> "
                        + "</message> ";
           MsgStr551 = "<message>"
                        + " <header messageClass=\"551\" transactionID=\"551_0\" reply=\"1\" >  "
                        + "  <location routeID=\"1000\" routeName=\"SMT-1\" equipmentID=\"11001\" equipmentName=\"S01-L01-PRINT-M1\" zoneID=\"11001\" zonePos=\"3\" zoneName=\"S01-L01-PRINT-M1\" laneNo=\"1\" controllerGuid=\"\" />"
                        + " </header>"
                        + " <body>"
                        + "   <panel state=\"0\" pcbID=\"1ABCDEABCDEABCDE123P00000000\" productName=\"10002011502A021\" productSide=\"1\" timestamp=\"2017-12-08T17:24:31+08:00\" stageNo=\"1\" />"
                        + "   <materials>"
                        + "     <material ref=\"1\" id=\"000C2981D137-6165\" part=\"GW\" qty=\"1\" p1=\"2\" p2=\"-1\" p3=\"-1\" p4=\"0\" />"
                        + "     <material ref=\"2\" id=\"000C2981D137-6166\" part=\"GD\" qty=\"1\" p1=\"3\" p2=\"-1\" p3=\"-1\" p4=\"0\" />"
                        + "     <material ref=\"3\" id=\"000C2981D137-6164\" part=\"XG\" qty=\"1\" p1=\"1\" p2=\"-1\" p3=\"-1\" p4=\"0\" />"
                        + "   </materials>"
                        + " </body>"
                        + " </message>";

           MsgStr580 =   "<message>"
                        + " <header messageClass=\"580\" transactionID=\"580_0\" reply=\"1\" >"
                        + "  <location routeID=\"1001\" routeName=\"SMT-1\" equipmentID=\"1004\" equipmentName=\"S01-L01-NPM-M1\" zoneID=\"1000\" zonePos=\"1\" zoneName=\"S01-L01-NPM-M1\" laneNo=\"1\" controllerGuid=\"\" />"
                        + " </header><body>"
                        + "  <product name=\"10001074901B011B\" side=\"1\" />"
                        + "  <parts>"
                        + "    <part name=\"2.04.02.0403-00\" ref=\"1\" />"
                        + "    <part name=\"2.04.02.0446-00\" ref=\"2\" />"
                        + "    <part name=\"2.04.04.0253-00\" ref=\"3\" />"
                        + "    <part name=\"2.04.01.1873-00\" ref=\"4\" />"
                        + "    <part name=\"2.04.01.1640-00\" ref=\"5\" />"
                        + "    <part name=\"2.04.01.1984-00\" ref=\"6\" />"
                        + "    <part name=\"2.04.04.0251-00\" ref=\"7\" />"
                        + "    <part name=\"2.04.00.0192-00\" ref=\"8\" />"
                        + "    <part name=\"2.04.01.0867-00\" ref=\"9\" />"
                        + "  </parts>"
                        + "  <comps>"
                        + "    <comp pos=\"1\" ref=\"1\" refDes=\"C606\" />"
                        + "    <comp pos=\"1\" ref=\"1\" refDes=\"C608\" />"
                        + "    <comp pos=\"1\" ref=\"1\" refDes=\"C609\" />"
                        + "    <comp pos=\"1\" ref=\"1\" refDes=\"C610\" />"
                        + "    <comp pos=\"2\" ref=\"4\" refDes=\"R618\" />"
                        + "    <comp pos=\"2\" ref=\"4\" refDes=\"R619\" />"
                        + "    <comp pos=\"2\" ref=\"4\" refDes=\"R620\" />"
                        + "    <comp pos=\"2\" ref=\"4\" refDes=\"R621\" />"
                        + "  </comps>"
                        + "</body></message>";
           MsgStrAny = "  ";
        }

        private void BT_START_Click(object sender, RoutedEventArgs e)
        {
            _ip = TB_AIPIP.Text.Trim();
            _port = int.Parse(TB_PORT.Text.ToString());

            if (_scm == null)
            {
                _scm = new SocketClientManager(1024*8);
                _scm.OnReceiveMsg   += ClientOnReceiveMsg;
                _scm.OnConnected    += ClientOnConnected;
                _scm.OnFaildConnect += ClientOnFailedConnect;
                _scm.OnDisconnect   += ClientOnServerDisconnect;
            }
            else
            {
                _scm.Stop();
            }
            _scm.Start(_ip,_port);
        }

        private void BT_STOP_Click(object sender, RoutedEventArgs e)
        {
            _scm.Stop();
            ViewSocketOff();
        }

        public void ClientOnReceiveMsg(byte[] _byte)
        {
            try
            {
                int index = 0;
                int headerStartNum = -1;
                int contStartNum = -1;
                int contEndNum = -1;
                int headerLengh;
                int messLengh;
                if (_byte.Count() > 0)
                {
                    foreach (byte C in _byte)
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
                        byteheader[i] = _byte[headerStartNum + 1 + i]; ;
                    }

                    int.TryParse(Encoding.ASCII.GetString(byteheader), out messLengh);

                    //List转换数组
                    byte[] byteMessContFromMES = new byte[messLengh];
                    Array.Copy(_byte, contStartNum + 1, byteMessContFromMES, 0, messLengh);

                    //数组转换String
                    string msg = Encoding.ASCII.GetString(byteMessContFromMES).Replace("\0", "");

                    if (msg == "ERROR")
                    {
                        return;
                    }
                    else if (msg == "PING_RSP")
                    {
                        _times_Receive_PING++;
                        AppendReceiveText("PING_RSP(" + _times_Receive_PING + "/" + _times_Total_PING + "):Received");
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            TB_TIMES_PING.Text = (_times_Total_PING - _times_Receive_PING).ToString();
                        }));
                    }
                    else
                    {
                        try
                        {
                            XmlDocument doc = new XmlDocument();
                            doc.LoadXml(msg);
                            XmlNode messageNode = doc.SelectSingleNode("message");
                            XmlNode headerNode = messageNode.SelectSingleNode("header");
                            XmlElement headerE = (XmlElement)headerNode;

                            switch (headerE.GetAttribute("messageClass").ToString())
                            {
                                case "501":
                                    AppendReceiveText("Msg501(" + ++_times_Receive_501 + "/" + _times_Total_501 + ")Transaction ID :" + headerE.GetAttribute("transactionID").Split('_')[1].ToString() + "_Receive");
                                    this.Dispatcher.Invoke((Action)(() =>
                                    {
                                        TB_TIMES_501.Text = (_times_Total_501 - _times_Receive_501).ToString();
                                    }));
                                    break;
                                case "550":
                                    AppendReceiveText("Msg550(" + ++_times_Receive_550 + "/" + _times_Total_550 + ")Transaction ID :" + headerE.GetAttribute("transactionID").Split('_')[1].ToString() + "_Receive");
                                    this.Dispatcher.Invoke((Action)(() =>
                                    {
                                        TB_TIMES_550.Text = (_times_Total_550 - _times_Receive_550).ToString();
                                    }));
                                    break;
                                case "551":
                                    AppendReceiveText("Msg551(" + ++_times_Receive_551 + "/" + _times_Total_551 + ")Transaction ID :" + headerE.GetAttribute("transactionID").Split('_')[1].ToString() + "_Receive");
                                    this.Dispatcher.Invoke((Action)(() =>
                                    {
                                        TB_TIMES_551.Text = (_times_Total_551 - _times_Receive_551).ToString();
                                    }));
                                    break;
                                case "580":
                                    try
                                    {
                                        AppendReceiveText("Msg580(" + ++_times_Receive_580 + "/" + _times_Total_580 + ")Transaction ID :" + headerE.GetAttribute("transactionID").Split('_')[1].ToString() + "_Receive");
                                        this.Dispatcher.Invoke((Action)(() =>
                                        {
                                            TB_TIMES_580.Text = (_times_Total_580 - _times_Receive_580).ToString();
                                        }));
                                    }
                                    catch
                                    {
                                        AppendReceiveText("Msg580_Receive" +"Transaction ID :" + headerE.GetAttribute("transactionID"));
                                    }
                                    
                                    break;
                                
                                default:
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            AppendReceiveText("ERROR" + ex.ToString());
                        }
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

        private void ViewSocketOn()
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                BT_START.IsEnabled = false;
                BT_STOP.IsEnabled = true;
                GP_MSG.IsEnabled = true;
                GP_MSG_AUTO.IsEnabled = true;
            }));
        }
        private void ViewSocketOff()
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                BT_START.IsEnabled = true;
                BT_STOP.IsEnabled = false;
                GP_MSG.IsEnabled = false;
                GP_MSG_AUTO.IsEnabled = false;
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
        private void BT_CLEAN_Click(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                TB_LOG.Clear();
            }));
        }

        public byte[] PackingSocketMsg(string MsgString)
        {
            List<byte> bytelistForPacking = new List<byte>();
            int length = System.Text.Encoding.ASCII.GetByteCount(MsgString);
            bytelistForPacking.AddRange(Encoding.ASCII.GetBytes(_STX.ToString()));
            bytelistForPacking.AddRange(Encoding.ASCII.GetBytes(length.ToString()));
            bytelistForPacking.AddRange(Encoding.ASCII.GetBytes(_SOH.ToString()));
            bytelistForPacking.AddRange(Encoding.ASCII.GetBytes(MsgString));
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

        #region Msg PING
        private void TB_SEND_PING_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _timer_PING = int.Parse(TB_TIMER_PING.Text);
                _times_Total_PING = int.Parse(TB_TIMES_PING.Text);
                //Msg Ping
                if (socketConnectedThread_PING == null)
                {
                    socketConnectedThread_PING = new Thread(SENDPING);
                    socketConnectedThread_PING.IsBackground = true;
                    socketConnectedThread_PING.Start();
                }
                _SEND_PING_On = true;
                _Event_SEND_PING.Set();
                _times_Send_PING = 0;
                _times_Receive_PING = 0;
            }
            catch
            {

            }
        }
        public void SENDPING()
        {
            while (true)
            {
                if (_SEND_PING_On)
                {
                    for (; _times_Send_PING < _times_Total_PING; )
                    {
                        _times_Send_PING++;
                        if (_scm._isConnected)
                        {
                            _scm.SendMsg(PackingSocketMsg("PING_REQ"));
                            AppendSendText("PING_REQ(" + _times_Send_PING.ToString() + "/" + _times_Total_PING.ToString() + "):Send");
                        }
                        else
                        {
                            _times_Send_PING = _times_Total_PING;
                        }
                        if (_times_Send_PING < _times_Total_PING)
                        {
                            Thread.Sleep(_timer_PING);
                        }
                    }
                    _SEND_PING_On = false;
                }
                else
                {
                    _Event_SEND_PING.WaitOne();
                }
            }
        }
        private void TB_STOP_PING_Click(object sender, RoutedEventArgs e)
        {
            _times_Total_PING = 0;
            _times_Send_PING = 0;
            _times_Receive_PING = 0;
            TB_TIMES_PING.Text = "1";
        }
        #endregion

        #region Msg 501
        private void TB_SEND_501_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _timer_501 = int.Parse(TB_TIMER_501.Text);
                _times_Total_501 = int.Parse(TB_TIMES_501.Text);
                //Msg Ping
                if (socketConnectedThread_501 == null)
                {
                    socketConnectedThread_501 = new Thread(SEND501);
                    socketConnectedThread_501.IsBackground = true;
                    socketConnectedThread_501.Start();
                }
                _SEND_501_On = true;
                _Event_SEND_501.Set();
                _times_Send_501 = 0;
                _times_Receive_501 = 0;
                _Transaction_index_501 = 0;
            }
            catch
            {

            }
        }
        private void SEND501()
        {
            while (true)
            {
                if (_SEND_501_On)
                {
                    for (;_times_Send_501 < _times_Total_501;)
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(MsgStr501);
                        XmlNode _messageNode = doc.SelectSingleNode("message");
                        XmlNode _bodyNode = _messageNode.SelectSingleNode("header");
                        XmlElement _bodyNodeE = (XmlElement)_bodyNode;
                        int transactionID = int.Parse(_bodyNodeE.GetAttribute("transactionID").ToString().Split('_')[1]);
                        _bodyNode.Attributes["transactionID"].Value = "501_" + (++_Transaction_index_501).ToString();
                        MsgStr501 = doc.InnerXml;
                        _times_Send_501++;
                        if (_scm._isConnected)
                        {
                            _scm.SendMsg(PackingSocketMsg(MsgStr501));
                            AppendSendText("Msg501(" + _times_Send_501.ToString() + "/" + _times_Total_501.ToString() + ")Transaction ID :" + _Transaction_index_501.ToString()+"_Send");
                        }
                        else
                        {
                            _times_Send_501 = _times_Total_501;
                        }
                        if (_times_Send_501 < _times_Total_501)
                        {
                            Thread.Sleep(_timer_501);
                        }
                    }
                    _SEND_501_On = false;
                }
                else
                {
                    _Event_SEND_501.WaitOne();
                }
            }
        }
        private void TB_STOP_501_Click(object sender, RoutedEventArgs e)
        {
            _times_Total_501 = 0;
            _times_Send_501 = 0;
            _times_Receive_501 = 0;
            _Transaction_index_501 = 0;
            TB_TIMES_501.Text = "1";
        }
        private void TB_EDIT_501_Click(object sender, RoutedEventArgs e)
        {
            XmlEditor _XMLEditor = new XmlEditor("501");
            _XMLEditor.Show();
        }

        #endregion

        #region MSG 550
        private void TB_SEND_550_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _timer_550 = int.Parse(TB_TIMER_550.Text);
                _times_Total_550 = int.Parse(TB_TIMES_550.Text);
                //Msg Ping
                if (socketConnectedThread_550 == null)
                {
                    socketConnectedThread_550 = new Thread(SEND550);
                    socketConnectedThread_550.IsBackground = true;
                    socketConnectedThread_550.Start();
                }
                _SEND_550_On = true;
                _Event_SEND_550.Set();
                _times_Send_550 = 0;
                _times_Receive_550 = 0;
                _Transaction_index_550 = 0;
            }
            catch
            {

            }
        }
        private void TB_EDIT_550_Click(object sender, RoutedEventArgs e)
        {
            XmlEditor _XMLEditor = new XmlEditor("550");
            _XMLEditor.Show();

        }
        private void SEND550()
        {
            while (true)
            {
                if (_SEND_550_On)
                {
                    for (; _times_Send_550 < _times_Total_550; )
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(MsgStr550);
                        XmlNode _messageNode = doc.SelectSingleNode("message");
                        XmlNode _bodyNode = _messageNode.SelectSingleNode("header");
                        XmlElement _bodyNodeE = (XmlElement)_bodyNode;
                        int transactionID = int.Parse(_bodyNodeE.GetAttribute("transactionID").ToString().Split('_')[1]);
                        _bodyNode.Attributes["transactionID"].Value = "550_" + (++_Transaction_index_550).ToString();
                        MsgStr550 = doc.InnerXml;
                        _times_Send_550++;
                        if (_scm._isConnected)
                        {
                            _scm.SendMsg(PackingSocketMsg(MsgStr550));
                            AppendSendText("Msg550(" + _times_Send_550.ToString() + "/" + _times_Total_550.ToString() + ")Transaction ID :" + _Transaction_index_550.ToString() + "_Send");
                        }
                        else
                        {
                            _times_Send_550 = _times_Total_550;
                        }
                        if (_times_Send_550 < _times_Total_550)
                        {
                            Thread.Sleep(_timer_550);
                        }
                    }
                    _SEND_550_On = false;
                }
                else
                {
                    _Event_SEND_550.WaitOne();
                }
            }
        }
        private void TB_STOP_550_Click(object sender, RoutedEventArgs e)
        {
            _times_Total_550 = 0;
            _times_Send_550 = 0;
            _times_Receive_550 = 0;
            _Transaction_index_550 = 0;
            TB_TIMES_550.Text = "1";
        }
        #endregion

        #region MSG 551
        private void TB_SEND_551_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _timer_551 = int.Parse(TB_TIMER_551.Text);
                _times_Total_551 = int.Parse(TB_TIMES_551.Text);
                //Msg Ping
                if (socketConnectedThread_551 == null)
                {
                    socketConnectedThread_551 = new Thread(SEND551);
                    socketConnectedThread_551.IsBackground = true;
                    socketConnectedThread_551.Start();
                }
                _SEND_551_On = true;
                _Event_SEND_551.Set();
                _times_Send_551 = 0;
                _times_Receive_551 = 0;
                _Transaction_index_551 = 0;
            }
            catch
            {

            }
        }
        private void TB_EDIT_551_Click(object sender, RoutedEventArgs e)
        {
            XmlEditor _XMLEditor = new XmlEditor("551");
            _XMLEditor.Show();

        }
        private void SEND551()
        {
            while (true)
            {
                if (_SEND_551_On)
                {
                    for (; _times_Send_551 < _times_Total_551; )
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(MsgStr551);
                        XmlNode _messageNode = doc.SelectSingleNode("message");
                        XmlNode _bodyNode = _messageNode.SelectSingleNode("header");
                        XmlElement _bodyNodeE = (XmlElement)_bodyNode;
                        int transactionID = int.Parse(_bodyNodeE.GetAttribute("transactionID").ToString().Split('_')[1]);
                        _bodyNode.Attributes["transactionID"].Value = "551_" + (++_Transaction_index_551).ToString();
                        MsgStr551 = doc.InnerXml;
                        _times_Send_551++;
                        if (_scm._isConnected)
                        {
                            _scm.SendMsg(PackingSocketMsg(MsgStr551));
                            AppendSendText("Msg551(" + _times_Send_551.ToString() + "/" + _times_Total_551.ToString() + ")Transaction ID :" + _Transaction_index_551.ToString() + "_Send");
                        }
                        else
                        {
                            _times_Send_551 = _times_Total_551;
                        }
                        if (_times_Send_551 < _times_Total_551)
                        {
                            Thread.Sleep(_timer_551);
                        }
                    }
                    _SEND_551_On = false;
                }
                else
                {
                    _Event_SEND_551.WaitOne();
                }
            }
        }
        private void TB_STOP_551_Click(object sender, RoutedEventArgs e)
        {
            _times_Total_551 = 0;
            _times_Send_551 = 0;
            _times_Receive_551 = 0;
            _Transaction_index_551 = 0;
            TB_TIMES_551.Text = "1";
        }
        #endregion

        #region MSG 580
        private void TB_SEND_580_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _timer_580 = int.Parse(TB_TIMER_580.Text);
                _times_Total_580 = int.Parse(TB_TIMES_580.Text);
                //Msg Ping
                if (socketConnectedThread_580 == null)
                {
                    socketConnectedThread_580 = new Thread(SEND580);
                    socketConnectedThread_580.IsBackground = true;
                    socketConnectedThread_580.Start();
                }
                _SEND_580_On = true;
                _Event_SEND_580.Set();
                _times_Send_580 = 0;
                _times_Receive_580 = 0;
                _Transaction_index_580 = 0;
            }
            catch
            {

            }
        }
        private void TB_EDIT_580_Click(object sender, RoutedEventArgs e)
        {
            XmlEditor _XMLEditor = new XmlEditor("580");
            _XMLEditor.Show();

        }
        private void SEND580()
        {
            while (true)
            {
                if (_SEND_580_On)
                {
                    for (; _times_Send_580 < _times_Total_580; )
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(MsgStr580);
                        XmlNode _messageNode = doc.SelectSingleNode("message");
                        XmlNode _bodyNode = _messageNode.SelectSingleNode("header");
                        XmlElement _bodyNodeE = (XmlElement)_bodyNode;
                        int transactionID = int.Parse(_bodyNodeE.GetAttribute("transactionID").ToString().Split('_')[1]);
                        _bodyNode.Attributes["transactionID"].Value = "580_" + (++_Transaction_index_580).ToString();
                        MsgStr580 = doc.InnerXml;
                        _times_Send_580++;
                        if (_scm._isConnected)
                        {
                            _scm.SendMsg(PackingSocketMsg(MsgStr580));
                            AppendSendText("Msg580(" + _times_Send_580.ToString() + "/" + _times_Total_580.ToString() + ")Transaction ID :" + _Transaction_index_580.ToString() + "_Send");
                        }
                        else
                        {
                            _times_Send_580 = _times_Total_580;
                        }
                        if (_times_Send_580 < _times_Total_580)
                        {
                            Thread.Sleep(_timer_580);
                        }
                    }
                    _SEND_580_On = false;
                }
                else
                {
                    _Event_SEND_580.WaitOne();
                }
            }
        }
        private void TB_STOP_580_Click(object sender, RoutedEventArgs e)
        {
            _times_Total_580 = 0;
            _times_Send_580 = 0;
            _times_Receive_580 = 0;
            _Transaction_index_580 = 0;
            TB_TIMES_580.Text = "1";
        }
        #endregion

        #region MSG 551 AUTO
        private void TB_SEND_551_AUTO_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    TB_TIMES_551_AUTO.Text = "0";
                    TB_TIMES_Rece_551_AUTO.Text = "0";
                }));

                _timer_551_Auto = int.Parse(TB_TIMER_551_AUTO.Text);
                //Msg Ping
                if (socketConnectedThread_551_Auto == null)
                {
                    socketConnectedThread_551_Auto = new Thread(SEND551_AUTO);
                    socketConnectedThread_551_Auto.Start();
                }
                _SEND_551_Auto_On = true;
                _Event_SEND_551_Auto.Set();
            }
            catch
            {

            }
        }
        private void SEND551_AUTO()
        {
            string responseMsgString;
            while (true)
            {
                if (_SEND_551_Auto_On)
                {
                    for (int i = 0; i < StaticObj.StrList_LNBRecePCBID.Count; i++ )
                    {
                        if (StaticObj.StrList_LNBRecePCBID[i].Msg551 == true)
                        {
                            responseMsgString = "<message><header messageClass=\"551\" transactionID=\"551AUTO_" + StaticObj.StrList_LNBRecePCBID[i].PCBID + "\" reply=\"1\"><location routeID=\"1000\" routeName=\"SMT-1\" equipmentID=\"1000\" equipmentName=\"S01-L01-SPI-M1\" zoneID=\"1000\" zonePos=\"3\" zoneName=\"S01-L01-SPI-M1\" laneNo=\"1\" controllerGuid=\"\" /></header><body><panel state=\"0\" pcbID=\"" + StaticObj.StrList_LNBRecePCBID[i].PCBID + "\" productName=\"11\" productSide=\"1\" timestamp=\"2017-12-25T14:03:30+08:00\" stageNo=\"1\" /><materials><material ref=\"1\" id=\"000C2981D137-29242\" part=\"GW\" qty=\"1\" p1=\"2\" p2=\"-1\" p3=\"-1\" p4=\"0\" /><material ref=\"2\" id=\"000C2981D137-29240\" part=\"GD\" qty=\"1\" p1=\"3\" p2=\"-1\" p3=\"-1\" p4=\"0\" /><material ref=\"3\" id=\"000C2981D137-29238\" part=\"XG\" qty=\"1\" p1=\"1\" p2=\"-1\" p3=\"-1\" p4=\"0\" /></materials></body></message>";
                            if (_scm._isConnected)
                            {
                                StaticObj.StrList_LNBRecePCBID[i].Msg551 = false;
                                _scm.SendMsg(PackingSocketMsg(responseMsgString));
                                StrList_551AutoTransID.Add("551AUTO_" + StaticObj.StrList_LNBRecePCBID[i].PCBID);
                                AppendSendText("Msg551AUTO Transa ID :" + StaticObj.StrList_LNBRecePCBID[i].PCBID + "_Send");
                                this.Dispatcher.Invoke((Action)(() =>
                                {
                                    TB_TIMES_551_AUTO.Text = (int.Parse(TB_TIMES_551_AUTO.Text) + 1).ToString();
                                }));
                                Thread.Sleep(_timer_551_Auto);
                            }
                        }
                    }
                }
                else
                {
                    _Event_SEND_551.WaitOne();
                }

                Thread.Sleep(_timer_551_Auto * 5);
            }
        }
        private void TB_STOP_551_AUTOClick(object sender, RoutedEventArgs e)
        {
            _SEND_551_Auto_On = false;
            _Event_SEND_551.Set();
        }
        #endregion

        #region MSG Any
        private void TB_SEND_Any_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Msg Ping
                if (socketConnectedThread_Any == null)
                {
                    socketConnectedThread_Any = new Thread(SENDAny);
                    socketConnectedThread_Any.IsBackground = true;
                    socketConnectedThread_Any.Start();
                }
                _SEND_Any_On = true;
                _Event_SEND_Any.Set();
            }
            catch
            {

            }
        }
        private void TB_EDIT_Any_Click(object sender, RoutedEventArgs e)
        {
            XmlEditor _XMLEditor = new XmlEditor("Any");
            _XMLEditor.Show();

        }
        private void SENDAny()
        {
            while (true)
            {
                if (_SEND_Any_On)
                {
                    
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(MsgStrAny);
                    MsgStrAny = doc.InnerXml;
                    if (_scm._isConnected)
                    {
                        _scm.SendMsg(PackingSocketMsg(MsgStrAny));
                        AppendSendText("MsgAny_Send");
                    }
                    else
                    {
                        AppendSendText("MsgAny_Failed Send");
                    }
                    _SEND_Any_On = false;
                }
                else
                {
                    _Event_SEND_Any.WaitOne();
                }
            }
        }
        private void TB_STOP_Any_Click(object sender, RoutedEventArgs e)
        {
            
        }
        #endregion

    }
}
