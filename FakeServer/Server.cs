using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FakeServer;

internal class Server
{
    private TcpListener _listener;
    private int _port;

    public Server(int port)
    {
        _port = port;
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public async Task Run()
    {
        _listener.Start();
        Console.WriteLine($"Listening on {_port}...");
        var id = 0;

        while (true)
        {
            id++;
            TcpClient? client = null;

            try
            {
                client = await _listener.AcceptTcpClientAsync();
                Console.WriteLine($"{id} Got Connection {(client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString()}");

                var task = HandleConnection(client, id);
            }
            catch(Exception e)
            {
                Console.WriteLine($"{id} Error: {e.Message}");

                if (client != null) client.Dispose();
            }
        }
    }

    private static async Task HandleConnection(TcpClient _client, int id)
    {
        using TcpClient client = _client;
        await using NetworkStream stream = client.GetStream();
        await SendMessage(id, stream, "Login: ");

        bool sentPasswordPrompt = false;
        string response = "";

        while (_client.GetState() == TcpState.Established)
        {
            System.Threading.Thread.Sleep(100);
            var buffer = new byte[1024];
            var received = await stream.ReadAsync(buffer, 0, buffer.Length);
            response += Encoding.UTF8.GetString(buffer, 0, received);

            if (response.Trim().Length > 0)
            {
                if (response.IndexOf(ControlChars.Lf) > -1 && !sentPasswordPrompt)
                {
                    Console.WriteLine($"{id} Response: {response.Trim()}");
                    sentPasswordPrompt = true;
                    await SendMessage(id, stream, "Password: ");
                    response = "";
                }
                else if (response.IndexOf(ControlChars.Lf) > -1 && sentPasswordPrompt)
                {
                    Console.WriteLine($"{id} Response: {response.Trim()}");
                    await SendMessage(id, stream, "Login Failed.");
                    System.Threading.Thread.Sleep(5000);
                    response = "";
                    break;
                }
            }
        }

        Console.WriteLine($"{id} Disconnected");
    }

    private static async Task SendMessage(int id, NetworkStream stream, string message)
    {
        var dateTimeBytes = Encoding.UTF8.GetBytes(message);
        await stream.WriteAsync(dateTimeBytes);
        Console.WriteLine($"{id} Sent message: {message} {DateTime.Now.ToString()}");
    }
}

public static class MyExtensionMethods
{
    public static TcpState GetState(this TcpClient tcpClient)
    {
        var foo = IPGlobalProperties.GetIPGlobalProperties()
          .GetActiveTcpConnections()
          .SingleOrDefault(x => x.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint)
                             && x.RemoteEndPoint.Equals(tcpClient.Client.RemoteEndPoint)
          );

        return foo != null ? foo.State : TcpState.Unknown;
    }
}