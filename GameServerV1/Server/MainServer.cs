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

namespace GameServerV1
{
    public class MainServer
    {
        private static int PORT = 9000;
           /*
            * port 9000 -> 10000;
            */           
        private static int DEFPACSIZE = 2048;

        const int TYPE_NonPack = 0;

        const int TYPE_SingUpS = 1;
        const int TYPE_SingUpR = 2;

        const int TYPE_LogInS = 3;
        const int TYPE_LogInR = 4;

        const int TYPE_CreateRoomS = 5;
        const int TYPE_CreateRoomR = 6;

        public const string BD_SOURCE_USERS =
            @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=..\..\Server\bd\Users.mdf;Integrated Security=True";
        private static TcpListener listener;
        private List<TcpClient> clients = new List<TcpClient>();
        private static BinaryFormatter binFormatter;
        public float IsTimer { internal get; set; } = 5; 
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
                        switch ((int)myObject["type"])
                        {
                            case TYPE_LogInS:
                                {
                                    var data = new Dictionary<string, object>();
                                    data["type"] = TYPE_LogInR;
                                    using SqlConnection connection = new SqlConnection(BD_SOURCE_USERS);
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
                                break;
                            case TYPE_SingUpS:
                                {
                                    var data = new Dictionary<string, object>();
                                    data["type"] = TYPE_SingUpR;
                                    using (SqlConnection connection = new SqlConnection(BD_SOURCE_USERS))
                                        try
                                        {
                                            connection.Open();
                                            string g = Guid.NewGuid().ToString();
                                            string sql = $"INSERT INTO users (name, email, password, uid, leng)"+
                                                $"VALUES  (" +
                                                $"'{(string)myObject["name"]}'," +
                                                $"'{(string)myObject["email"]}'," +
                                                $"'{(string)myObject["pass"]}', " +
                                                $"'{g}', " +
                                                $"'{(string)myObject["leng"]}')";
                                            Console.WriteLine($"BD New user: \n{(string)myObject["name"]}\n{g}");
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
                                    sendDictionary(stream, data);
                                }
                                break;
                            case TYPE_CreateRoomS:
                                {

                                }break;
                            case TYPE_NonPack:
                                {
                                    Console.WriteLine("Non Dictionary Data: " + myObject["data"].ToString().Substring(0,10));
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
                Console.WriteLine($"Error client {Client.Client.RemoteEndPoint} : {e.Message}");
                closeConnect(Client);
            }
        }
        void closeConnect(TcpClient? client)
        {
            Console.WriteLine($"Client disconnected: {client.Client.RemoteEndPoint}");
            clients.Remove(client);
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
                        data["type"] = TYPE_NonPack;
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
   
    }
}
