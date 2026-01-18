using System.Data.SQLite;

namespace AutoWeighingAndPrinting.Data
{
    public static class SqlLiteHelper
    {

        public static object ExecuteScalar(string conString, string commandText, SQLiteParameter[] aryParams)
        {
            object result;
            using (var connection = new SQLiteConnection(conString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(commandText, connection))
                {
                    command.Parameters.AddRange(aryParams);
                    result = command.ExecuteScalar();
                }
            }
            return result;
        }


        public static void ExecuteNonQuery(string conString, string commandText, SQLiteParameter[] aryParams)
        {
            using (var connection = new SQLiteConnection(conString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(commandText, connection))
                {
                    command.Parameters.AddRange(aryParams);
                    command.ExecuteScalar();
                }
            }
        }
    }
}

