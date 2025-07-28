using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;

public static class NLogConfig
{
    public static LoggingConfiguration Default
    {
        get
        {
            var config = new LoggingConfiguration();

            // 配置 ColoredConsole 目标
            var consoleTarget = new ColoredConsoleTarget
            {
                Name = "console",
                Layout = "[${time}] [${level:uppercase=true}] [${logger}] ${message}${onexception:${newline}${exception:format=tostring}}"
            };

            // 配置日志文件目标
            var logFileTarget = new FileTarget
            {
                Name = "logfile",
                FileName = "${basedir}/logs/${logger}-${shortdate}.all.log",
                Layout = "[${time}] [${level:uppercase=true}] [${logger}] ${message}",
                ArchiveAboveSize = 10485760,
                ArchiveFileName = "${basedir}/logs/${shortdate}.{#}.all.log",
                ArchiveSuffixFormat = "_{0:00}",
                MaxArchiveFiles = 100
            };

            // 配置错误文件目标
            var errorFileTarget = new FileTarget
            {
                Name = "errorfile",
                FileName = "${basedir}/logs/${logger}-${shortdate}.error.log",
                Layout = "[${time}] [${level:uppercase=true}] [${logger}] ${message}${onexception:${newline}${exception:format=tostring}}",
                ArchiveAboveSize = 10485760,
                ArchiveFileName = "${basedir}/logs/${shortdate}.{#}.error.log",
                ArchiveSuffixFormat = "_{0:00}",
                MaxArchiveFiles = 100
            };

            // 配置日志文件目标
            var journalTarget = new FileTarget
            {
                Name = "journal",
                FileName = "${basedir}/journal/${shortdate}/${logger}.txt",
                Layout = "[${time}] ${message}"
            };

            // 添加目标到配置
            config.AddTarget(consoleTarget);
            config.AddTarget(logFileTarget);
            config.AddTarget(errorFileTarget);
            config.AddTarget(journalTarget);

            // 配置规则
            // Microsoft.* 的规则
            var microsoftRule = new LoggingRule("Microsoft.*", NLog.LogLevel.Info, consoleTarget)
            {
                Final = true
            };
            config.LoggingRules.Add(microsoftRule);

            // 控制台输出规则
            var consoleRule = new LoggingRule("*", NLog.LogLevel.Debug, consoleTarget);
            config.LoggingRules.Add(consoleRule);

            // 日志文件规则
            var logFileRule = new LoggingRule("*", NLog.LogLevel.Debug, logFileTarget);
            config.LoggingRules.Add(logFileRule);

            // 错误文件规则
            var errorFileRule = new LoggingRule("*", NLog.LogLevel.Error, errorFileTarget);
            config.LoggingRules.Add(errorFileRule);

            // 追踪日志规则
            var journalRule = new LoggingRule("*", NLog.LogLevel.Trace, journalTarget);
            config.LoggingRules.Add(journalRule);

            return config;
        }
    }
}