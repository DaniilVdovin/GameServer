using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace GameServerV1.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            // int port = int.Parse(Console.ReadLine());
            //  MainServer server = new MainServer(port);
            SQLDataManager.InitSQL();
            

             ConsoleKeyInfo cki;
             do
             {
                 cki = Console.ReadKey();
             } while (cki.Key != ConsoleKey.Escape);
        }
       
    }
}
