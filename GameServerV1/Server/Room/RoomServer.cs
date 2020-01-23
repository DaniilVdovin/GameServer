using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Data.SqlClient;

namespace GameServerV1.Server
{

    public class RoomServer
    {
        public int PORT;
        public IPAddress ADDPRES;
        Socket socket;
        private const int bufSize = 8 * 1024;
        private State state = new State();
        private EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback recv = null;

        List<EndPoint> EndPoints = new List<EndPoint>();
        List<User> Users = new List<User>();

        public class State
        {
            public byte[] buffer = new byte[bufSize];
        }
        public RoomServer(IPAddress address, int port)
        {
            this.PORT = port;
            this.ADDPRES = address;

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            socket.Bind(new IPEndPoint(ADDPRES, PORT));
        }
        public void Connect(TcpClient? point)
        {
            //  Users.Add(new User());
            EndPoints.Add(point.Client.LocalEndPoint);

            socket.BeginConnect(point.Client.RemoteEndPoint, (ar) =>
             {
                 socket.EndConnect(ar);
                 Console.WriteLine($"Room on {PORT} : Connected {socket.RemoteEndPoint} ");
                 Send($"HTTP/1.1 200 OK\n\r\n\r<html><h1 style='display: flex; justify-content: center;'>{socket.LocalEndPoint} {PORT}</h1></html>");
                 //  Receive();
             }, state);
        }
        public void Send(string text)
        {
            byte[] data = Encoding.ASCII.GetBytes(text);
            socket.BeginSend(data, 0, data.Length, SocketFlags.None, (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = socket.EndSend(ar);
                Console.WriteLine("Room on port:{2} SEND: {0}, {1}", bytes, text,PORT);
            }, state);
        }
        private void Receive()
        {
            socket.BeginReceiveFrom(state.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv = (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = socket.EndReceiveFrom(ar, ref epFrom);
                socket.BeginReceiveFrom(so.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv, so);
                Console.WriteLine("Room on port:{3} RECV: {0}: {1}, {2}", 
                    epFrom.ToString(), bytes, Encoding.ASCII.GetString(so.buffer, 0, bytes), PORT);
            }, state);
        }
    }
}
