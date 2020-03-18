﻿using System;
using System.Collections;
using System.Security.Cryptography;
using System.Data.SQLite;
using System.IO;
using System.Data;
using System.Text;

namespace GameServerV1.Server
{
    public static class SQLDataManager
    {
        private static String dbFileName;
        private static SQLiteConnection m_dbConn;

        private static readonly string TAG="SQL";

        public static void InitSQL()
        {
            m_dbConn = new SQLiteConnection();
          
            dbFileName = "db.sqlite";


            if (!File.Exists(dbFileName))
                SQLiteConnection.CreateFile(dbFileName);
            try
            {
                m_dbConn = new SQLiteConnection("Data Source=" + dbFileName + ";Version=3;");
                m_dbConn.Open();

                Console.WriteLine(TAG + "Connect");
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(TAG + "Disconect");
                Console.WriteLine(TAG + "Error: " + ex.Message);
            }

        }

        public static User CreateNewUser(string name, string email, string passwoed)
        {
            string find = $"Select 1 from users where \"email\"='{email}'";
            try
            {
                SQLiteCommand Command = new SQLiteCommand(find, m_dbConn);
                Command.ExecuteNonQuery();
                using (SQLiteDataReader oReader = Command.ExecuteReader())
                {
                    while (oReader.Read())
                        if (oReader.Read())
                        {
                            oReader.Close();
                            Command.Dispose();
                            return GetUserData(email,passwoed);
                        }
                }

            }
            catch(Exception e)
            {
                Console.WriteLine(TAG + "Error: " + e.Message);
            }

            string id = Guid.NewGuid().ToString();
            string comand =
               "INSERT INTO users(id, name, email, pass, status) " +
               $"VALUES(\"{id}\", \"{name}\", \"{email}\"," +
               $" \"{GetMd5Hash(passwoed)}\", 0)";
            try
            {
                SQLiteCommand Command = new SQLiteCommand(comand, m_dbConn);
                Command.ExecuteNonQuery();
                
                Console.WriteLine(TAG + "Create User " +id);
                return new User(name, id);
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(TAG + "Disconect");
                Console.WriteLine(TAG + "Error: " + ex.Message);
            }
            return null;

        }
        public static User GetUserData(string email,string pass)
        {
            string find = $"Select 1 from users where \"email\"='{email}' AND \"pass\"='{GetMd5Hash(pass)}'";
            try
            {
                SQLiteCommand Command = new SQLiteCommand(find, m_dbConn);
                Command.ExecuteNonQuery();
                using (SQLiteDataReader oReader = Command.ExecuteReader())
                {
                    while (oReader.Read())
                        if (oReader.Read())
                        {
                            oReader.Close();
                            Command.Dispose();
                            
                        }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(TAG + "Error: " + e.Message);
            }
            return null;
        }
        static string GetMd5Hash(string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
    }
   
}
