using System.Net.Sockets;
using System.Text;

namespace TcpClientTestApp;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var ipAddress = "127.0.0.1";
        var port = 13000;

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(ipAddress, port);
            Console.WriteLine($"Connected to server at {ipAddress}:{port}");

            await using var networkStream = client.GetStream();

            var receiveTask = Task.Run(() => ReceiveMessagesAsync(networkStream));

            while (true)
            {
                Console.WriteLine("Enter message to send (or type 'exit' to quit):");
                var message = Console.ReadLine();

                if (message?.ToLower() == "exit")
                {
                    break;
                }

                await SendMessageAsync(message, networkStream);
            }

            client.Close();
            Console.WriteLine("Disconnected from server.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task SendMessageAsync(string message, NetworkStream stream)
    {
        var padding = new string(' ', 8);

        var messageBytes = Encoding.UTF8.GetBytes(message);

        var totalLength = 4 + 8 + messageBytes.Length;

        var lengthStr = totalLength.ToString().PadLeft(4, '0');
        var lengthBytes = Encoding.ASCII.GetBytes(lengthStr);

        var paddingBytes = Encoding.ASCII.GetBytes(padding);

        await stream.WriteAsync(lengthBytes);
        await stream.WriteAsync(paddingBytes);
        await stream.WriteAsync(messageBytes);
    }

    private static async Task ReceiveMessagesAsync(NetworkStream stream)
    {
        var buffer = new byte[1024 * 1024];

        try
        {
            while (true)
            {
                var totalBytesRead = 0;
                while (totalBytesRead < 4)
                {
                    var bytesRead = await stream.ReadAsync(buffer, totalBytesRead, 4 - totalBytesRead);
                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Server closed the connection.");
                        return;
                    }
                    totalBytesRead += bytesRead;
                }

                var lengthStr = Encoding.ASCII.GetString(buffer, 0, 4);
                if (!int.TryParse(lengthStr, out var totalLength))
                {
                    Console.WriteLine("Failed to parse message length.");
                    return;
                }

                var remainingBytes = totalLength - 4;
                totalBytesRead = 0;
                while (totalBytesRead < remainingBytes)
                {
                    var bytesRead = await stream.ReadAsync(buffer.AsMemory(totalBytesRead, remainingBytes - totalBytesRead));
                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Server closed the connection.");
                        return;
                    }
                    totalBytesRead += bytesRead;
                }

                var message = Encoding.UTF8.GetString(buffer, 8, totalBytesRead - 8);

                Console.WriteLine($"Received from server: {message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error receiving data: {ex.Message}");
        }
    }
}