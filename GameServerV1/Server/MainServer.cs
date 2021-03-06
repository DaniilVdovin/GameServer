﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Npgsql;

namespace GameServerV1.Server
{
    public enum Types
    {
        test = -1,
        TYPE_NonPack = 0,

        TYPE_SingUp = 1,
        TYPE_LogIn = 3,

        TYPE_Get_User = 4,


        TYPE_LogOut = 10,

        TYPE_CreateRoomS = 5,
        TYPE_CreateRoomR = 6,

        TYPE_update_rules = 7,
        TYPE_update_users = 8,

        TYPE_i_wanna_info = 9,
        TYPE_i_wanna_users = 11,
        TYPE_i_newUser = 12,

        TYPE_roomsend_auto_user = 13,

        ROOM_Leave = 14,
        ROOM_Send_Damage = 15,
        Room_Send_Transform = 16,

        ADMIN = 1000
    }

    public class MainServer
    {

        private static int PORT = 9000;
        /*
         * port 9000 -> 10000;
         */
        private static int portroomnow = PORT;
        private static int DEFPACSIZE = 8 * 1024;

        private static TcpListener listener;
        private List<TcpClient> clients = new List<TcpClient>();
        public static List<RoomServer> rooms = new List<RoomServer>();
        private static BinaryFormatter binFormatter = new BinaryFormatter();
        public float IsTimer { internal get; set; } = 60;
        public static bool _status { get; set; }
        public MainServer(int port)
        {
            try
            {
                PORT = port;
                listener = new TcpListener(IPAddress.Any, PORT);
                listener.Start();
                Console.Title = "WGServer";
                Console.WriteLine("Listening...");
                _status = true;
                while (_status)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    try
                    {
                        if (client != null)
                        {
                            Thread temp = new Thread(Channel);
                            temp.Start(client);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: " + e.Message);
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }
        ~MainServer()
        {
            // Если "слушатель" был создан
            if (listener != null)
            {
                // Остановим его
                listener.Stop();
            }
        }
        private void Channel(object obj)
        {

            TcpClient Client = (TcpClient)obj;
            User user = null;
            clients.Add(Client);
            NetworkStream stream = Client.GetStream();
            Client.SendBufferSize = DEFPACSIZE;
            Client.ReceiveBufferSize = DEFPACSIZE;
            Console.WriteLine($"\n\nnew client: {Client.Client.RemoteEndPoint.ToString()} | Count: {clients.Count}");
            try
            {
                float timer = IsTimer;
                while (true)
                {
                    Byte[] readingData = new Byte[DEFPACSIZE];
                    int PackSize = 0;
                    if (stream.DataAvailable)
                    {
                        timer = IsTimer;
                        do
                        {
                            PackSize = stream.Read(readingData, 0, readingData.Length);
                        }
                        while (stream.DataAvailable);
                    }
                    if (PackSize > 0)
                    {

                        //Dictionary<string, object> myObject = ByteToDictionary(readingData, PackSize); 
                        Dictionary<string, object> myObject = StringJsonToDictionary(readingData, PackSize);

                        Console.WriteLine($"Type: {myObject["type"]}");
                        switch ((Types)Convert.ToInt32(myObject["type"]))
                        {
                            case Types.test:
                                {
                                    if (!SQLDataManager.ValidationByMAC((string)myObject["mac"]))
                                        closeConnect(Client);
                                }
                                break;
                            case Types.TYPE_LogIn:
                                {
                                    user = LogIn(stream, myObject);
                                    if (user is null)
                                    {
                                        closeConnect(Client);
                                        return;
                                    }
                                }
                                break;
                            case Types.TYPE_SingUp:
                                {
                                    user = SingUp(stream, myObject);
                                }
                                break;
                            case Types.TYPE_LogOut:
                                {
                                    setUserStatys(user.uId, 0);
                                    user = null;
                                    closeConnect(Client);
                                }
                                return;
                            case Types.TYPE_CreateRoomS:
                                {
                                    CreateNewRoom(stream, IPAddress.Any);
                                }
                                break;
                            case Types.TYPE_NonPack:
                                {
                                    string t = myObject["data"].ToString();
                                    Console.WriteLine("Non Dictionary Data: " + t.Substring(0, 11));
                                    
                                    byte[] data = Encoding.ASCII.GetBytes("HTTP/1.1 404\n\r\n\r" + File.ReadAllText("Server/WebUI/NotFound.html")
                                        .Replace("{users}","" + (clients.Count-1))
                                        .Replace("{rooms}", "" + rooms.Count)
                                        .Replace("{color}", "red")
                                        .Replace("{status}", "User with this MACAddress can't play now")
                                        );
                                    if (t.Substring(0, 20).Contains("?mac="))
                                    {
                                        string mac = t.Substring(t.IndexOf("=")+1, t.IndexOf('H')-11);
                                        bool val = SQLDataManager.ValidationByMAC(mac);
                                        if (val)
                                        data = Encoding.ASCII.GetBytes("HTTP/1.1 404\n\r\n\r" + File.ReadAllText("Server/WebUI/NotFound.html")
                                                                                  .Replace("{users}", "" + (clients.Count - 1))
                                                                                  .Replace("{rooms}", "" + rooms.Count)
                                                                                  .Replace("{color}", "green")
                                                                                  .Replace("{status}", "User with this MACAddress can play now")
                                                                                  );
                                        Console.WriteLine($"Now check MAC: {mac} answer = {val}");
                                    }
                                      
                                    stream.Write(data, 0, data.Length);
                                    closeConnect(Client);
                                }
                                return;
                            case Types.ADMIN:
                                {
                                    switch (myObject["cmd"])
                                    {
                                        case "getRooms":
                                            {
                                                var d = new Dictionary<string, object>();
                                                rooms.ForEach(room =>
                                                {
                                                    d.Add("" + room.PORT, room.Rules.MatchState);
                                                });
                                                sendDictionaryByJson(stream, d);
                                            }
                                            break;
                                    }
                                }
                                break;
                        }

                    }
                    Thread.Sleep(300);
                    /*timer -= 0.3f;
                    if (timer <= 0)
                    {
                        Console.WriteLine($"Timeout: {IsTimer} sec lost");
                        closeConnect(Client);
                        return;
                    }*/
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error client: {e.Message}\n{e.StackTrace}");
                closeConnect(Client);
            }
        }
        User SingUp(NetworkStream? stream, Dictionary<string, object>? myObject)
        {
            var data = new Dictionary<string, object>();
            data["type"] = (int)Types.TYPE_Get_User;
            User user = SQLDataManager.CreateNewUser((string)myObject["name"], (string)myObject["email"], (string)myObject["pass"]);

            data["name"] = (string)myObject["name"];
            data["uid"] = user.uId;
            data["req"] = 1;
            data["error"] = "null";

            Console.WriteLine($"BD New user: {(string)myObject["name"]} :: {user.uId}");
            sendDictionaryByJson(stream, data);
            return user;
        }
        User LogIn(NetworkStream? stream, Dictionary<string, object>? myObject)
        {
            var data = new Dictionary<string, object>();
            data["type"] = (int)Types.TYPE_Get_User;
            User user = SQLDataManager.GetUserData((string)myObject["email"], (string)myObject["pass"]);
            if (user != null)
            {
                data["name"] = user.name;
                data["uid"] = user.uId;
                data["req"] = 1;
                data["error"] = "null";
                Console.WriteLine($"BD login user: {user.name} :: {user.uId}");
                sendDictionaryByJson(stream, data);
            }
            return user;
        }
        void setUserStatys(string uid, int statys)
        {
            SQLDataManager.UdpateStatus(uid, statys);
        }
        void closeConnect(TcpClient? client, User? user = null)
        {
            Console.WriteLine($"Client disconnected: {client.Client.RemoteEndPoint}");
            clients.Remove(client);

            if (user != null)
                setUserStatys(user.uId, 0);

            client.Close();
            client.Dispose();
            Console.WriteLine($"Client count: {clients.Count}\n");
        }
        void sendDictionary(NetworkStream stream, Dictionary<string, object> valuePairs)
        {
            using (var mStream = new MemoryStream())
            {
                binFormatter.Serialize(mStream, valuePairs);
                stream.Write(mStream.ToArray(), 0, mStream.ToArray().Length);
                mStream.Close();
            }
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
                        data["type"] = "TYPE_NonPack";
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
        RoomServer FindActiveRoom()
        {
            foreach (RoomServer room in rooms)
            {
                Rules rules = room.Rules;
                if (rules.Alive == 1 & (rules.BlueUser + rules.RedUser) > room.Users.Count)
                    return room;

            }
            return null;
        }
        void CreateNewRoom(NetworkStream? stream, IPAddress address)
        {

            var data = new Dictionary<string, object>();
            data["type"] = (int)Types.TYPE_CreateRoomR;
            RoomServer roomServer = FindActiveRoom();
            if (roomServer == null)
            {
                try
                {
                    roomServer = new RoomServer(address, ++portroomnow)
                    {
                        Rules = new Rules(RedScore: 1)
                    };
                    Console.WriteLine($"Create new room{roomServer.PORT}");
                    rooms.Add(roomServer);
                    data["req"] = 1;
                    data["port"] = roomServer.PORT;
                    data["error"] = "null";
                }
                catch (Exception e)
                {
                    data["req"] = 0;
                    data["error"] = e.Message;
                    Console.WriteLine($"\nError bd: {e.Message}\n{e.StackTrace}\n");
                }
            }
            else
            {
                Console.WriteLine($"Create new.. no we have free room {roomServer.PORT}");
                data["req"] = 1;
                data["port"] = roomServer.PORT;
                data["error"] = null;
            }
            sendDictionaryByJson(stream, data);
        }
        Dictionary<string, object> StringJsonToDictionary(byte[] data, int size)
        {
            Dictionary<string, object> temp = new Dictionary<string, object>();
            string json = Encoding.UTF8.GetString(data, 0, size);
            try
            {
                Console.WriteLine($"I get: {size} byte");
                if (json.Contains("type"))
                    temp = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                else
                {
                    temp["type"] = (int)Types.TYPE_NonPack;
                    temp["data"] = json;
                }
                return temp;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in StrJsonToDict: {e.Message}");
                return null;
            }
        }
        void sendDictionaryByJson(NetworkStream stream, Dictionary<string, object> valuePairs)
        {
            string json = ConvertDictionaryToJsonHard(valuePairs);
            Console.WriteLine($"Send Json: {json}");
            byte[] data = Encoding.UTF8.GetBytes(json);
            stream.Write(data, 0, data.Length);
        }
        string ConvertDictionaryToJsonHard(Dictionary<string, object> valuePairs)
        {
            string temp = "{";

            foreach (KeyValuePair<string, object> obj in valuePairs)
            {
                if (obj.Value is int)
                    temp += $@"'{obj.Key}':{(Int32)obj.Value},";
                if (obj.Value is string)
                    temp += $@"'{obj.Key}':'{(string)obj.Value}',";
            }

            return temp[0..^1] + "}";

        }
    }
}
