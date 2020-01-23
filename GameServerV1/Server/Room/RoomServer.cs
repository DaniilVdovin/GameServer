using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Data.SqlClient;
using System.IO;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace GameServerV1.Server
{
    public class Rules
    {
        public int
            BlueUser,
            RedUser,

            BlueScore,
            RedScore,

            MatchState,

            Alive,
            timer;
        public Rules(int Alive = 1, int BlueUser = 5, int RedUser = 5, int BlueScore = 0, int RedScore = 0, int MatchState = 0)
        {
            this.Alive = Alive;
            this.BlueUser = BlueUser;
            this.RedUser = RedUser;

            this.BlueScore = BlueScore;
            this.RedScore = RedScore;

            this.MatchState = MatchState;
        }
    }
    public class RoomServer
    {
        public Rules Rules          { get;  set; }
        public int PORT             { get; internal set; }
        public IPAddress ADDPRES    { get; internal set; }
        TcpListener socket;
        private const int bufSize = 8 * 1024;
        private State state = new State();
        private AsyncCallback recv = null;
        private static BinaryFormatter binFormatter;
        List<EndPoint> EndPoints = new List<EndPoint>();
        public List<User> Users { get; internal set; }    

        public class State
        {
            public byte[] buffer = new byte[bufSize];
        }
        public RoomServer(IPAddress address, int port)
        {
            this.PORT = port;
            this.ADDPRES = address;
            Users = new List<User>();
            Rules = new Rules();
            new Thread(init).Start();
           
        }
        public void init()
        {
            socket = new TcpListener(IPAddress.Any, PORT);
            socket.Start();
            try
            {

                while (true)
                {

                    using (TcpClient client = socket.AcceptTcpClient())
                    {
                        if (Rules.Alive == 1 && Users.Count < Rules.RedUser + Rules.BlueUser)
                        {
                            NetworkStream stream = client.GetStream();
                            Send(stream, FromDictionary(getRules()));
                        }
                    }

                    Thread.Sleep(300);
                }

            }
            catch (SocketException e)
            {
                Console.WriteLine($"      Room: {PORT} have e: {e.Message}\n{e.StackTrace}");
            }
        }
        Dictionary<string, object> getRules()
        {
            var rules = new Dictionary<string, object>();
            rules["type"] = (int)Types.TYPE_update_rules;

            rules["alive"] = Rules.Alive;

            rules["bs"] = Rules.BlueScore;
            rules["rs"] = Rules.RedScore;

            rules["bu"] = Rules.BlueUser;
            rules["ru"] = Rules.RedUser;

            rules["ms"] = Rules.MatchState;
            rules["timer"] = Rules.timer;
            return rules;
        }
        Dictionary<string, object> getAllUserData()
        {
            var users = new Dictionary<string, object>();
            users["type"] = (int)Types.TYPE_update_users;
            users["users"] = getListUser();
            return users;
        }
        byte[] FromDictionary(Dictionary<string, object> valuePairs)
        {
            byte[] temp;
            using (var mStream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(mStream, valuePairs);
                temp = mStream.ToArray();
                mStream.Close();
            }
            return temp;
        }
        Dictionary<string, object> ByteToDictionary(byte[] readingData, int PackSize)
        {
            using (var mStream = new MemoryStream())
                try
                {
                    if (PackSize > 0)
                    {
                        mStream.Write(readingData, 0, PackSize);
                        mStream.Position = 0;

                        return binFormatter.Deserialize(mStream) as Dictionary<string, object>;
                    }
                }
                catch (SerializationException e)
                {
                    if (PackSize > 0)
                    {
                        var data = new Dictionary<string, object>();
                        data["type"] = (int)Types.TYPE_NonPack;
                        try
                        {
                            data["data"] = Encoding.ASCII.GetString(readingData, 0, PackSize);
                            return data;
                        }
                        catch (Exception ex)
                        {
                            data["data"] = ex.Message;
                            return data;
                        }
                    }
                }
            return null;
        }
        public void Send(NetworkStream stream, byte[] data)
        {
            stream.Write(data, 0, data.Length);
            Console.WriteLine($"Room {PORT} send: {data.Length} byte");
            /*socket.BeginSend(data, 0, data.Length, SocketFlags.None, (ar) =>
            {
                int bytes = socket.EndSend(ar);
            }, state);*/
        }
        private void Receive(Socket socket)
        {
            socket.BeginReceive(state.buffer, 0, bufSize, SocketFlags.None, recv = (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = socket.EndReceive(ar);
                socket.BeginReceive(so.buffer, 0, bufSize, SocketFlags.None,recv, so);
                Console.WriteLine("Room on port:{2} RECV: {0}: {1},", 
                     bytes, Encoding.ASCII.GetString(so.buffer, 0, bytes), PORT);
            }, state);
        }
        public Dictionary<string, object>[] getListUser()
        {
            var list = new List<Dictionary<string, object>>();
            foreach(User u in Users)
            {
                var user = new Dictionary<string, object>();
                user["name"] = u.name;
                user["uid"] = u.uId;
                user["solderclass"] = u.SolderClass;
                user["group"] = u.group;
                user["health"] = u.Health;
                user["pos"] = new float[] { u.position.x,u.position.y,u.position.z };
                user["top"] = new float[] { u.rotation.x, u.rotation.y };
                list.Add(user);
            }
            return list.ToArray();
        }
    }
}
