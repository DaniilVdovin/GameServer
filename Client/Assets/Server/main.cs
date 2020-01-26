using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Server
{
    public class main : MonoBehaviour
    {
        private string text = "";
        public Text textUI;
        public InputField em, pas, ipi;
        public string ip = "192.168.1.2",
                      Email = "email@mail.com",
                      Name = "thisname",
                      Password = "parol",
                      lang = "ru";
        CtServer server;
        void Start()
        {
            em.text = Email;
            pas.text = Password;
            ip = ipi.text;

        }
        public void init()
        {
            server = new CtServer(ip, 9000);
            server.OnChangeUser += Server_OnChangeUser;
            server.OnError += Server_OnError;
            server.OnNewRoom += Server_OnNewRoom;

            StartCoroutine(StartPing(ip));
            StartCoroutine(StartPing("64.233.162.94"));
            StartCoroutine(StartPing("87.250.250.242"));

            text += "\nServer Connect";
        }
        IEnumerator StartPing(string ip)
        {
            WaitForSeconds f = new WaitForSeconds(0.05f);
            Ping p = new Ping(ip);
            while (p.isDone == false)
            {
                yield return f;
            }
            text += $"\nPing to {ip} : {p.time}";
        }

        private void Server_OnPingDone(object sender, string e)
        {
            text += $"Ping: {e}";
        }

        private void FixedUpdate()
        {
            textUI.text = text; 
        }

        private void Server_OnNewRoom(object sender, EventArgs e)
        {
            text += $"\nRoom: {server.CurrentRoom.port}";
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
            server.Logout();
        }
    }
}
