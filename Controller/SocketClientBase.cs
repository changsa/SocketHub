
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UtilLibrary;

namespace AIP.Elink.MesConvertor.Controller.SocketChannel
{
    abstract class SocketClientBase
    {
        //Const Data
        private const char _STX = (char)0x02;
        private const char _SOH = (char)0x01;
        private const char _ETX = (char)0x03;
        public const string CONVERTOR = "AIP";
        public const string MES = "MES";
        public const string PANACIM = "PANACIM";
        public const string ERROR = "ERROR";
        //Msg Class 
        string _msgClass4Log = null;
        //Socket
        public SocketClientManager _scm = null;
        string _str_MsgContFromMES = null;
        private string _ip;
        private int _port;
        private int _timer;
        private int _PingRetryCount = 3;
        private int _PING_RetryTimes = 0;
        List<byte> _receivedSocketBuffer;
        private bool _PING_Send = false;
        private bool _PING_Received = false;

        //三次失败睡觉
        private bool _socketReset = false;
        //成功被叫起床

        //Thread 
        private Thread _mainThread = null;
        private bool _mainThread_isOn = false;
        private Thread _childTheadPING = null;
        private bool _childThreadPING_isOn = false;

        //Reset Event
        private AutoResetEvent _childThreadPING_ResetEvent = new AutoResetEvent(false);
        //Locker
        private readonly object _clientOnReceiveLocker = new object();

        //Callback
        public delegate void OnSocketConvertorListViewMsgHandler( string _msgDetail,string _send, string _received,string _status);
        public event OnSocketConvertorListViewMsgHandler OutputReceiveDataToListView_Callback;

        public SocketClientBase(string MsgClass)
        {
            _msgClass4Log = MsgClass;
            _receivedSocketBuffer =  new List<byte>();
            //默认开启PING MSG 待命状态
            _childTheadPING = new Thread(PINGSocketThread);
            _childThreadPING_isOn = false;
            _childTheadPING.Start();
        }

        #region Parents Start/Stop
        public void Start(int timer,string ip,int port)
        {
            try
            {
                AppendToListView("Socket " + _msgClass4Log + " : Start ! IP >>" + ip + ":" + port.ToString(), CONVERTOR, MES,"OK");
                //初始化变量
                _PING_Send = false;
                _PING_Received = false;

                //判断赋值
                if (_timer != timer || _ip != ip || _port != port)
                {
                    _ip = ip;
                    _port = port;
                    _timer = timer;
                }
               
                //开启Socket端口
                try
                {
                    if (_scm != null)
                    {
                        _scm.Stop();
                    }
                    else
                    {
                        _scm = new SocketClientManager(1024*8);
                        _scm.OnReceiveMsg += ClientOnReceiveMsg;
                        _scm.OnConnected += ClientOnConnected;
                        _scm.OnFaildConnect += ClientOnFailedConnect;
                        _scm.OnDisconnect += ClientOnServerDisconnect;
                    }
                    
                    _scm.Start(_ip, _port);
                }
                catch
                {
                    AppendToListView("Socket " + _msgClass4Log + " : SOCKETBASE_0001", "SOCKETBASE", ERROR , "NG");
                    _scm.Stop();
                }

                //开启主线程
                if(_mainThread == null)
                {
                    _mainThread = new Thread(MainSocketThread);
                    _mainThread_isOn = true;
                    _mainThread.Start();
                }
                else
                {
                    //线程不在运行或者线程已经被Aborted
                    if (_mainThread.ThreadState != ThreadState.Running || _mainThread.ThreadState == ThreadState.Aborted)
                    {
                        //开始主线程
                        _mainThread = new Thread(MainSocketThread);
                        _mainThread_isOn = true;
                        _mainThread.Start();
                    }//如果线程正在运行
                    else if (_mainThread.ThreadState == ThreadState.Running)
                    {
                        return;
                    }
                    else
                    {
 
                    }
                }
            }
            catch
            {
                AppendToListView("Socket " + _msgClass4Log + " : SOCKETBASE_0002", "SOCKETBASE", ERROR, "NG");
            }
        }
        public bool Stop()
        {
            AppendToListView("Socket " + _msgClass4Log + " : STOP ! IP >>" + _ip + ":" + _port.ToString(), CONVERTOR, MES, "OK");
            try
            {
                //关闭主线程
                if (_mainThread != null)
                {
                    if (_mainThread.ThreadState == ThreadState.Running)
                    {
                        _mainThread_isOn = false;
                    }
                    else
                    {
                        //如果线程不为空并且也没有在运行的话，那么关闭线程并且清空。
                        _mainThread_isOn = false;
                        _mainThread.Abort();
                        _mainThread = null;
                    }
                }

                //关闭PING子线程
                if (_childTheadPING.ThreadState == ThreadState.Running)
                {
                    _childThreadPING_isOn = false;
                }

                //关闭Socket端口
                return _scm.Stop();
            }
            catch
            {
                AppendToListView("Socket " + _msgClass4Log + " : SOCKETBASE_0003", "SOCKETBASE", ERROR, "NG");
                return false;
            }
        }
        public bool CheckStatus()
        {
            if (_scm != null)
            {
                return _scm._isConnected;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region Thread
        private void MainSocketThread()
        {
            while (_mainThread_isOn)
            {
                try
                {
                    Thread.Sleep(1000);
                    //如果Ping三次失败
                    if (_socketReset || !_scm._isConnected)
                    {
                        //让PING线程睡觉
                        _childThreadPING_isOn = false;
                        _childThreadPING_ResetEvent.Set();

                        //重新连接Socket
                        _scm.Stop();
                        Thread.Sleep(1000);
                        //_scm = new SocketClientManager();
                        //_scm.OnReceiveMsg += ClientOnReceiveMsg;
                        //_scm.OnConnected += ClientOnConnected;
                        //_scm.OnFaildConnect += ClientOnFailedConnect;
                        _scm.Start(_ip, _port);
                        Thread.Sleep(1000);
                    }
                    
                    if (_scm._isConnected)
                    {
                        //如果PING线程睡觉，那么唤醒PING线程
                        if (_childTheadPING.ThreadState == ThreadState.WaitSleepJoin)
                        {
                            _childThreadPING_isOn = true;
                            _childThreadPING_ResetEvent.Set();
                        }
                        else if (_childTheadPING.ThreadState == ThreadState.Stopped)
                        {
                            _childTheadPING.Start();
                            _childThreadPING_isOn = true;
                            _childThreadPING_ResetEvent.Set();
                        }
                    }
                }
                catch
                {
                    AppendToListView("Socket " + _msgClass4Log + " : SOCKETBASE_0004", "SOCKETBASE", ERROR, "NG");
                }
                Thread.Sleep(_timer);
            }
            
        }
        private void PINGSocketThread()
        {
            while (true)
            {
                try
                {
                    if (_childThreadPING_isOn)
                    {
                        if (_scm != null && _scm._isConnected)
                        {
                            //判断发送PING Request
                            //没发PING没接收PING    或    发送PING接收PING
                            if ((!_PING_Send && !_PING_Received) || (_PING_Send && _PING_Received))
                            {
                                _PING_Send = true;
                                _PING_Received = false;
                                _socketReset = false;
                                _scm.SendMsg(PackingSocketMsg("PING_REQ"));
                                AppendToListView("Socket " + _msgClass4Log + " <<Send : PING_REQ", CONVERTOR, MES, "OK");
                                
                                _PING_RetryTimes = 0;
                            }
                            else
                            {
                                //没法PING 并且 接收PING
                                if (!_PING_Send && _PING_Received)
                                {
                                    _PING_Send = false;
                                    _PING_Received = false;
                                    _socketReset = true;
                                    _childThreadPING_isOn = false;
                                }
                                //发送送没收到
                                else if (_PING_Send && !_PING_Received)
                                {
                                    //重发的次数小于三次
                                    if (_PING_RetryTimes < _PingRetryCount) 
                                    {
                                        _PING_Send = true;
                                        _PING_Received = false;
                                        AppendToListView("Socket " + _msgClass4Log + " : PING is no repsonse,Try " + (++_PING_RetryTimes).ToString() + " times : " + _ip + ":" + _port.ToString()
                                                        + " : Will Try Send PING in " + (_timer / 1000).ToString() + " s !", "SOCKETBASE", ERROR,"NG");
                                        _scm.SendMsg(PackingSocketMsg("PING_REQ"));
                                        //AppendToListView("Socket " + _msgClass4Log + " : PING_REQ : " + _PING_RetryTimes.ToString() + "/" + _PingRetryCount.ToString(), CONVERTOR, MES,"OK");
                                    }
                                    //三次失败，自己去睡觉
                                    else if (_PING_RetryTimes >= _PingRetryCount)
                                    {
                                        _PING_Send = false;
                                        _PING_Received = false;
                                        _PING_RetryTimes = 0;
                                        AppendToListView("Socket " + _msgClass4Log + " : Channel will be reset since Ping is no repsonse 3 times : " + _ip + ":" + _port.ToString()
                                                    , "SOCKETBASE", ERROR, "NG");
                                        
                                        //自己睡觉，告诉主线程停止循环自己
                                        _socketReset = true;
                                        _childThreadPING_isOn = false;
                                    }
                                }
                            }
                        }
                        else
                        {
                            _PING_Send = false;
                            _PING_Received = false;
                            _socketReset = false;
                            _childThreadPING_isOn = false;
                        }
                    }
                    else
                    {
                        _PING_Send = false;
                        _PING_Received = false;
                        //等待主人(线程)叫起床
                        _childThreadPING_ResetEvent.WaitOne();
                    }
                }
                catch
                {
                    AppendToListView("Socket " + _msgClass4Log + " : SOCKETBASE_0006", "SOCKETBASE", ERROR, "NG");
                }
                Thread.Sleep(_timer);
            }
        }
        #endregion

        #region  Socket CallBack
        private void ClientOnConnected()
        {
            try
            {
                AppendToListView("Socket " + _msgClass4Log + " : Successful Connection to MES : " + _ip + ":" + _port.ToString(), MES, CONVERTOR, "OK");
                //让PING线程起床
                _childThreadPING_isOn = true;
                _childThreadPING_ResetEvent.Set();
            }
            catch
            {
                AppendToListView("Socket " + _msgClass4Log + " : SOCKETBASE_0007", "SOCKETBASE", ERROR, "NG");
            }
        }
        private void ClientOnFailedConnect()
        {
            try
            {
                AppendToListView("Socket " + _msgClass4Log + " : Failed Connection to MES : " + _ip + ":" + _port.ToString()
                    + " : Will Try Again in " + (_timer / 1000).ToString() + " s !", CONVERTOR, MES, "NG");
                //PING停止
                _childThreadPING_isOn = false;
                _childThreadPING_ResetEvent.Set();
            }
            catch
            {
                AppendToListView("Socket " + _msgClass4Log + " : SOCKETBASE_0008", "SOCKETBASE", ERROR, "NG");
            }
        }
        private void ClientOnServerDisconnect()
        {
            AppendToListView("Socket " + _msgClass4Log + " : MES Server is Shutdown : " + _ip + ":" + _port.ToString()
                    + " : Will Try Again in " + (_timer / 1000).ToString() + " s !", CONVERTOR, MES, "NG");

            _socketReset = true;
            Stop();
            Thread.Sleep(2000);
            Start(_timer,_ip,_port);
        }
        private void ClientOnReceiveMsg(byte[] _byte)
        {
            try
            {
                lock (_clientOnReceiveLocker)
                {
                    try
                    {
                        bool wholeMsg = false;
                        if (_scm._socketClientInfo != null)
                        {
                            if (_byte[0] == 0)
                            {
                                return;
                            }
                            foreach (byte C in _byte)
                            {
                                if (C == _ETX)
                                {
                                    wholeMsg = true;
                                }
                            }
                            if (wholeMsg)
                            {
                                _receivedSocketBuffer.AddRange(_byte);
                                ParseMesMsg(_receivedSocketBuffer);
                                _receivedSocketBuffer.Clear();
                            }
                            else
                            {
                                if ((_receivedSocketBuffer.Count() + _byte.Count()) > 1024 * 16)
                                {
                                    _receivedSocketBuffer.Clear();
                                }
                                else
                                {
                                    _receivedSocketBuffer.AddRange(_byte);
                                }
                            }
                        }
                    }
                    catch
                    {
                        _receivedSocketBuffer.Clear();
                        AppendToListView("Socket " + _msgClass4Log + " : SOCKETBASE_0009", "SOCKETBASE", ERROR, "NG");
                    }
                    return;
                }
            }
            catch
            {
                AppendToListView("Socket " + _msgClass4Log + " : SOCKETBASE_0010", "SOCKETBASE", ERROR, "NG");
            }
        }
        #endregion

        #region Process Data
        public virtual bool ParseMesMsg(List<byte> byteList_receMsg)
        {
            lock (this)
            {
                try
                {
                    int index = 0;
                    int headerStartIndex = -1;
                    int contStartIndex = -1;
                    int contEndIndex = -1;
                    int headerStrLengh;
                    int msgLengh;
                    foreach (byte C in byteList_receMsg)
                    {
                        if (C == _STX)
                        {
                            headerStartIndex = index;
                        }
                        if (C == _SOH)
                        {
                            contStartIndex = index;
                        }
                        if (C == _ETX)
                        {
                            contEndIndex = index;
                        }
                        index++;
                    }
                    if (headerStartIndex != -1 && contStartIndex != -1 && contEndIndex != -1)
                    {
                        headerStrLengh = contStartIndex - headerStartIndex - 1;
                        byte[] byteheader = new byte[headerStrLengh];
                        for (int i = 0; i < headerStrLengh; i++)
                        {
                            byteheader[i] = byteList_receMsg[headerStartIndex + 1 + i]; ;
                        }
                        int.TryParse(Encoding.ASCII.GetString(byteheader), out msgLengh);

                        _str_MsgContFromMES = Encoding.ASCII.GetString(byteList_receMsg.ToArray(), contStartIndex + 1, msgLengh);

                        if (_str_MsgContFromMES == "ERROR")
                        {
                            return false;
                        }
                        else if (_str_MsgContFromMES == "PING_RSP")
                        {
                            _PING_Received = true;
                            //MES -> Convertor = log
                            AppendToListView("Socket " + _msgClass4Log + " <<Receive : PING_RSP", MES, CONVERTOR, "OK");
                            return true;
                        }
                        else
                        {
                            if (_scm._isConnected)
                            {
                                SendMsgToPanaCIM(_str_MsgContFromMES);
                            }
                            else
                            {
                                AppendToListView("ConverSocket_" + _msgClass4Log + "_00006_MES is Disconnected,the message has been thrown away!", ERROR, ERROR, "NG");
                            }
                            return true;
                        }
                    }
                    else
                    {
                        AppendToListView("Socket " + _msgClass4Log + " : SOCKETBASE_0011", "SOCKETBASE", ERROR, "NG");
                        return false;
                    }
                }
                catch
                {
                    AppendToListView("Socket " + _msgClass4Log + " : SOCKETBASE_0012", "SOCKETBASE", ERROR, "NG");
                    return false;
                }
            }
        }
        public abstract bool SendMsgToMES(string _msgString, string _ip);
        public abstract bool SendMsgToPanaCIM(string XMLString);
        //打印Log到界面
        public void AppendToListView(string _messageDetail, string _sender = "MES", string _receiver = "AIP", string _status = "OK")
        {
            OutputReceiveDataToListView_Callback(_messageDetail, _sender, _receiver, _status);
            return;
        }
        //封装Msg数据
        public byte[] PackingSocketMsg(string MsgString)
        {
            List<byte> bytelistForPacking = new List<byte>() ;
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
        #endregion
    }
}
