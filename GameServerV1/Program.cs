using System;

namespace GameServerV1
{
    class Program
    {
        static void Main(string[] args)
        {
            MainServer server = new MainServer();
            

            ConsoleKeyInfo cki;
            do
            {
                cki = Console.ReadKey(); 
               
            } while (cki.Key != ConsoleKey.Escape);
        }
    }
}
