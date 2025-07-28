using System.Net;
using System.Net.Sockets;

namespace TcpClient;

public class Worker : BackgroundService
{
    private readonly ILoggerFactory loggerFactory;
    private readonly IConfiguration configuration;
    private readonly EndPoint remote;
    private readonly Random random = new();

    public Worker(
        ILoggerFactory loggerFactory,
        IConfiguration configuration)
    {
        this.loggerFactory = loggerFactory;
        this.configuration = configuration;
        string host = configuration["host"] ?? "localhost";
        string port = configuration["port"] ?? throw new ArgumentNullException(nameof(port));
        if (!int.TryParse(port, out var portNum) || portNum < 1 || portNum > 65535)
        {
            throw new ArgumentException("Invalid port number");
        }
        if (IPAddress.TryParse(host, out var ip))
        {
            this.remote = new IPEndPoint(ip, portNum);
        }
        else
        {
            this.remote = new DnsEndPoint(host, portNum);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                await socket.ConnectAsync(remote);
                var local = GetHostAndPort(socket.LocalEndPoint!);
                var logger = loggerFactory.CreateLogger(local.Host);
                logger.LogInformation("================================================================");
                logger.LogInformation("{port}> Connect to {remote}", local.Port, remote);
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

                var buffer = new byte[random.Next(256, 512)];
                random.NextBytes(buffer);
                await socket.SendAsync(buffer, stoppingToken);
                var dataString = string.Join(" ", buffer.Select(b => b.ToString("X2")));
                logger.LogInformation("{port}> {len} bytes sent: {data}", local.Port, buffer.Length, dataString);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                await socket.SendAsync(buffer, stoppingToken);
                logger.LogInformation("{port}> [Retrans 1] {len} bytes sent: {data}", local.Port, buffer.Length, dataString);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                await socket.SendAsync(buffer, stoppingToken);
                logger.LogInformation("{port}> [Retrans 2] {len} bytes sent: {data}", local.Port, buffer.Length, dataString);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

                socket.Close();
                logger.LogInformation("{port}> Disconnected", local.Port);
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                logger.LogInformation("================================================================\r\n");
            }
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    static (string Host, int Port) GetHostAndPort(EndPoint endpoint) => endpoint switch
    {
        IPEndPoint ip => (ip.Address.ToString(), ip.Port),
        DnsEndPoint dns => (dns.Host, dns.Port),
        _ => throw new InvalidCastException(),
    };
}
