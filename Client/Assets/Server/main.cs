using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace Server
{
    public class main : MonoBehaviour
    {
        private string text = "";
        public Text textUI;
        public InputField em, pas, ipi;
        public string ip        = "server.minms.keenetic.pro:90",
                      Email     = "email@mail.com",
                      Name      = "thisname",
                      Password  = "parol",
                      lang      = "ru";
        CtServer server;

        public GameObject enPref;
        public GameObject player;

        public List<GameObject> enemy = new List<GameObject>();
        
        void Start()
        {
            em.text = Email;
            pas.text = Password;
            ipi.text = ip;
        }
        public void init()
        {
            server = new CtServer(ipi.text.Split(':')[0], int.Parse(ipi.text.Split(':')[1]));
           
            server.OnChangeUser += Server_OnChangeUser;
            server.OnError += Server_OnError;
            server.OnNewRoom += Server_OnNewRoom;

            StartCoroutine(StartPing(ipi.text));
            StartCoroutine(UdpateTransform());
        }

        private void Server_OnUpdate(object sender, User[] e)
        {
            foreach (User user in e)
            {
                if (!con(enemy, user))
                {
                    var en = Instantiate(enPref);
                    en.name = user.uId;
                    en.transform.position = user.position;
                    enemy.Add(en);
                }
            }
            text += "\n Users: " + e.Length;
        }
        bool con(List<GameObject>e,User user)
        {
            foreach (GameObject u in e)
                if (u.name == user.uId)
                {
                    u.transform.position = user.position;
                    return true;
                }
            return false;
        }
        IEnumerator StartPing(string ip)
        {
            WaitForSeconds f = new WaitForSeconds(0.05f);
            Ping p = new Ping(ip);
            int timeout = 300;
            while (p.isDone == false)
            {
                timeout--;
                if (timeout <= 0) break;
                yield return f;
            }
            text += $"\nPing to {ip} : {p.time}";
            text += "\nServer Connect";
        }
        private void Update()
        {
#if UNITY_ANDROID
            foreach (Touch touch in Input.touches)
            {
                if (touch.fingerId == 0)
                    switch (touch.phase)
                    {
                        case TouchPhase.Ended:
                            {
                                player.transform.position = Camera.main.ScreenToWorldPoint(touch.position) + Vector3.forward * 10;
                            }
                            break;
                    }
            }
#endif
#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
                    player.transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition) + Vector3.forward * 10;
#endif

        }
        private void FixedUpdate()
        {
            textUI.text = text;                
        }

        private void Server_OnNewRoom(object sender, EventArgs e)
        {
            text += $"\nRoom: {server.CurrentRoom.port}";
            server.CurrentRoom.OnUpdate += Server_OnUpdate;
        }
        private void Server_OnError(object sender,string e)
        {
            text += $"\nError: {e}";
        }
        private void Server_OnChangeUser(object sender, EventArgs e)
        { 
            text += $"\nUser :\nname:{server.CurrentUser.name}\nuid:{server.CurrentUser.uId}";
        }
        public void Sing()
        { 
            server.SingUp(name, em.text, pas.text, lang); 
        }
        public void LogIn()
        {
            server.LogIn(em.text, pas.text); 
        }
        public void create()
        {
            server.CreateRoom();
            if (server.CurrentRoom != null)
                text += $"\nCreate Room on port: {server.CurrentRoom.port}";
        }
        public void getRules()
        {
            server.CurrentRoom.GetNewRules();
        }
        public void getAllUser()
        {
            server.CurrentRoom.GetNewUsers();
        }
        private void OnDestroy()
        {
            server.CurrentRoom.Leave();
            server.Logout();
        }

        public IEnumerator UdpateTransform()
        {
            var WaitForSeconds = new WaitForSeconds(0.3f);
            while (true)
            {
                if (server != null && server.CurrentRoom != null)
                {
                    server.CurrentRoom.SendTransform(
                        new Single[] { 
                            player.transform.position.x,
                            player.transform.position.y,
                            player.transform.position.z 
                        },
                        new Single[] {
                            player.transform.position.x,
                            player.transform.position.y
                        });
                }
                yield return WaitForSeconds;
            }
        }
    }
}
