using System;
using MySql.Data.MySqlClient;

namespace SystemUI
{
    public class DBConnection
    {
        private static string server = "localhost";
        private static string database = "musiclib_db";
        private static string username = "root";
        private static string password = "password";

        public static string ConnectionString =
            $"Server={server};Database={database};Uid={username};Pwd={password};";

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        public static bool TestConnection()
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
