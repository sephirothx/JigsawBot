using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace JigsawBot
{
    public class LoggingService
    {
        private string LogDirectory { get; }
        private string LogFile => Path.Combine(LogDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.txt");

        public static LoggingService Instance { get; private set; }

        public LoggingService(DiscordSocketClient discord, CommandService commands)
        {
            LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
            Instance     = this;

            discord.Log  += LogAsync;
            commands.Log += LogAsync;
        }

        public Task LogAsync(LogMessage msg)
        {
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }

            if (!File.Exists(LogFile))
            {
                File.Create(LogFile).Dispose();
            }

            string logText = $"{DateTime.UtcNow:HH:mm:ss} " +
                             $"[{msg.Severity}] "           +
                             $"{msg.Source}: "              +
                             $"{msg.Exception?.ToString() ?? msg.Message}";
            File.AppendAllText(LogFile, logText + "\n");

            return Console.Out.WriteLineAsync(logText);
        }
    }
}