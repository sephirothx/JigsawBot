using System;
using System.IO;
using System.Timers;
using Discord;
using Microsoft.Extensions.Configuration;

namespace JigsawBot
{
    public class BackupService
    {
        public string BackupDir { get; }
        public string DatabasePath { get; }

        public BackupService(IConfigurationRoot config)
        {
            BackupDir    = config["backup_dir"];
            DatabasePath = Path.Combine(AppContext.BaseDirectory, "ProjectDB.db");

            var timer = new Timer(8 * 60 * 60 * 1000);
            timer.Elapsed += OnTimerElapsed;

            timer.Start();
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (!Directory.Exists(BackupDir))
            {
                Directory.CreateDirectory(BackupDir);
            }

            string backupFile = Path.Combine(BackupDir, $"backup_{DateTime.UtcNow:yyyyMMdd-HHmmss}.db");

            try
            {
                File.Copy(DatabasePath, backupFile);
                LoggingService.Instance.LogAsync(new LogMessage(LogSeverity.Verbose,
                                                                "Backup",
                                                                $"{backupFile} saved successfully."));
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogAsync(new LogMessage(LogSeverity.Error,
                                                                "Backup",
                                                                "Failed to copy the Database.",
                                                                ex));
            }
        }
    }
}