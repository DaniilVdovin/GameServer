using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class main : MonoBehaviour
{
    public Text text;
    public InputField em, pas;
    public string Email = "email@mail.com",
                  Name = "thisname",
                  Password = "parol",
                  lang = "ru";
    CtServer server;
    User user = null;
    Room room;
    void Start()
    {
        server = new CtServer("192.168.1.2", 9000);
        text.text += "\nServer Connect";
        em.text = Email;
        pas.text = Password;
    }
    public void Sing()
    {
        if (user == null)
        {
            user = server.SingUp(name, em.text, pas.text, lang);
            if (user != null)
                text.text += $"\nUser :\nname:{user.name}\nuid:{user.uid}";
        }
    }
    public void LogIn()
    {
        if (user == null)
        {
            user = server.LogIn(em.text, pas.text);
            if (user != null)
                text.text += $"\nUser :\nname:{user.name}\nuid:{user.uid}";
        }
    }
    public void create()
    {
        room = server.CreateRoom();
        text.text += $"";
    }
    private void OnDestroy()
    {
        //Logout
    }
}
