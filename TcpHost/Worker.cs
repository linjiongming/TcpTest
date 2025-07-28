using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace TcpHost;

public class Worker : BackgroundService
{
    private readonly IConfiguration configuration;
    private readonly ILoggerFactory loggerFactory;
    private readonly TcpListener listener;
    private readonly ILogger logger;

    public Worker(
        IConfiguration configuration,
        ILoggerFactory loggerFactory)
    {
        this.configuration = configuration;
        this.loggerFactory = loggerFactory;
        string port = configuration["port"] ?? throw new ArgumentNullException(nameof(port));
        if (!int.TryParse(port, out var portNum) || portNum < 1 || portNum > 65535)
        {
            throw new ArgumentException("Invalid port number");
        }
        this.listener = new TcpListener(IPAddress.Any, portNum);
        this.logger = loggerFactory.CreateLogger(listener.LocalEndpoint!.ToString()!);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        listener.Start();
        logger.LogInformation("[Start]\r\n");
        while (!stoppingToken.IsCancellationRequested)
        {
            var client = await listener.AcceptTcpClientAsync(stoppingToken);
            logger.LogInformation("Accept {remote}", client.Client.RemoteEndPoint);
            _ = ReceiveAsync(client, stoppingToken);
            await Task.Delay(300, stoppingToken);
        }
        listener.Stop();
        logger.LogInformation("[Stop]\r\n");
    }

    private async Task ReceiveAsync(TcpClient client, CancellationToken cancellation)
    {
        var remote = GetHostAndPort(client.Client.RemoteEndPoint!);
        var logger1 = loggerFactory.CreateLogger(remote.Host);
        logger1.LogInformation("================================================================");
        try
        {
            var stopwatch = new Stopwatch();
            var buffer = new byte[4096];
            using var stream = client.GetStream();
            while (!cancellation.IsCancellationRequested)
            {
                logger1.LogInformation("{port}> Read begin", remote.Port);
                stopwatch.Restart();
                var count = await stream.ReadAsync(buffer, cancellation);
                stopwatch.Stop();
                if (count == 0)
                {
                    logger1.LogInformation("{port}> Read break", remote.Port);
                    break;
                }
                var dataString = string.Join(" ", new ArraySegment<byte>(buffer, 0, count).Select(b => b.ToString("X2")));
                logger1.LogInformation("{port}> {data}", remote.Port, dataString);
                logger1.LogInformation("{port}> Read end, {count} bytes took {ts}", remote.Port, count, stopwatch.Elapsed);
            }
            logger1.LogInformation("{port}> Disconnected!", remote.Port);
        }
        catch (OperationCanceledException) { /* ignore */ }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
        {
            logger1.LogInformation("{port}> Socket interrupted: {code}", remote.Port, ex.ErrorCode);
        }
        catch (Exception ex)
        {
            logger1.LogError(ex, "{port}> Error reading", remote.Port);
        }
        finally
        {
            logger1.LogInformation("================================================================\r\n");
        }
    }

    static (string Host, int Port) GetHostAndPort(EndPoint endpoint) => endpoint switch
    {
        IPEndPoint ip => (ip.Address.ToString(), ip.Port),
        DnsEndPoint dns => (dns.Host, dns.Port),
        _ => throw new InvalidCastException(),
    };
}
