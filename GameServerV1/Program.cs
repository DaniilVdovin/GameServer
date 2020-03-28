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
           SQLDataManager.InitSQL();

           MainServer server = new MainServer(9000);
           ConsoleKeyInfo cki;
           do
           {
              cki = Console.ReadKey();
           } while (cki.Key != ConsoleKey.Escape);
        }
       
    }
}
