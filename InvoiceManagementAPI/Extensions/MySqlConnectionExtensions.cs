using MySqlConnector;
using System.Data;

namespace InvoiceManagementAPI.Extensions
{
    public static class MySqlConnectionExtensions
    {
        public static async Task SetTimezoneAsync(this MySqlConnection connection)
        {
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SET time_zone = '-05:00'";
            await cmd.ExecuteNonQueryAsync();
        }
    }
}