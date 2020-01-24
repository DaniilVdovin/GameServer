using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Server
{
    public class main : MonoBehaviour
    {
        public Text text;
        public InputField em, pas,ipi;
        public string ip = "192.168.1.2",
                      Email = "email@mail.com",
                      Name = "thisname",
                      Password = "parol",
                      lang = "ru";
        CtServer server;
        User user = null;
        Room room;
        void Start()
        {
            server = new CtServer(ip, 9000);
            try
            {
                text.text += "\nServer Connect";
                em.text = Email;
                pas.text = Password;
            }
            catch(Exception e)
            {
                Debug.LogError(e.Message + "\n" + e.StackTrace);
                Application.Quit();
            }
            server.Logout("0");
            //StartCoroutine(test());
        }/*
        public IEnumerator test()
        {
            for(int i = 0; i < 1000; i++)
            {
                server = new CtServer("192.168.1.2", 9000);
                LogIn();
                create();
                yield return new WaitForSeconds(0.01f);
                user = null;
                room = null;
            }
        }
        public void Sing()
        {
           
            if (user == null)
            {
                user = server.SingUp(name, em.text, pas.text, lang);
                if (user != null)
                    text.text += $"\nUser :\nname:{user.name}\nuid:{user.uId}";
            }
        }
        public void LogIn()
        {
            if (user == null)
            {
                user = server.LogIn(em.text, pas.text);
                if (user != null)
                    text.text += $"\nUser :\nname:{user.name}\nuid:{user.uId}";
            }
        }
        public void create()
        {
            if (user != null)
            {
                room = server.CreateRoom(user);
                if (room != null)
                    text.text += $"\nCreate Room on port: {room.port}";  
            }
        }
        private void OnDestroy()
        {
            server.Logout(user.uId);
        }*/
    }
}
