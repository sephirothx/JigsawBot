using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace JigsawBot
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService      _commands;
        private readonly IConfigurationRoot  _config;
        private readonly IServiceProvider    _provider;

        public CommandHandler(DiscordSocketClient discord,
                              CommandService commands,
                              IConfigurationRoot config,
                              IServiceProvider provider)
        {
            _discord  = discord;
            _commands = commands;
            _config   = config;
            _provider = provider;

            _discord.MessageReceived += OnMessageReceivedAsync;
            _discord.UserJoined      += OnUserJoined;
        }

        private async Task OnUserJoined(SocketGuildUser u)
        {
            if (u.IsBot)
            {
                return;
            }

            var id      = ulong.Parse(_config["greeting_channel"]);
            var channel = _discord.GetChannel(id) as SocketTextChannel ?? throw new Exception("Greeting Channel is not valid.");
            await channel.SendMessageAsync($"Hello {u.Mention}. I want to play a game with you.");

            var user = new UserModel
                       {
                           Id     = u.Id.ToString(),
                           Name   = u.Username,
                           Solved = 0
                       };

            SqliteDataAccess.AddNewUser(user);
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            if (!(s is SocketUserMessage msg))
            {
                return;
            }

            if (msg.Author.Id == _discord.CurrentUser.Id)
            {
                return;
            }

            var context = new SocketCommandContext(_discord, msg);

            int argPos = 0;
            if (msg.HasStringPrefix(_config["prefix"], ref argPos) ||
                msg.HasMentionPrefix(_discord.CurrentUser, ref argPos))
            {
                await LoggingService.Instance.LogAsync(new LogMessage(LogSeverity.Debug, "Bot",
                                                                      $"{msg.Author.Id} - {msg.Author.Username}: {msg.Content}"));
                var result = await _commands.ExecuteAsync(context, argPos, _provider);

                if (!result.IsSuccess)
                {
                    await LoggingService.Instance.LogAsync(new LogMessage(LogSeverity.Error, "Bot",
                                                                          result.ToString()));
                }
            }
        }
    }
}
