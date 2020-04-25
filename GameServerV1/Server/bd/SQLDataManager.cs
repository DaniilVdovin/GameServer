using System;
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

        private static readonly string TAG="SQLManager";

        public static void InitSQL()
        {
            m_dbConn = new SQLiteConnection();
          
            dbFileName = "db.sqlite";

            try
            {
                m_dbConn = new SQLiteConnection("Data Source=" + dbFileName + ";Version=3;");
                m_dbConn.Open();

                Console.WriteLine(TAG + " Connect");
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(TAG + "Disconect");
                Console.WriteLine(TAG + "Error: " + ex.Message);
            }

        }

        public static User CreateNewUser(string name, string email, string passwoed)
        {
            string find = $"Select * from users where \"email\"='{email}'";
            try
            {
                SQLiteCommand Command = new SQLiteCommand(find, m_dbConn);
                Command.ExecuteNonQuery();
                using (SQLiteDataReader oReader = Command.ExecuteReader())
                {
                    if(oReader.HasRows)
                    while (oReader.Read())
                        {
                            UdpateLastLogin(oReader.GetValue(0) as string);
                            return new User(oReader.GetValue(0) as string, oReader.GetValue(3) as string);
                        }
                }

            }
            catch(Exception e)
            {
                Console.WriteLine(TAG + "Error: " + e.Message);
                Console.WriteLine(TAG + "Error: " + e.StackTrace);
            }

            string id = Guid.NewGuid().ToString();
            string comand =
               "INSERT INTO users(id, name, email, pass, status, regdate, lastlogin) " +
               $"VALUES(\"{id}\", \"{name}\", \"{email}\"," +
               $" \"{GetMd5Hash(passwoed)}\", 1, \"{DateTime.Now}\", \"{DateTime.Now}\")" +
               $"";
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

                Console.WriteLine(TAG + "Error: " + ex.StackTrace);
            }
            return null;

        }
        public static User GetUserData(string email,string pass)
        {
            string find = $"Select * from users where \"email\"='{email}' AND \"pass\"='{GetMd5Hash(pass)}'";
            User user = null;
            try
            {
                SQLiteCommand Command = new SQLiteCommand(find, m_dbConn);
                Command.ExecuteNonQuery();
                using (SQLiteDataReader oReader = Command.ExecuteReader())
                {
                    if (oReader.HasRows)
                    while (oReader.Read())
                    {   
                            user = new User(oReader.GetValue(0) as string, oReader.GetValue(3) as string);
                            if (user != null)
                            {
                                oReader.Close();
                                Command.Dispose();
                                break;
                            }
                        }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(TAG + "Error: " + e.Message);
                Console.WriteLine(TAG + "Error: " + e.StackTrace);
            }
            if(user != null)
                UdpateLastLogin(user.uId);
            return user;
        }
        public static void UdpateStatus(string uid, int statys)
        {
            string oString = $"UPDATE users SET \"status\"={statys} where \"id\"='{uid}'";
            try
            {
                SQLiteCommand Command = new SQLiteCommand(oString, m_dbConn);
                Command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine(TAG + "Error: " + e.Message);
                Console.WriteLine(TAG + "Error: " + e.StackTrace);
            }
        }
        public static void UdpateLastLogin(string uid)
        {
            string oString = $"UPDATE users SET \"lastlogin\"=\"{DateTime.Now}\" where \"id\"='{uid}'";
            try
            {
                SQLiteCommand Command = new SQLiteCommand(oString, m_dbConn);
                Command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine(TAG + "Error: " + e.Message);
                Console.WriteLine(TAG + "Error: " + e.StackTrace);
            }
        }
        static string GetMd5Hash(string input)
        {
            byte[] data = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }
    }
   
}
