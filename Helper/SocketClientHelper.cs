using NLog;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace UtilLibrary
{
    public class SocketClientManager
    {
        private EndPoint RemoteEndPoint = null;
        public StateObjectSocketClient _socketClientInfo = null;
        private Logger logger = LogManager.GetLogger("SocketHelper.SocketClientManager");
        public bool _isConnected = false;
        private string _ip = null;
        private int _port = 0;

        public delegate void OnConnectedHandler();
        public event OnConnectedHandler OnConnected;
        public event OnConnectedHandler OnFaildConnect;
        public event OnConnectedHandler OnDisconnect;
        public delegate void OnConnectedWithRemoteIPHandler(string IP);
        public event OnConnectedWithRemoteIPHandler OnConnectedWithRemoteIP;
        public event OnConnectedWithRemoteIPHandler OnFaildConnectWithRemoteIP;
        public event OnConnectedWithRemoteIPHandler OnDisconnectWithRemoteIP;

        public delegate void OnReceiveMsgHandler(byte[] _byte);
        public event OnReceiveMsgHandler OnReceiveMsg;
        public delegate void OnReceiveWithRemoteIPMsgHandler(byte[] _byte, string IP);
        public event OnReceiveWithRemoteIPMsgHandler OnReceiveWithRemoteIPMsg;
        private ManualResetEvent connectDone = new ManualResetEvent(false);

        public SocketClientManager(int BuffSize)
        {
            _socketClientInfo = new StateObjectSocketClient();
            _socketClientInfo.BUFF_SIZE = BuffSize;
            _socketClientInfo.buffer = new byte[BuffSize];
        }

        public void Start(string ip, int port)
        {
            try
            {
                _ip = ip;
                _port = port;
                IPAddress _ipAdd = IPAddress.Parse(ip);
                RemoteEndPoint = new IPEndPoint(_ipAdd, port);
                _socketClientInfo.workSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socketClientInfo.workSocket.BeginConnect(RemoteEndPoint, new AsyncCallback(ConnectedCallback), _socketClientInfo.workSocket);
                logger.Info("Start Socket Client to Connect IP:" + RemoteEndPoint.ToString());
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }
        }
        
        public bool Stop()
        {
            _isConnected = false;
            try
            {
                _socketClientInfo.workSocket.Shutdown(System.Net.Sockets.SocketShutdown.Both);
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
                return false;
            }
            _socketClientInfo.workSocket.Close();
            _socketClientInfo.workSocket = null;
            logger.Info("Stop Socket Client on Connection with IP:" + RemoteEndPoint.ToString());
            return true;
        }

        private void ConnectedCallback(IAsyncResult ar)
        {
            _socketClientInfo.workSocket = ar.AsyncState as Socket;
            if (_socketClientInfo.workSocket.Connected)
            {
                _isConnected = true;
                if (this.OnConnected != null) OnConnected();
                if (this.OnConnectedWithRemoteIP != null) OnConnectedWithRemoteIP(RemoteEndPoint.ToString());
                try
                {
                    // Begin receiving the data from the remote device.
                    _socketClientInfo.workSocket.BeginReceive(_socketClientInfo.buffer, 0, _socketClientInfo.BUFF_SIZE, 0, new AsyncCallback(ReceiveCallback), _socketClientInfo);
                }
                catch (Exception ex)
                {
                    IsServerShutdown();
                    logger.Error(ex.ToString());
                }
            }
            else
            {
                _isConnected = false;
                if (this.OnFaildConnect != null) OnFaildConnect();
                if (this.OnFaildConnectWithRemoteIP != null) OnFaildConnectWithRemoteIP(RemoteEndPoint.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StateObjectSocketClient  state = (StateObjectSocketClient)ar.AsyncState;
                // Read data from the remote device.
                int bytesRead = state.workSocket.EndReceive(ar);

                if (bytesRead > 0)
                {
                    //response = state.sb.ToString();
                    _isConnected = true;
                    if (OnReceiveMsg != null) OnReceiveMsg(state.buffer);
                    if (OnReceiveWithRemoteIPMsg != null) OnReceiveWithRemoteIPMsg(state.buffer, _socketClientInfo.workSocket.RemoteEndPoint.ToString());
                    Array.Clear(_socketClientInfo.buffer, 0, _socketClientInfo.BUFF_SIZE);
                    // Get the rest of the data.
                    state.workSocket.BeginReceive(state.buffer, 0, _socketClientInfo.BUFF_SIZE, 0, new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    IsServerShutdown();
                }
            }
            catch(Exception ex)
            {
                logger.Error(ex.ToString());
                if (_socketClientInfo.workSocket == null)
                {
                    IsServerShutdown();
                }
                else
                {
                    if (_socketClientInfo.workSocket.Connected != true)
                    {
                        IsServerShutdown();
                    }
                }
            }
        }

        public void SendMsg(string msg)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(msg);
            try
            {
                _socketClientInfo.workSocket.Send(buffer);
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
                if (!_socketClientInfo.workSocket.Connected)
                {
                    IsServerShutdown();
                }
            }
        }

        public void SendMsg(byte[] msg)
        {
            try
            {
                _socketClientInfo.workSocket.Send(msg);
            }
            catch(Exception ex)
            {
                logger.Error(ex.ToString());
                
                IsServerShutdown();
            }
        }

        public void IsServerShutdown()
        {
            _isConnected = false;
            if (OnDisconnect != null) OnDisconnect();
            if (OnDisconnectWithRemoteIP != null) OnDisconnectWithRemoteIP(RemoteEndPoint.ToString());
        }
    }
    public class StateObjectSocketClient
    {
        public Socket workSocket = null;
        public int BUFF_SIZE = 0;
        public byte[] buffer = null;
    }
}

