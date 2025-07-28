using NLog.Extensions.Logging;
using System.Net;
using TcpClient;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders().AddNLog(NLogConfig.Default);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
