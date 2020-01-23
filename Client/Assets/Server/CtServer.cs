using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using UnityEngine;
namespace Server
{
    public class CtServer
    {
        public const int TYPE_SingUpS = 1;
        public const int TYPE_SingUpR = 2;

        public const int TYPE_LogInS = 3;
        public const int TYPE_LogInR = 4;

        public const int TYPE_LogOut = 10;

        public const int TYPE_CreateRoomS = 5;
        public const int TYPE_CreateRoomR = 6;

        public const int TYPE_update_rules = 7;
        public const int TYPE_update_users = 8;

        public const int TYPE_i_wanna_info = 9;
        public const int TYPE_i_wanna_users = 11;
        public const int TYPE_i_newUser = 12;




        const int BUFFFER_SIZE = 8 * 1024;
        TcpClient tcpClient;
        NetworkStream stream;
        public string host;
        public static BinaryFormatter binFormatter = new BinaryFormatter();
        public CtServer(string host, int port)
        {
            this.host = host;
            tcpClient = new TcpClient(this.host, port);
            stream = tcpClient.GetStream();
        }
        void sendDictionary(Dictionary<string, object> valuePairs)
        {
            using (var mStream = new MemoryStream())
            {
                binFormatter.Serialize(mStream, valuePairs);
                stream.Write(mStream.ToArray(), 0, mStream.ToArray().Length);
                mStream.Close();
            }
        }
        void sendString(string msg)
        {
            byte[] data = Encoding.ASCII.GetBytes(msg);
            stream.Write(data, 0, data.Length);
        }
        public static Dictionary<string, object> ByteToDictionary(byte[] readingData, int PackSize)
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
                catch (Exception e)
                {
                    Debug.LogError("ByteToDictionary: " + e.Message);
                }
            return null;
        }
        public User SingUp(string name, string email, string password, string leng)
        {

            var data = new Dictionary<string, object>();
            data["type"] = TYPE_SingUpS;
            data["name"] = name;
            data["email"] = email;
            data["pass"] = password;
            data["leng"] = leng;
            sendDictionary(data);
            while (true)
            {
                byte[] rdata = new byte[BUFFFER_SIZE];
                int csize = 0;
                do
                {
                    csize = stream.Read(rdata, 0, rdata.Length);

                } while (stream.DataAvailable);
                var obj = ByteToDictionary(rdata, csize);
                /*
                 * [Sing Up Pack]
                 * type  - 0xFB
                 * req  -  0 or 1 
                 * error - info
                 * 
                 */
                if ((int)obj["type"] == TYPE_SingUpR)
                {
                    if ((string)obj["error"] != null)
                        Debug.LogError($"Error :{obj["error"]}");

                    if ((int)obj["req"] == 1)
                    {
                        return new User((string)obj["name"], (string)obj["uid"]);
                    }
                }
            }

        }
        public User LogIn(string email, string password)
        {
            var data = new Dictionary<string, object>();
            data["type"] = TYPE_LogInS;
            data["email"] = email;
            data["pass"] = password;

            sendDictionary(data);
            while (true)
            {
                byte[] rdata = new byte[BUFFFER_SIZE];
                int csize = 0;
                do
                {
                    csize = stream.Read(rdata, 0, rdata.Length);
                } while (stream.DataAvailable);
                var obj = ByteToDictionary(rdata, csize);
                /*
                 * [Log in Pack]
                 * type  - 0xAF
                 * req  -  0 or 1 
                 * error - info
                 * 
                 * uid - id
                 * name - name
                 *
                 */
                if ((int)obj["type"] == TYPE_LogInR)
                {
                    if ((string)obj["error"] != null)
                        Debug.LogError($"Error :{obj["error"]}");
                    if ((int)obj["req"] == 1)
                        return new User((string)obj["name"], (string)obj["uid"]);
                }
            }
        }
        public void Logout(string uid)
        {
            var data = new Dictionary<string, object>();
            data["type"] = TYPE_LogOut;
            data["uid"] = uid;

            sendDictionary(data);

        }
        public Room CreateRoom(User us)
        {
            if (us.uId == null) return null; 
            var data = new Dictionary<string, object>();
            data["type"] = TYPE_CreateRoomS;
            data["uid"] = us.uId;

            sendDictionary(data);
            while (true)
            {
                byte[] rdata = new byte[BUFFFER_SIZE];
                int csize = 0;
                do
                {
                    csize = stream.Read(rdata, 0, rdata.Length);
                } while (stream.DataAvailable);
                var obj = ByteToDictionary(rdata, csize);
                /*
                 * [Log in Pack]
                 * type  - 0xAF
                 * req  -  0 or 1 
                 * error - info
                 * 
                 * uid - id
                 * name - name
                 *
                 */
                if ((int)obj["type"] == TYPE_CreateRoomR)
                {
                    if ((string)obj["error"] != null)Debug.LogError($"Error :{obj["error"]}");
                    if ((int)obj["req"] == 1)
                    {
                        Debug.Log($"Connect to: {host} : {(int)obj["port"]}");
                        return new Room(host, (int)obj["port"], us); ;
                    }
                    else return null;
                }      
            }
            return null;
        }
    }
}