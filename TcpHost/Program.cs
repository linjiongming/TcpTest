using NLog.Extensions.Logging;
using TcpHost;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders().AddNLog(NLogConfig.Default);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
