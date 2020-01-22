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
        public string HOST;
        Socket socket;
        private const int bufSize = 8 * 1024;
        private State state = new State();
        private EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback recv = null;

        List<IPEndPoint> EndPoints = new List<IPEndPoint>();
        List<User> Users = new List<User>();

        public class State
        {
            public byte[] buffer = new byte[bufSize];
        }
        public RoomServer(string address, int port)
        {
            this.PORT = port;
            this.HOST = address;

            init();
        }
        void init()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.ReuseAddress, true);
            socket.Bind(new IPEndPoint(IPAddress.Parse(HOST), PORT));

            var data = new Dictionary<string, object>();
            data["type"] = Types.TYPE_LogInR;
            using SqlConnection connection = new SqlConnection(MainServer.BD_SOURCE_USERS);
            try
            {
                connection.Open();
                string sql = $"";

                SqlCommand command = new SqlCommand(sql, connection);
                command.ExecuteNonQuery();
                command.Dispose();

                data["req"] = 1;
                data["error"] = null;

            }
            catch (SqlException e)
            {
                data["req"] = 0;
                data["error"] = e.Message;
                Console.WriteLine($"\nError bd: {e.Message}\n{e.StackTrace}\n");
            }
        }
        public void Connect(string address,int port)
        {
            var end = IPEndPoint.Parse(address + ":" + port);
          //  Users.Add(new User());
            EndPoints.Add(end);
            socket.Connect(end);
            Receive();
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
