using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
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
        }
    }
    public class Room
    {

        public int port { get; internal set; }
        public Rules rules;

        public List<User> users { get; set; }
        public User GurrentYUser { get; set; }

        private const int bufSize = 8 * 1024;
        private AsyncCallback recv = null;
        private State state = new State();
        public User CurrentUser {get;set;}
        TcpClient tcpClient;
        NetworkStream stream;
        public class State
        {
            public byte[] buffer = new byte[bufSize];
        }
        public Room(string address,int port,User us)
        {
            CurrentUser = us;
            this.port = port;
            try
            {
                while (true)
                {
                    if (CurrentUser != null)
                    {
                        tcpClient = new TcpClient(address, this.port);
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
                        switch ((int)obj["type"])
                        {
                            case CtServer.TYPE_update_rules:
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
                            case CtServer.TYPE_update_users:
                                {
                                    UpdateUsersFromDictionaryArray((Dictionary<string, object>[])obj["users"]);
                                    Debug.Log("Set new user pack");
                                }
                                break;
                        }
                    }
                }
            }
        }
        void UpdateUsersFromDictionaryArray(Dictionary<string, object>[] value)
            {
            List<User> temp = new List<User>();
            foreach (Dictionary<string, object> data in value)
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
            
            users = temp;
            temp.Clear();
        }
        void Logic()
        {
            JoinRoom();
            Thread.Sleep(300);
            GetNewRules();
            GetNewUsers();
        }
        void JoinRoom()
        {
            var data = new Dictionary<string, object>();
            data["type"] = CtServer.TYPE_i_newUser;
            data["name"] = CurrentUser.name;
            data["uid"] = CurrentUser.uId;
            data["group"] = CurrentUser.group;
            data["health"] = CurrentUser.Health;
            data["solderclass"] = CurrentUser.SolderClass;

            sendPack(data);
        }
        void GetNewRules()
        {
            var data = new Dictionary<string, object>();
            data["type"] = CtServer.TYPE_i_wanna_info;

        }
        void GetNewUsers()
        {
            var data = new Dictionary<string, object>();
            data["type"] = CtServer.TYPE_i_wanna_users;

            sendPack(data);
        }
        void sendPack(Dictionary<string, object> valuePairs)
        {
            using (var mStream = new MemoryStream())
            {
                CtServer.binFormatter.Serialize(mStream, valuePairs);
                stream.Write(mStream.ToArray(), 0, mStream.ToArray().Length);
                mStream.Close();
            }
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
        private Dictionary<string, object> Udpate()
        {
            while(true)
            {
                State so = new State();
                int bytes = stream.Read(so.buffer, 0, so.buffer.Length);
                if (bytes != 0)
                {
                    Debug.Log($"Pack size: {bytes} byte");
                    return CtServer.ByteToDictionary(so.buffer, bytes);
                }
            }
        }
        
    }
}