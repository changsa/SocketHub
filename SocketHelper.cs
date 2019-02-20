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

        public SocketManager()
        {
            _listSocketInfo = new Dictionary<string, StateObjectSocketServer>();
            
        }

        public void Start( string ip, int port)
        {
            ipAdd = IPAddress.Parse(ip);
            _endPoint = new IPEndPoint(ipAdd, port);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(_endPoint); //绑定端口
            _socket.Listen(BACKLOG); //开启监听
            _ConnectedSwitch    = true;
            _DisconnectedSwitch = true;
            _diconnectSocket_Thread = new Thread(Diconnectlistener);
            _diconnectSocket_Thread.Start();
            _acceptSocket_Thread    = new Thread(AcceptClient);
            _acceptSocket_Thread.Start();
        }

        public bool Stop()
        {
            try
            {
                _ConnectedSwitch = false;
                _DisconnectedSwitch = false;
                _socket.Close();
                _socket.Dispose();
                foreach (StateObjectSocketServer s in _listSocketInfo.Values)
                {
                    s.workSocket.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                    if (s.workSocket.Connected == false && OnDisConnected != null)
                    {
                        OnDisConnected(s.workSocket.RemoteEndPoint.ToString());
                    }
                }
                _listSocketInfo.Clear();
                return true;
            }
            catch
            {
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
                            if (_listSocketInfo[ipPort].workSocket.Poll(10, SelectMode.SelectRead))
                            {
                                _listSocketInfo[ipPort].workSocket.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                                _listSocketInfo.Remove(ipPort);
                                if (OnDisConnected != null)
                                {
                                    OnDisConnected(ipPort);
                                } 
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                    Thread.Sleep(1000);
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
                    //MessageBox.Show(ex.ToString());
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
                    if (_ConnectedSwitch == false)
                    {
                        //return;
                    }
                    state.workSocket = listener.EndAccept(ar);
                    // Create the state object.
                    _listSocketInfo.Add(state.workSocket.RemoteEndPoint.ToString(), state);
                    if (OnConnected != null)
                    {
                        OnConnected(state.workSocket.RemoteEndPoint.ToString());
                    }
                    state.workSocket.BeginReceive(state.buffer, 0, StateObjectSocketServer.BUFFER_SIZE, 0, new AsyncCallback(ReceiveCallback), state);
                }
            }
            catch
            {
 
            }
        }
       
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                StateObjectSocketServer state = (StateObjectSocketServer)ar.AsyncState;
                if (state.workSocket.Connected)
                {
                    int bytesRead = state.workSocket.EndReceive(ar);
                    if (bytesRead > 0)
                    {
                        if (OnReceiveMsg != null) OnReceiveMsg(state.buffer,state.workSocket.RemoteEndPoint.ToString());

                        // Get the rest of the data.
                        state.workSocket.BeginReceive(state.buffer, 0, StateObjectSocketServer.BUFFER_SIZE, 0, new AsyncCallback(ReceiveCallback), state);
                    }
                }
                else
                {
                    _listSocketInfo.Remove(state.workSocket.RemoteEndPoint.ToString());
                }
            }
            catch
            {
 
            }
        }
        
        public void SendMsg(string text, string endPoint)
        {
            if (_listSocketInfo.Keys.Contains(endPoint) && _listSocketInfo[endPoint] != null)
            {
                _listSocketInfo[endPoint].workSocket.Send(Encoding.ASCII.GetBytes(text));
            }
        }
        
        public void SendMsg(byte[] text, string endPoint)
        {
            if (_listSocketInfo.Keys.Contains(endPoint) && _listSocketInfo[endPoint] != null)
            {
                if (!_listSocketInfo[endPoint].workSocket.Poll(10, SelectMode.SelectRead))
                {
                    _listSocketInfo[endPoint].workSocket.Send(text);
                }
            }
        }

        public class StateObjectSocketServer
        {
            // Client  socket.
            public Socket workSocket = null;
            // Size of receive buffer.
            public const int BUFFER_SIZE = 1024 * 16;
            // Receive buffer.
            public byte[] buffer = new byte[BUFFER_SIZE];
        }
    }
}
