using System;
using System.Linq;
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

            _discord.MessageReceived       += OnMessageReceivedAsync;
            _discord.UserJoined            += OnUserJoined;
            _discord.ReactionAdded         += OnReactionAdded;
            _discord.ReactionRemoved       += OnReactionRemoved;
            _discord.UserVoiceStateUpdated += OnUserVoiceStateUpdated;
        }

        private static async Task OnUserVoiceStateUpdated(SocketUser user,
                                                          SocketVoiceState state_1,
                                                          SocketVoiceState state_2)
        {
            if (user.IsBot) return;
            
            var channel = BotActions.GetChannelFromConfig(Constants.VOICE_CHANNEL);

            if (state_1.VoiceChannel == null && state_2.VoiceChannel != null)
            {
                var log = new LogMessage(LogSeverity.Verbose, "Bot",
                                         $"{user.Username} joined voice channel {state_2.VoiceChannel.Name}. " +
                                         $"Users in channel: {state_2.VoiceChannel.Users.Count(u => !u.IsBot)}.");
                await LoggingService.Instance.LogAsync(log);

                await BotActions.SetChannelViewPermissionAsync(user, channel, false);
                await BotActions.SendDirectMessageAsync(user,
                                                        $"You now have access to {channel.Mention}. "                              +
                                                        "You can use it to discuss puzzles with the other users in the voice chat " +
                                                        "and it will be wiped when everyone leaves the voice chat.");
            }
            else if (state_1.VoiceChannel != null && state_2.VoiceChannel == null)
            {
                var log = new LogMessage(LogSeverity.Verbose, "Bot",
                                         $"{user.Username} left voice channel {state_1.VoiceChannel.Name}. " +
                                         $"Users in channel: {state_1.VoiceChannel.Users.Count(u => !u.IsBot)}.");
                await LoggingService.Instance.LogAsync(log);

                await BotActions.SetChannelViewPermissionAsync(user, channel, true);
                if (state_1.VoiceChannel.Users.Count(u => !u.IsBot) == 0)
                {
                    await BotActions.PurgeChannel(channel);
                }
            }
        }

        private async Task OnReactionAdded(Cacheable<IUserMessage, ulong> message,
                                           ISocketMessageChannel channel,
                                           SocketReaction reaction)
        {
            if (message.Id == ulong.Parse(_config[Constants.PREFERENCES_MESSAGE]) &&
                reaction.Emote.Name.Equals("👀"))
            {
                var user = reaction.User.Value;

                await BotActions.SetSolvedChannelsViewPermissionAsync(user, false);
                await BotActions.SendDirectMessageAsync(user, "Your solved puzzles are now being shown.");
            }
        }

        private async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> message,
                                             ISocketMessageChannel channel,
                                             SocketReaction reaction)
        {
            if (message.Id == ulong.Parse(_config[Constants.PREFERENCES_MESSAGE]) &&
                reaction.Emote.Name.Equals("👀"))
            {
                var user = reaction.User.Value;

                await BotActions.SetSolvedChannelsViewPermissionAsync(user, true);
                await BotActions.SendDirectMessageAsync(user, "Your solved puzzles are now being hidden.");
            }
        }

        private static async Task OnUserJoined(SocketGuildUser u)
        {
            if (u.IsBot)
            {
                return;
            }

            if (SqliteDataAccess.GetUserById(u.Id.ToString()) == null)
            {
                var user = new UserModel
                           {
                               Id         = u.Id.ToString(),
                               Name       = u.Username,
                               Solved     = 0,
                               Score      = 0,
                               HideSolved = true
                           };

                SqliteDataAccess.AddOrUpdateUser(user);
            }

            await BotActions.SendMessageToChannelAsync(Utility.GetGreetingMessage(u.Mention),
                                                       Constants.GREETING_CHANNEL);
            await BotActions.SendHelpMessageAsync(u);
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
                await LoggingService.Instance.LogAsync(new LogMessage(LogSeverity.Verbose, "Bot",
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
