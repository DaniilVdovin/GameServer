
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Server
{
    public enum Types
    {
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
    public class CtServer
    {
        public event EventHandler OnChangeUser;
        public event EventHandler OnNewRoom;
        public event EventHandler<string> OnError;
        public void ChangeUser()
        {
            OnChangeUser?.Invoke(this, EventArgs.Empty);
        }
        public void ChangeRoom()
        {
            OnNewRoom?.Invoke(this, EventArgs.Empty);
        }
        public void Error(string message = "def:null")
        {
            OnError?.Invoke(this, message);
        }

        const int BUFFFER_SIZE = 8 * 1024;
        TcpClient tcpClient;
        NetworkStream stream;
        public string host;
        public int port;
        public static BinaryFormatter binFormatter = new BinaryFormatter();

        public User CurrentUser;
        public Room CurrentRoom;

        private bool bListner = true;

        public CtServer(string host, int port)
        {
            
            this.host = host;
            this.port = port;
            
            tcpClient = new TcpClient(this.host, this.port);
            stream = tcpClient.GetStream();
            new Thread(Listner).Start();
        }
        void Listner()
        {
            try
            {
                byte[] rdata = new byte[BUFFFER_SIZE];
                while (bListner)
                {
                    int csize = 0;
                    do
                    {
                        csize = stream.Read(rdata, 0, rdata.Length);
                    } while (stream.DataAvailable);
                    if (csize > 0)
                    {
                        var obj = ByteJsonToDictionaryHard(rdata, csize);
                        if (obj != null)
                        {
                            Debug.Log($"Type: {obj["type"]} Size: {csize} byte");
                            switch ((Types)obj["type"])
                            {
                                case Types.TYPE_Get_User:
                                    {
                                        if (Convert.ToInt32(obj["req"]) == 1)
                                        {
                                            CurrentUser = new User((string)obj["name"], (string)obj["uid"]);
                                            Debug.Log($"User Info:\nName: {CurrentUser.name}\nuId: {CurrentUser.uId}");
                                            ChangeUser();
                                            if ((string)obj["error"] != "null") Debug.LogError($"Error :{obj["error"]}");
                                        }
                                    }
                                    break;
                                case Types.TYPE_CreateRoomR:
                                    {
                                        if (Convert.ToInt32(obj["req"]) == 1)
                                        {
                                            CurrentRoom = new Room(host, Convert.ToInt32(obj["port"]), CurrentUser);
                                            Debug.Log($"Room Info:\nport/name: {CurrentRoom.port}");
                                            ChangeRoom();
                                            bListner = false;
                                        }
                                    }
                                    break;
                            }
                           
                        }
                        else Error("data is null");
                    }
                    Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {
                Error(e.Message);
                Debug.LogError(e.Message + "\n" + e.StackTrace);
            }
        }
        void sendDictionary(Dictionary<string, object> valuePairs)
        {
            using (var mStream = new MemoryStream())
            {
                binFormatter.Serialize(mStream, valuePairs);
                mStream.Position = 0;
                stream.Write(mStream.ToArray(), 0, mStream.ToArray().Length);
                mStream.Close();
            }
        }
        public void sendString(string msg)
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
        public void SingUp(string name, string email, string password, string leng)
        {

            var data = new Dictionary<string, object>();
            data["type"] = (int)Types.TYPE_SingUp;
            data["name"] = name;
            data["email"] = email;
            data["pass"] = password;
            data["leng"] = leng;
            sendDictionaryByJson(data);


        }
        public void LogIn(string email, string password)
        {
            var data = new Dictionary<string, object>();
            data["type"] = (int)Types.TYPE_LogIn;
            data["email"] = email;
            data["pass"] = password;

            sendDictionaryByJson(data);
        }
        public void Logout()
        {
            if (CurrentUser != null)
            {
                var data = new Dictionary<string, object>();
                data["type"] = (int)Types.TYPE_LogOut;
                sendDictionaryByJson(data);
            }
        }
        public void CreateRoom()
        {
            if (CurrentUser is null) return;
            var data = new Dictionary<string, object>();
            data["type"] = (int)Types.TYPE_CreateRoomS;
            sendDictionaryByJson(data);
        }
        void sendDictionaryByJson(Dictionary<string, object> valuePairs)
        {
            //string json = JsonUtility.ToJson(valuePairs);
            string json = ConvertDictionaryToJsonHard(valuePairs);
            Debug.Log(json);
            byte[] data = Encoding.ASCII.GetBytes(json);
            stream.Write(data, 0, data.Length);
        }
        public static Dictionary<string, object> StringJsonToDictionary(byte[] data, int size)
        {
            var temp = new Dictionary<string, object>();
            try
            {
                string json = Encoding.UTF8.GetString(data, 0, size);
                Debug.Log($"I have: {json}");
                if (json.Length < 2) return null;
                temp = JsonUtility.FromJson(json, typeof(Dictionary<string, object>)) as Dictionary<string, object>;
                return temp;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in StrJsonToDict: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }
        public static Dictionary<string, object> ByteJsonToDictionaryHard(byte[] data, int size)
        {
            string json = Encoding.UTF8.GetString(data, 0, size);
            string[] jsonArray = json.Replace(@"{", "").Replace(@"}", "").Split(',');
            Debug.Log("json parametr size: " + jsonArray.Length);
            var temp = new Dictionary<string, object>();
            foreach (string obj in jsonArray)
                if (obj != null)
                {
                    int val;
                    object[] t = obj.Split(':');
                    t[0] = t[0].ToString().Replace(@"'", "");
                    t[1] = t[1].ToString().Replace(@"'", "");
                    //Debug.Log($"json parameter Key:{t[0]} Value:{(int.TryParse((string)t[1], out val) ? val: t[1])}");
                    temp.Add((string)t[0], int.TryParse((string)t[1],out val)?val:t[1]);
                }
             return temp;
            
        }
        public static string ConvertDictionaryToJsonHard(Dictionary<string, object> valuePairs)
        {
            string temp = @"{";

            foreach (KeyValuePair<string, object> obj in valuePairs)
            {
                if (obj.Value is float)
                    temp += $@"'{obj.Key}':{(float)obj.Value},";
                if (obj.Value is int)
                    temp += $@"'{obj.Key}':{(Int32)obj.Value},";
                if (obj.Value is string)
                    temp += $@"'{obj.Key}':'{(string)obj.Value}',";
            }

            return temp.Substring(0, temp.Length - 1) + @"}";

        }
    }
}