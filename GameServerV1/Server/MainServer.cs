using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;

namespace GameServerV1.Server
{
    public enum Types
    {
        TYPE_NonPack = 0,

        TYPE_SingUpS = 1,
        TYPE_SingUpR = 2,

        TYPE_LogInS = 3,
        TYPE_LogInR = 4,

        TYPE_LogOut = 10,

        TYPE_CreateRoomS = 5,
        TYPE_CreateRoomR = 6


    }
    
    public class MainServer
    {
        
        private static int PORT = 9000;
           /*
            * port 9000 -> 10000;
            */           
        private static int DEFPACSIZE = 8*1024;
      

        public const string BD_SOURCE_USERS =
            @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\stels\source\repos\DaniilVdovin\GameServer\GameServerV1\Server\bd\Users.mdf;Integrated Security=True";
        private static TcpListener listener;
        private List<TcpClient> clients = new List<TcpClient>();
        private List<RoomServer> rooms = new List<RoomServer>();
        private static BinaryFormatter binFormatter;
        public float IsTimer { internal get; set; } = 60; 
        public static bool _status { get; set; }
        public MainServer()
        {
            listener = new TcpListener(IPAddress.Any, PORT);
            binFormatter = new BinaryFormatter();
            listener.Start();
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
                        var myObject = ByteToDictionary(readingData, PackSize);
                        Console.WriteLine($"Type: {myObject["type"]}");
                        switch ((Types)myObject["type"])
                        {
                            case Types.TYPE_LogInS:
                                {
                                   user = LogIn(null,stream,myObject);
                                }
                                break;
                            case Types.TYPE_SingUpS:
                                {
                                   user = SingUp(stream, myObject);
                                }
                                break;
                            case Types.TYPE_LogOut:
                                setUserStatys(myObject["uid"].ToString(),0);
                                user = null;
                                closeConnect(Client);
                                return;
                            case Types.TYPE_CreateRoomS:
                                {
                                    var data = new Dictionary<string, object>();
                                    data["type"] = Types.TYPE_CreateRoomR;
                                    using (SqlConnection connection = new SqlConnection(BD_SOURCE_USERS))
                                        try
                                        {
                                            string oString =   $"Select * from users where uid='{myObject["uid"]}'";
                                            SqlCommand oCmd = new SqlCommand(oString, connection);
                                            connection.Open();
                                            using (SqlDataReader oReader = oCmd.ExecuteReader())
                                            {
                                                while (oReader.Read())
                                                {
                                                    
                                                }

                                                connection.Close();
                                            }
                                        }
                                        catch (SqlException e)
                                        {
                                            data["req"] = 0;
                                            data["error"] = e.Message;
                                            Console.WriteLine($"\nError bd: {e.Message}\n{e.StackTrace}\n");
                                        }
                                    sendDictionary(stream, data);
                                }
                                break;
                            case Types.TYPE_NonPack:
                                {
                                    Console.WriteLine("Non Dictionary Data: " + myObject["data"].ToString().Substring(0,10));
                                    byte[] data = Encoding.ASCII.GetBytes("HTTP/1.1 404\n\r\n\r<html><h1 style='display: flex; justify-content: center;'>404</h1></html>");
                                    stream.Write(data, 0, data.Length);
                                    closeConnect(Client);
                                }
                                return;
                        }
                        
                    }
                    Thread.Sleep(500);
                    timer -= 0.5f;
                    if (timer<=0)
                    {
                        Console.WriteLine($"Timeout: {IsTimer} sec lost");
                        closeConnect(Client);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error client {Client.Client.RemoteEndPoint} : {e.Message}\n{e.StackTrace}");
                closeConnect(Client);
            }
        }
        User SingUp(NetworkStream? stream, Dictionary<string, object>? myObject)
        {
            var data = new Dictionary<string, object>();
            data["type"] = (int)Types.TYPE_SingUpR;
            using (SqlConnection connection = new SqlConnection(BD_SOURCE_USERS))
                try
                {
                    string oString = $"Select * from users where email='{myObject["email"]}'";
                    SqlCommand oCmd = new SqlCommand(oString, connection);
                    connection.Open();
                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        if (oReader.Read())
                        {
                            connection.Close();
                            oReader.Close();

                            return LogIn(data,stream, myObject);
                        }else
                        {
                            oReader.Close();
                            oCmd.Dispose();

                        }
                    }
                    string g = Guid.NewGuid().ToString();
                    string sql = $"INSERT INTO users (name, email, password, uid, leng)" +
                        $"VALUES  (" +
                        $"'{(string)myObject["name"]}'," +
                        $"'{(string)myObject["email"]}'," +
                        $"'{(string)myObject["pass"]}', " +
                        $"'{g}', " +
                        $"'{(string)myObject["leng"]}')";
                    Console.WriteLine($"BD New user: {(string)myObject["name"]} :: {g}");
                    SqlCommand command = new SqlCommand(sql, connection);
                    command.ExecuteNonQuery();
                    command.Dispose();
                    data["uid"] = g;
                    data["req"] = 1;
                    data["error"] = null;
                    sendDictionary(stream, data);


                    connection.Close();
                    return new User((string)myObject["name"], g);
                }
                catch (SqlException e)
                {
                    data["req"] = 0;
                    data["error"] = e.Message;
                    Console.WriteLine($"\nError bd: {e.Message}\n{e.StackTrace}\n");
                    sendDictionary(stream, data);
                    return null;
                }

        }
        User LogIn(Dictionary<string, object> data, NetworkStream? stream, Dictionary<string, object>? myObject)
        {
            if (data == null)
            {
                data = new Dictionary<string, object>();
                data["type"] = (int)Types.TYPE_LogInR;
            }
            using (SqlConnection connection = new SqlConnection(BD_SOURCE_USERS))
                try
                {
                    string oString = $"Select * from users where email='{myObject["email"]}'";
                    SqlCommand oCmd = new SqlCommand(oString, connection);
                    connection.Open();
                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        if (oReader.Read())
                        {
                            Console.WriteLine($"BD Login user: {oReader["name"].ToString()} :: {oReader["uid"].ToString()}");
                            data["req"] = 1;
                            data["name"] = oReader["name"].ToString();
                            data["uid"] = oReader["uid"].ToString();
                            data["error"] = null;
                            setUserStatys(oReader["uid"].ToString(), 1);
                           
                            oReader.Close();
                            connection.Close();

                            sendDictionary(stream, data);
                            return new User(data["name"].ToString(), data["uid"].ToString());
                        }
                        else
                        {
                            data["req"] = 0;
                            data["error"] = "Not Found";
                            
                        }

                        oReader.Close();
                        connection.Close();
                        sendDictionary(stream, data);
                        return null;
                    }
                }
                catch (SqlException e)
                {
                    data["req"] = 0;
                    data["error"] = e.Message;
                    Console.WriteLine($"\nError bd: {e.Message}\n{e.StackTrace}\n");
                  
                }
            sendDictionary(stream, data);
            return null;
        }
        void setUserStatys(string uid,int statys)
        {
            using (SqlConnection connection = new SqlConnection(BD_SOURCE_USERS))
                try
                {
                    string oString = $"UPDATE users SET status={statys} where uid='{uid}'";
                    SqlCommand oCmd = new SqlCommand(oString, connection);
                    connection.Open();
                    oCmd.ExecuteNonQuery();   
                }
                catch (SqlException e)
                {
                    Console.WriteLine($"\nError bd: {e.Message}\n{e.StackTrace}\n");
                }
        }
        void closeConnect(TcpClient? client,User? user=null)
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
   
        void CreateNewRoom()
        {

        }
    }
}
