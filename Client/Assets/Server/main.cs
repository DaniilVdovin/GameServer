using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class main : MonoBehaviour
{
    
    void Start()
    {
        CtServer server = new CtServer("192.168.1.2", 8080);

        server.SingUp("thisname", "email@mail.com", "parol", "ru");


    }

}
