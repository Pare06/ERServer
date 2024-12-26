using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Data.SqlClient;

namespace ERServer;

internal class Program
{
    private static async Task Main()
    {
        SqlConnection conn = new("Data Source=localhost;Database=ERCreator;Integrated Security=SSPI;");
        conn.Open();

        TcpListener listener = new(IPAddress.Any, 65534);
        listener.Start();

        while (true)
        {
            TcpClient handler = await listener.AcceptTcpClientAsync();

            NetworkStream stream = handler.GetStream();

            StreamReader reader = new(stream, Encoding.UTF8);
            string[] data = (await reader.ReadLineAsync())!.Split(' ');
                
            string deviceId = data[0];
            string softwareKey = data[1];

            SqlCommand selectKey = new("SELECT COUNT(*) FROM keys WHERE softwareKey = @key", conn);
            selectKey.Parameters.AddWithValue("@key", softwareKey);

            SqlCommand selectId = new("SELECT COUNT(*) FROM keys WHERE softwareKey = @key AND deviceId = @id", conn); 
            selectId.Parameters.AddWithValue("@key", softwareKey);
            selectId.Parameters.AddWithValue("@id", deviceId);

            bool keyExists = (int)selectKey.ExecuteScalar() > 0;
            bool hwidChecks = (int)selectId.ExecuteScalar() > 0;

            if (keyExists && !hwidChecks)
            {
                await stream.WriteAsync(new byte[] { 0 });
            }
            else
            {
                SqlCommand cmdInsert = new("INSERT INTO keys (softwareKey, deviceId) VALUES (@key, @id)", conn);
                cmdInsert.Parameters.AddWithValue("@key", softwareKey);
                cmdInsert.Parameters.AddWithValue("@id", deviceId);
                cmdInsert.ExecuteNonQuery();

                await stream.WriteAsync(new byte[] { 1 });
            }

            reader.Dispose();

            await stream.DisposeAsync();
            handler.Dispose();
        }
    }
}