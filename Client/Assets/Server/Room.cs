using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using UnityEngine;
namespace Server
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
        public Rules(int Alive = 1, int BlueUser = 5, int RedUser = 5, int BlueScore = 0, int RedScore = 0, int MatchState = 0,int timer =0)
        {
            this.Alive = Alive;
            this.BlueUser = BlueUser;
            this.RedUser = RedUser;

            this.BlueScore = BlueScore;
            this.RedScore = RedScore;

            this.MatchState = MatchState;
            this.timer = timer;
        }
    }
    public class Room
    {
        public event EventHandler<User[]> OnUpdate;
        public void uUpdate()
        {
            OnUpdate?.Invoke(this, users.ToArray());
        }
        public int port { get; internal set; }
        public Rules rules;

        public List<User> users { get; set; }

        private const int bufSize = 8 * 1024;
        private State state = new State();
        public User CurrentUser { get; set; }
        TcpClient tcpClient;
        NetworkStream stream;
        private bool alive;

        public class State
        {
            public byte[] buffer = new byte[bufSize];
        }
        public Room(string address, int port, User us)
        {
            alive = true;
            CurrentUser = us;
            this.port = port;
            try
            {
                while (true)
                {
                    if (CurrentUser != null)
                    {
                        tcpClient = new TcpClient(address, this.port);
                        tcpClient.NoDelay = true;
                        stream = tcpClient.GetStream();

                        new Thread(listner).Start();
                        new Thread(Logic).Start();
                        break;
                    }
                }
            }
            catch (SocketException e)
            {
                Debug.LogError(e.Message + "\n" + e.StackTrace);
            }
        }
        void listner()
        {
            while (true)
            {
                if (stream.DataAvailable)
                {
                    var obj = Udpate();
                    if (obj != null)
                    {
                        switch ((Types)Convert.ToInt32(obj["type"]))
                        {
                            case Types.TYPE_update_rules:
                                {
                                    rules = new Rules(
                                    (int)obj["alive"],
                                    (int)obj["bu"],
                                    (int)obj["ru"],
                                    (int)obj["bs"],
                                    (int)obj["rs"],
                                    (int)obj["ms"],
                                    (int)obj["timer"]);
                                    Debug.Log("Set new Rules");
                                }
                                break;
                            case Types.TYPE_update_users:
                                {
                                    UpdateUsersFromDictionaryArray((string)obj["users"]);
                                    Debug.Log("Set new user pack");
                                }
                                break;
                        }
                    }
                }
            }
        }
        void UpdateUsersFromDictionaryArray(string data)
        {
            if (data.Contains("#"))
            {
                List<User> temp = new List<User>();
            /*foreach (Dictionary<string, object> data in value)
            {
                User user = new User((string)data["name"], (string)data["uid"]);
                user.group = (int)data["group"];
                user.Health = (int)data["health"];
                user.SolderClass = (int)data["solderclass"];
                var pos = (float[])data["pos"];
                user.position = new Vector3(pos[0], pos[1], pos[2]);
                var rot = (float[])data["rot"];
                user.rotation = new Vector2(rot[0], rot[1]);
                temp.Add(user);
            }
            */
                var t = data.Replace("'[", "").Replace("]'", "");
                string[] d;
                if (t.Contains("*")) d = t.Split('*');
                else d = new string[] { t };

                Debug.Log($"User Pack: {d[0]}");
                foreach (object us in d)
                    if (us != null)
                    {
                        var tl = us.ToString().Split('#');
                        if (tl != null)
                        {
                            User user = new User((string)tl[0].Split(':')[1], (string)tl[1].Split(':')[1]);
                            user.SolderClass = int.Parse(tl[2].Split(':')[1]);
                            user.group = int.Parse(tl[3].Split(':')[1]);
                            user.Health = int.Parse(tl[4].Split(':')[1]);
                            user.position = new Vector3(
                                int.Parse(tl[5].Split(':')[1]),
                                int.Parse(tl[6].Split(':')[1]),
                                int.Parse(tl[7].Split(':')[1]));
                            user.rotation = new Vector2(
                                int.Parse(tl[8].Split(':')[1]),
                                int.Parse(tl[9].Split(':')[1]));
                            temp.Add(user);
                        }
                    }
                users = temp;
                temp.Clear();
                uUpdate();
            }
        }
        void Logic()
        {
            JoinRoom();
        }
        public void SendTransform(Single[] position, Single[] rotation)
        {
            if (alive)
            {
                var data = new Dictionary<string, object>();
                data["type"] = (int)Types.Room_Send_Transform;
                //position
                data["px"] = (position[0].ToString("0.0#")).ToString(CultureInfo.InvariantCulture).Replace(",",".");
                data["py"] = (position[1].ToString("0.0#")).ToString(CultureInfo.InvariantCulture).Replace(",", ".");
                data["pz"] = (position[2].ToString("0.0#")).ToString(CultureInfo.InvariantCulture).Replace(",", ".");
                //rotation
                data["rx"] = (rotation[0].ToString("0.0#")).ToString(CultureInfo.InvariantCulture).Replace(",", ".");
                data["ry"] = (rotation[1].ToString("0.0#")).ToString(CultureInfo.InvariantCulture).Replace(",", ".");
                //.ToString(CultureInfo.InvariantCulture)
                Udpate();
                sendPack(data);
            }
        }
        private void JoinRoom()
        {
            var data = new Dictionary<string, object>
            {
                ["type"] = (int)Types.TYPE_i_newUser,
                ["name"] = CurrentUser.name,
                ["uid"] = CurrentUser.uId,
                ["group"] = CurrentUser.group,
                ["health"] = CurrentUser.Health,
                ["solderclass"] = CurrentUser.SolderClass
            };

            sendPack(data);
        }
        public void GetNewRules()
        {
            var data = new Dictionary<string, object>();
            data["type"] = (int)Types.TYPE_i_wanna_info;
            sendPack(data);
        }
        public void GetNewUsers()
        {
            var data = new Dictionary<string, object>();
            data["type"] = (int)Types.TYPE_i_wanna_users;
            sendPack(data);
        }
        void sendPack(Dictionary<string, object> valuePairs)
        {
            //string json = JsonUtility.ToJson(valuePairs);
            string json = CtServer.ConvertDictionaryToJsonHard(valuePairs);
            Debug.Log(json);
            byte[] data = Encoding.UTF8.GetBytes(json);
            stream.Write(data, 0, data.Length);
        }
        void sTimer(object st)
        {
            if ((int)st != 0)
                rules.timer = (int)st;
            do
            {
                rules.timer--;
                Thread.Sleep(1000);
            } while (rules.timer > 0);
        }
        void CloseRoom()
        {

        } 
        public void Leave()
        {
            alive = false;
            var data = new Dictionary<string, object>();
            data["type"] = (int)Types.ROOM_Leave;
            sendPack(data);
        }
        private Dictionary<string, object> Udpate()
        {
            while(true)
            {
                State so = new State();
                int bytes = stream.Read(so.buffer, 0, so.buffer.Length);
                if (bytes != 0)
                {
                    Debug.Log($"Pack size: {bytes} byte");
                    return CtServer.ByteJsonToDictionaryHard(so.buffer, bytes);
                }
            }
        }
        
    }
}