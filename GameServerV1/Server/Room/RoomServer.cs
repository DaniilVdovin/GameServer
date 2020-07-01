using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Globalization;
using System.Data.SQLite;

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
        public int TickServer = 100;
        public Rules Rules          { get;  set; }
        public int PORT             { get; internal set; }
        public IPAddress ADDPRES    { get; internal set; }
        TcpListener socket;
        private const int bufSize = 8 * 1024;
        private State state = new State();
        private AsyncCallback recv = null;
        private static BinaryFormatter binFormatter = new BinaryFormatter();
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
                    TcpClient client = socket.AcceptTcpClient();
                    client.NoDelay = true;
                    try
                    {
                        new Thread(listner).Start(client);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message + "\n" + e.StackTrace);
                    }
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine($"Room: {PORT} have e: {e.Message}\n{e.StackTrace}");
            }
        }
        void listner(object? client)
        {       
            User currentuser = null;
            NetworkStream stream = ((TcpClient)client).GetStream();
            while (true)
            {
                if (stream.DataAvailable)
                {
                    var ob = Udpate(stream);
                    foreach(var obj in ob)
                    if (obj != null)
                        switch ((Types)Convert.ToInt32(obj["type"]))
                        {
                            case Types.TYPE_i_newUser:
                                {
                                    if (Rules.Alive == 1 && Users.Count < Rules.RedUser + Rules.BlueUser)
                                    {
                                        Users.Add(currentuser = NewUser(stream, obj));
                                    }
                                    //нужен фидбк
                                }
                                break;
                            case Types.TYPE_i_wanna_info:
                                {
                                    Send(stream, ConvertDictionaryToByteHard(getRules()), currentuser.name);
                                }
                                break;
                            case Types.TYPE_i_wanna_users:
                                {
                                    Send(stream, ConvertDictionaryToByteHard(getAllUserData()), currentuser.name);
                                }
                                break;
                            case Types.ROOM_Leave:
                                {
                                    stream.Close();
                                    ((TcpClient)client).Close();
                                    Console.WriteLine($"Room {PORT} User:{currentuser.name} Leave");
                                    Users.Remove(currentuser);
                                    currentuser = null;
                                }
                                return;
                            case Types.ROOM_Send_Damage:
                                {
                                    User To = FindUserByUID((string)obj["uid"]);
                                    if (DetectDamageVector(new Vector2(currentuser.position.x,currentuser.position.y),
                                                           currentuser.rotation.y,
                                                           new Vector2(To.position.x, To.position.y)
                                                           ))
                                    {
                                         
                                        //setdamage((int)obj["damage"]);
                                        Console.WriteLine($"Room {PORT} Damage from {currentuser.name} to {To.name} {obj["damage"]}");
                                    }
                                }
                                break;
                            case Types.Room_Send_Transform: 
                                {
                                    if (obj.ContainsKey("px") && obj.ContainsKey("rx"))
                                    {
                                        //Console.WriteLine($"Room {PORT} {currentuser.name} {(string)obj["px"]}  {(string)obj["py"]}  {(string)obj["pz"]}");
                                        int ind = Users.IndexOf(currentuser);
                                        currentuser.setPosition(
                                            float.Parse(((string)obj["px"]).Replace(".", ",")),
                                            float.Parse(((string)obj["py"]).Replace(".", ",")),
                                            float.Parse(((string)obj["pz"]).Replace(".", ",")));
                                        currentuser.setRotation(
                                            float.Parse(((string)obj["rx"]).Replace(".", ",")),
                                            float.Parse(((string)obj["ry"]).Replace(".", ",")));
                                        Users[ind] = currentuser;
                                    }
                                }
                                break;
                        }
                    ob = null;
                }
                //Logic
                {
                    if(currentuser != null)
                        Send(stream, ConvertDictionaryToByteHard(getAllUserData()), currentuser.name);
                }
                Thread.Sleep(TickServer);
            }
                   
        }
        bool DetectDamageVector(Vector2 UserPosition, double UserRotation, Vector2 TargetPosition)
        {
            double angleRadian = (UserRotation) * Math.PI / 180;
            double x, y, k, m, n;
            double
                x1 = UserPosition.x,
                y1 = UserPosition.y,

                x2 = (x1 + 300 - UserPosition.x) * (float)Math.Cos(angleRadian) - (y1 + 250 - UserPosition.y) * (float)Math.Sin(angleRadian) + UserPosition.x,
                y2 = (x1 + 300 - UserPosition.x) * (float)Math.Sin(angleRadian) + (y1 + 250 - UserPosition.y) * (float)Math.Cos(angleRadian) + UserPosition.y,

                x3 = (x1 + 300 - UserPosition.x) * (float)Math.Cos(angleRadian) - (y1 - 250 - UserPosition.y) * (float)Math.Sin(angleRadian) + UserPosition.x,
                y3 = (x1 + 300 - UserPosition.x) * (float)Math.Sin(angleRadian) + (y1 - 250 - UserPosition.y) * (float)Math.Cos(angleRadian) + UserPosition.y;
            /*    
            float
            x1 = 200,
            y1 = 200,

            x2 = 800,
            y2 = 200+150,

            x3 = 800,
            y3 = 200-150;*/
            /*
            Polygon polygon = new Polygon();
            var points = new PointCollection();
            points.Add(new Point(x1, y1));
            points.Add(new Point(x2, y2));
            points.Add(new Point(x3, y3));
            polygon.Points = points;
            polygon.Fill = new SolidColorBrush(Colors.Blue);
            polygon.Stroke = new SolidColorBrush(Colors.Black);
            polygon.Opacity = 1;
            plane.Children.Add(polygon);*/
            //координаты вершин треугольника
            x = TargetPosition.x;
            y = TargetPosition.y; //координаты произвольной точки  

            k = (x1 - x) * (y2 - y1) - (x2 - x1) * (y1 - y);
            m = (x2 - x) * (y3 - y2) - (x3 - x2) * (y2 - y);
            n = (x3 - x) * (y1 - y3) - (x1 - x3) * (y3 - y);

            bool result = ((k >= 0 && m >= 0 && n >= 0) || (k <= 0 && m <= 0 && n <= 0) ? true : false);
            Console.WriteLine(result);
            /*Polygon pot = new Polygon();
            var pots = new PointCollection();
            pots.Add(new Point(x, y));
            pots.Add(new Point(x + 10, y + 5));
            pots.Add(new Point(x + 5, y + 10));
            pot.Points = pots;
            pot.Fill = new SolidColorBrush(result?Colors.Aqua:Colors.Red);
            pot.Stroke = new SolidColorBrush(Colors.Black);
            pot.Opacity = 10;
            plane.Children.Add(pot);*/
            return result;
        }

        User FindUserByUID(string uid,int group=-1)
        {
            foreach(User u in Users)
            {
                if(group == -1?true:u.group == group)
                    if (u.uId == uid) return u;
            }
            return null;
        }
        Dictionary<string, object>[] Udpate(NetworkStream stream)
        {
            while (true)
            {
                State so = new State();
                int bytes = stream.Read(so.buffer, 0, so.buffer.Length);
                if (bytes != 0)
                {
                    return StringJsonToDictionary(so.buffer, bytes);
                }
            }
        }
        User NewUser(NetworkStream? stream, Dictionary<string, object> obj)
        {
           User currentuser = new User((string)obj["name"], (string)obj["uid"]);

            if (Rules.BlueUser > Rules.RedUser)
                currentuser.group = 1;
            else
                currentuser.group = 0;

            if ((int)Convert.ToInt32(obj["group"]) == 0) Rules.BlueUser++;
            else if ((int)Convert.ToInt32(obj["group"]) == 1) Rules.RedUser++;


            currentuser.Health = (int)Convert.ToInt32(obj["health"]);
            currentuser.SolderClass = (int)Convert.ToInt32(obj["solderclass"]);

            Console.WriteLine($"Room {PORT} new User {currentuser.name}:{currentuser.uId}" +
                $"\nUser Wanna play in {obj["group"]} group but w`ll be play in {currentuser.group} now user in room {Users.Count}");
            return currentuser;
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
            users["users"] = (string)getListUser();
            //Console.WriteLine("user pack: " + users["users"]);
            return users;
        }
        Dictionary<string, object> getAllUserDataByGroup(int group)
        {
            var users = new Dictionary<string, object>();
            users["type"] = (int)Types.TYPE_roomsend_auto_user;
            users["users"] = (string)getListUser();
            //Console.WriteLine("user pack: " + users["users"]);
            return users;
        }
        byte[] FromDictionary(Dictionary<string, object> valuePairs)
        {
            byte[] temp;
            using (var mStream = new MemoryStream())
            {
                binFormatter.Serialize(mStream, valuePairs);
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
                    Console.WriteLine(e.Message + "\n" + e.StackTrace);
                }
            return null;
        }
        public void Send(NetworkStream stream, byte[] data,string nameofuser="unknow")
        {
            try
            {
                stream.Write(data, 0, data.Length);
                //Console.WriteLine($"Room {PORT} send: {data.Length} byte to {nameofuser} | users: {Users.Count}");
            }
            catch (IOException e)
            {
                
            }
        }
        private void Receive(Socket socket)
        {
            socket.BeginReceive(state.buffer, 0, bufSize, SocketFlags.None, recv = (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = socket.EndReceive(ar);
                socket.BeginReceive(so.buffer, 0, bufSize, SocketFlags.None,recv, so);
                /*Console.WriteLine("Room on port:{2} RECV: {0}: {1},", 
                     bytes, Encoding.ASCII.GetString(so.buffer, 0, bytes), PORT);*/
            }, state);
        }
        public string getListUser()
        {
            try
            {
                string list = "[";
                foreach (User u in Users)
                {
                    string user = $"" +
                        $"'name';'{u.name}'#" +
                        $"'uid';'{u.uId}'#" +
                        $"'solderclass';{u.SolderClass}#" +
                        $"'group';{u.group}#" +
                        $"'health';{u.Health}#" +
                        $"'px';{u.position.x.ToString().Replace(",", ".")}#" +
                        $"'py';{u.position.y.ToString().Replace(",", ".")}#" +
                        $"'pz';{u.position.z.ToString().Replace(",", ".")}#" +

                        $"'rx';{u.rotation.x.ToString().Replace(",", ".")}#" +
                        $"'ry';{u.rotation.y.ToString().Replace(",", ".")}*";
                    /* 
                     user["name"] = ;
                     user["uid"] = ;
                     user["solderclass"] = ;
                     user["group"] = u.group;
                     user["health"] = u.Health;
                     user["pos"] = new float[] { u.position.x,u.position.y,u.position.z };
                     user["rot"] = new float[] { u.rotation.x, u.rotation.y };
                     */
                    list += user;
                }
                return list[0..^1] + "]";
            }
            catch (Exception e)
            {
                Console.WriteLine($"Room {PORT} Error getListUser {e.Message}");
                return "null";
            }
        }
        public string getListByGroup(int group)
        {
            string list = "[";
            foreach (User u in Users)
            if(u.group == group)
            {
                string user = $"" +
                    $"'name';'{u.name}'#" +
                    $"'uid';'{u.uId}'#" +
                    $"'solderclass';{u.SolderClass}#" +
                    $"'group';{u.group}#" +
                    $"'health';{u.Health}#" +
                    $"'px';{u.position.x}#" +
                    $"'py';{u.position.y}#" +
                    $"'pz';{u.position.z}#" +

                    $"'rx';{u.rotation.x}#" +
                    $"'ry';{u.rotation.y}*";
                list += user;
            }
            return list[0..^1] + "]";
        }
        Dictionary<string, object>[] StringJsonToDictionary(byte[] data, int size)
        {
            List<Dictionary<string, object>> temp = new List<Dictionary<string, object>>();
            try
            {
                string jsonall = Encoding.UTF8.GetString(data, 0, size);
                foreach (string json in jsonall.Split("}{"))
                {
                    var js = $"{{ { json.Replace('{', ' ').Replace('}', ' ')} }}";
                    temp.Add(JsonConvert.DeserializeObject<Dictionary<string, object>>(js));
                }
                return temp.ToArray();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in StrJsonToDict: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }
        byte[] ConvertDictionaryToByteHard(Dictionary<string, object> valuePairs)
        {
            string temp = @"{";

            foreach (KeyValuePair<string, object> obj in valuePairs)
            {
                if (obj.Value is int)
                    temp += $@"'{obj.Key}':{(Int32)obj.Value},";
                if (obj.Value is string)
                    temp += $@"'{obj.Key}':'{(string)obj.Value}',";
            }
            //Console.WriteLine("Pack send: " + temp);
            return Encoding.UTF8.GetBytes(temp[0..^1] + "}");

        }
    }
}
