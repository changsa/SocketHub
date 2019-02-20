using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace UtilLibrary
{
    public class SocketManager
    {
        public Dictionary<string, StateObjectSocketServer> _listSocketInfo = null;
        private Logger logger = LogManager.GetLogger("SocketHelper.SocketManager");
        StateObjectSocketServer _stateInfo = null;
        Socket _socket      = null;
        EndPoint _endPoint  = null;
        IPAddress ipAdd     = null;
        int BACKLOG = 20;
        
        public delegate void OnConnectedHandler(string clientIP);
        public event OnConnectedHandler OnConnected;
        public event OnConnectedHandler OnDisConnected;
        public delegate void OnReceiveMsgHandler(byte[] buffer, string clientIP);
        public event OnReceiveMsgHandler OnReceiveMsg;

        //Thread Controller
        Thread _acceptSocket_Thread = null;
        Thread _diconnectSocket_Thread = null;
        public bool _DisconnectedSwitch = false;
        public bool _ConnectedSwitch    = false;
        //New Socket Connect
        private ManualResetEvent _NewSocket_Event = new ManualResetEvent(false);

        public SocketManager(int BuffSize)
        {
            _listSocketInfo = new Dictionary<string, StateObjectSocketServer>();
            _stateInfo = new StateObjectSocketServer();
            _stateInfo.BUFFER_SIZE = BuffSize;
            _stateInfo.buffer = new byte[BuffSize];
        }

        public void Start( string ip, int port)
        {
            try
            {
                _listSocketInfo.Clear();
                ipAdd = IPAddress.Parse(ip);
                _endPoint = new IPEndPoint(ipAdd, port);
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.Bind(_endPoint); //绑定端口
                _socket.Listen(BACKLOG); //开启监听
                _ConnectedSwitch = true;
                _DisconnectedSwitch = true;
                _diconnectSocket_Thread = new Thread(Diconnectlistener);
                _diconnectSocket_Thread.Start();
                _acceptSocket_Thread = new Thread(AcceptClient);
                _acceptSocket_Thread.Start();
                logger.Info("Start Socket Server IP:"+_endPoint.ToString());
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }
        }

        public bool Stop()
        {
            try
            {
                logger.Info("Stop Socket Server IP:" + _endPoint.ToString());
                _ConnectedSwitch = false;
                _DisconnectedSwitch = false;
                foreach (StateObjectSocketServer s in _listSocketInfo.Values)
                {
                    if (s.workSocket != null)
                    {
                        s.workSocket.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                    }
                    logger.Info("Shutdown Socket Connection IP:" + s.workSocket.RemoteEndPoint.ToString());
                    if (s.workSocket.Connected == false && OnDisConnected != null)
                    {
                        OnDisConnected(s.workSocket.RemoteEndPoint.ToString());
                    }
                }
                _socket.Close();
                _socket.Dispose();
                _listSocketInfo.Clear();
                return true;
            }
            catch(Exception ex)
            {
                logger.Error(ex.ToString());
                return false;
            }
        }

        private void Diconnectlistener()
        {
            while (_DisconnectedSwitch)
            {
                if (_listSocketInfo.Count > 0)
                {
                    try
                    {
                        foreach (string ipPort in _listSocketInfo.Keys)
                        {
                            bool removed = false;
                            if (_listSocketInfo[ipPort].hasReceivedData)
                            {
                                //如果Work Socket为空，那么Remove
                                if (_listSocketInfo[ipPort].workSocket == null)
                                {
                                    removed = true;
                                }
                                //如果已经断开
                                else if (_listSocketInfo[ipPort].isConnect == false)
                                {
                                    removed = true;
                                    _listSocketInfo[ipPort].workSocket.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                                }
                                //如果Socket 60秒没有接受数据，那么断开
                                else if (_listSocketInfo[ipPort].LatestRecTime + 60 < CurrentUnixTime())
                                {
                                    removed = true;
                                    _listSocketInfo[ipPort].workSocket.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                                }
                            }
                            else
                            {
                                if (_listSocketInfo[ipPort].workSocket.Poll(10, SelectMode.SelectRead))
                                {
                                    _listSocketInfo[ipPort].workSocket.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                                    removed = true;
                                } 
                            }
                            if (removed)
                            {
                                _listSocketInfo.Remove(ipPort);
                                if (OnDisConnected != null)
                                {
                                    OnDisConnected(ipPort);
                                }
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                        logger.Error(ex.ToString());
                    }
                    Thread.Sleep(500);
                }
                Thread.Sleep(1000);
            }
        }

        private void AcceptClient()
        {
            while (_ConnectedSwitch)
            {
                try
                {
                    _NewSocket_Event.Reset();
                    // Start an asynchronous socket to listen for connections.
                    _socket.BeginAccept(new AsyncCallback(AcceptCallback), _socket);
                    _NewSocket_Event.WaitOne() ;

                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                }
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                if (_ConnectedSwitch == false)
                {
                    return;
                }
                else
                {
                    _NewSocket_Event.Set();
                    // Get the socket that handles the client request.
                    Socket listener = (Socket)ar.AsyncState;
                    StateObjectSocketServer state = new StateObjectSocketServer();
                    state.BUFFER_SIZE = _stateInfo.BUFFER_SIZE;
                    state.buffer = new byte[_stateInfo.BUFFER_SIZE];
                    if (_ConnectedSwitch == false)
                    {
                        return;
                    }
                    state.workSocket = listener.EndAccept(ar);
                    state.LatestRecTime = CurrentUnixTime();
                    // Create the state object.
                    _listSocketInfo.Add(state.workSocket.RemoteEndPoint.ToString(), state);
                    if (OnConnected != null)
                    {
                        OnConnected(state.workSocket.RemoteEndPoint.ToString());
                    }
                    state.workSocket.BeginReceive(state.buffer, 0, _stateInfo.BUFFER_SIZE, 0, new AsyncCallback(ReceiveCallback), state);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }
        }
       
        private void ReceiveCallback(IAsyncResult ar)
        {
            StateObjectSocketServer state = (StateObjectSocketServer)ar.AsyncState;
            try
            {
                if (state.workSocket.Connected)
                {
                    int bytesRead = state.workSocket.EndReceive(ar);
                    if (bytesRead > 0)
                    {
                        if (_listSocketInfo.ContainsKey(state.workSocket.RemoteEndPoint.ToString()))
                        {
                            _listSocketInfo[state.workSocket.RemoteEndPoint.ToString()].LatestRecTime = CurrentUnixTime();
                            _listSocketInfo[state.workSocket.RemoteEndPoint.ToString()].hasReceivedData = true;
                        }
                        if (OnReceiveMsg != null) OnReceiveMsg(state.buffer, state.workSocket.RemoteEndPoint.ToString());
                        Array.Clear(state.buffer, 0, _stateInfo.BUFFER_SIZE);
                        // Get the rest of the data.
                        state.workSocket.BeginReceive(state.buffer, 0, _stateInfo.BUFFER_SIZE, 0, new AsyncCallback(ReceiveCallback), state);
                    }
                    else
                    {
                        //收到数据为0 那么判断需要Remove并且断开连接。
                        if (_listSocketInfo.ContainsKey(state.workSocket.RemoteEndPoint.ToString()))
                        {
                            _listSocketInfo[state.workSocket.RemoteEndPoint.ToString()].isConnect = false; 
                        }
                    }
                }
                else
                {
                    //收到的Call Back时已经断开连接
                    _listSocketInfo[state.workSocket.RemoteEndPoint.ToString()].workSocket = null; 
                }
            }
            catch (Exception ex)
            {
                //收到数据为0 那么判断需要Remove并且断开连接。
                logger.Error(ex.ToString());
                if (_listSocketInfo.ContainsKey(state.workSocket.RemoteEndPoint.ToString()))
                {
                    _listSocketInfo[state.workSocket.RemoteEndPoint.ToString()].isConnect = false;
                }
                
            }
        }
        
        public void SendMsg(string text, string endPoint)
        {
            try
            {
                if (_listSocketInfo.Keys.Contains(endPoint))
                {
                    if (_listSocketInfo[endPoint] != null)
                    {
                        _listSocketInfo[endPoint].workSocket.Send(Encoding.ASCII.GetBytes(text));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }
        }
        
        public void SendMsg(byte[] text, string endPoint)
        {
            try
            {
                if (_listSocketInfo.Keys.Contains(endPoint))
                {
                    if (_listSocketInfo[endPoint] != null)
                    {
                        if (_listSocketInfo[endPoint].workSocket.Connected)
                        {
                            _listSocketInfo[endPoint].workSocket.Send(text);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }
        }

        public long CurrentUnixTime()
        {
            TimeSpan cha = (DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1)));
            long t = (long)cha.TotalSeconds;
            return t;   
        }

        public class StateObjectSocketServer
        {
            // Client  socket.
            public Socket workSocket = null;
            // Size of receive buffer.
            public int BUFFER_SIZE = 0;
            // Receive buffer.
            public byte[] buffer = null;
            public long LatestRecTime;
            public bool isConnect = true;
            public bool hasReceivedData = false;
        }
    }
}
