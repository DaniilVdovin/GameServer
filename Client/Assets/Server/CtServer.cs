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

public class RoomUser {
    int SolderClass;
    int health;
    User user;
    public Transform.Position Position;
    public Transform.Rotation Rotation;
    public struct Transform
    {
        public struct Position
        {
            public float x, y, z;
        }
        public struct Rotation
        {
           public float x, y;
        }
    }
    public UnityEngine.Transform getTransform(UnityEngine.Transform t)
    {
        UnityEngine.Transform data = t;
        data.position = new Vector3(Position.x,
                                    Position.y,
                                    Position.z);
        data.rotation =Quaternion.Euler( new Vector2(
                                        Rotation.x,
                                        Rotation.y));
        return data;
    }
    public User GetUser() => user;
    public int getSolderClass() => SolderClass;
}
public class Room { 
    public struct Rules {
        public int BlueScore,
            RedScore,
            MatchState,
            Alive,
            timer;
    }
    public Rules rules;
    List<RoomUser> users;
    public Room()
    {
        new Thread(Logic).Start();
    }
    void Logic()
    {
        new Thread(sTimer).Start(30);
    }
    void sTimer(object st)
    {   if ((int)st != 0)
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
    public void UpdateScore(int BS, int RS)
    {
        rules.BlueScore = BS;
        rules.RedScore = RS;
    }
}
public class User {
    private string name;
    private string uid;
    public User(string name, string uid)
    {
        this.name = name;
        this.uid = uid;
    }
    public string GetName() => name;
    public string GetUid() => uid;
}
public class CtServer
{
    public const int TYPE_SingUpS = 1;
    public const int TYPE_SingUpR = 2;

    public const int TYPE_LogInS = 3;
    public const int TYPE_LogInR = 4;

    TcpClient tcpClient;
    NetworkStream stream;

    BinaryFormatter binFormatter = new BinaryFormatter();
    public CtServer(string host, int port)
    {
        tcpClient = new TcpClient(host, port);
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
            catch (Exception e)
            {
                Debug.LogError("ByteToDictionary: " + e.Message);
            }
        return null;
    }
    public bool SingUp(string name, string email, string password, string leng)
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
            byte[] rdata = new byte[2048];
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
                return (int)obj["req"] == 1;
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
            byte[] rdata = new byte[2048];
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

}
