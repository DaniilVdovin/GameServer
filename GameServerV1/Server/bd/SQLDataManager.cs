using System;
using System.Collections;
using System.Data.SQLite;
using System.IO;
using System.Data;

namespace GameServerV1.Server
{
    public static class SQLDataManager
    {
        private static String dbFileName;
        private static SQLiteConnection m_dbConn;
        private static SQLiteCommand m_sqlCmd;

        private static readonly string TAG="SQL";

        public static void InitSQL()
        {
            m_dbConn = new SQLiteConnection();
            m_sqlCmd = new SQLiteCommand();

            dbFileName = "db.sqlite";

            Console.WriteLine(TAG + "Connect");

            if (!File.Exists(dbFileName))
                SQLiteConnection.CreateFile(dbFileName);

            try
            {
                m_dbConn = new SQLiteConnection("Data Source=" + dbFileName + ";Version=3;");
                m_dbConn.Open();
                m_sqlCmd.Connection = m_dbConn;

                m_sqlCmd.CommandText = "CREATE TABLE IF NOT EXISTS Catalog (id INTEGER PRIMARY KEY AUTOINCREMENT, author TEXT, book TEXT)";
                m_sqlCmd.ExecuteNonQuery();

                Console.WriteLine(TAG + "Create Table");
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(TAG + "Disconect");
                Console.WriteLine(TAG + "Error: " + ex.Message);
            }
        }


        public static User GetUserData()
        {
            return null;
        }
    }
   
}
