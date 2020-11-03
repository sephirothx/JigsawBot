using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace JigsawBot
{
    public class BotActions
    {
        private readonly DiscordSocketClient _client;
        private readonly IConfigurationRoot  _config;
        private readonly IDataAccess         _data;
        private readonly LoggingService      _logger;

        private readonly string _prefix;

        public BotActions(DiscordSocketClient client,
                          IConfigurationRoot config,
                          IDataAccess data,
                          LoggingService logger)
        {
            _client = client;
            _config = config;
            _data   = data;
            _logger = logger;

            _prefix = _config["prefix"];
        }

        public async Task SendHelpMessageAsync(SocketGuildUser user)
        {
            var channel = await user.GetOrCreateDMChannelAsync();

            var msg = new EmbedBuilder()
                     .WithTitle("User Commands")
                     .WithColor(Color.Blue)
                     .AddField($"{_prefix}help or {_prefix}h",
                               "Shows a list of all possible commands.")
                     .AddField($"{_prefix}answer or {_prefix}a",
                               "Attempts to solve a puzzle. The answer should be marked in `||spoiler tags||`.\n" +
                               $"`{_prefix}answer #channel-name ||answer||`")
                     .AddField($"{_prefix}hint",
                               "Gives you a hint about a specific puzzle.\n" +
                               $"`{_prefix}hint #channel-name`")
                     .AddField($"{_prefix}stats or {_prefix}s",
                               "Shows some informations about your or another user's puzzle solving performances.\n" +
                               $"`{_prefix}stats` : displays your stats\n"                                            +
                               $"`{_prefix}stats @user` or `{_prefix}stats user` : displays user's stats")
                     .AddField($"{_prefix}puzzle or {_prefix}p",
                               "Displays informations regarding all puzzles or a specific puzzle.\n" +
                               $"`{_prefix}puzzle`\n"                                                 +
                               $"`{_prefix}puzzle #channel-name`");

            await channel.SendMessageAsync(embed: msg.Build());

            // ReSharper disable once PossibleNullReferenceException
            if (user.GuildPermissions.Administrator)
            {
                msg = new EmbedBuilder()
                     .WithTitle("Admin Commands")
                     .WithColor(Color.Blue)
                     .AddField($"{_prefix}setanswer or {_prefix}sa",
                               "Sets an answer for a puzzle.\n" +
                               $"`{_prefix}setanswer #channel-name answer`")
                     .AddField($"{_prefix}setcloseanswer or {_prefix}sca",
                               "Sets a close answer for a puzzle.\n" +
                               $"`{_prefix}setcloseanswer #channel-name answer`")
                     .AddField($"{_prefix}sethint or {_prefix}sh",
                               "Sets a hint for a puzzle.\n" +
                               $"`{_prefix}sethint #channel-name answer`")
                     .AddField($"{_prefix}getanswer or {_prefix}ga\n"       +
                               $"{_prefix}getcloseanswer or {_prefix}gca\n" +
                               $"{_prefix}gethint or {_prefix}gh",
                               "Gets the Answer/CloseAnswer/Hint for one or every puzzle.\n" +
                               $"`{_prefix}getanswer #channel-name`\n"                        +
                               $"`{_prefix}getanswer` or `{_prefix}getanswer all`")
                     .AddField($"{_prefix}setquote or {_prefix}sq",
                               "Adds a new quote of the chosen type to the database. Types:\n" +
                               "0 - CorrectAnswer\n"                                           +
                               "1 - WrongAnswer\n"                                             +
                               "2 - Greeting\n"                                                +
                               "3 - CloseAnswer\n"                                             +
                               "4 - AlreadySolved\n"                                           +
                               "`_NAME_` is used as a placeholder for user mention.\n"         +
                               $"`{_prefix}setquote 2 Hello _NAME_.`");

                await channel.SendMessageAsync(embed: msg.Build());
            }

            await channel.CloseAsync();
        }

        public async Task SendMessageToChannelAsync(string message, string channel)
        {
            var ch = GetChannelFromConfig(channel);
            await ch.SendMessageAsync(message);
        }

        public async Task SendDirectMessageAsync(IUser user, string message = null, Embed embed = null)
        {
            var channel = await user.GetOrCreateDMChannelAsync();

            await channel.SendMessageAsync(message, embed: embed);
            await channel.CloseAsync();
        }

        public SocketTextChannel GetChannelFromConfig(string channel)
        {
            var id = ulong.Parse(_config[channel]);
            var ch = _client.GetChannel(id) as SocketTextChannel ?? throw new Exception($"Channel {id} is not valid.");

            return ch;
        }

        public async Task AddPuzzleDataAsync(PuzzleDataModel puzzle)
        {
            if (_data.GetPuzzle(puzzle.PuzzleCode) == null)
            {
                var newPuzzle = new PuzzleModel
                {
                    Code   = puzzle.PuzzleCode,
                    Points = Constants.PUZZLE_STARTING_POINTS
                };
                _data.AddOrUpdatePuzzle(newPuzzle);

                await SendMessageToChannelAsync($"Added new puzzle: <#{puzzle.PuzzleCode}>",
                                                Constants.NOTIFICATIONS_CHANNEL);
            }

            _data.AddPuzzleData(puzzle);
        }

        public async Task ProcessCorrectAnswer(IUser user, string code)
        {
            string userId = user.Id.ToString();

            var puzzle = _data.GetPuzzle(code);
            var dbUser = _data.GetUserById(userId) ?? new UserModel(user.Id.ToString(), user.Username);

            if (dbUser.HideSolved)
            {
                await SetChannelViewPermissionAsync(user, code, true);
            }

            dbUser.Score += puzzle.Points;
            dbUser.Solved++;
            _data.AddOrUpdateUser(dbUser);

            if (puzzle.Points != 1)
            {
                var usersWhoSolvedPuzzle = _data.GetUsersWhoCompletedPuzzle(code);

                foreach (var userWhoSolvedPuzzle in usersWhoSolvedPuzzle)
                {
                    userWhoSolvedPuzzle.Score -= puzzle.Points;
                    _data.AddOrUpdateUser(userWhoSolvedPuzzle);
                }

                _data.UpdatePuzzlePoints(code);
            }

            var cpm = new CompletedPuzzleModel
            {
                UserId        = userId,
                PuzzleCode    = code,
                DateCompleted = DateTime.UtcNow
            };

            _data.NewCompletedPuzzle(cpm);
        }

        public async Task UpdateLeaderboard()
        {
            var channel = GetChannelFromConfig(Constants.LEADERBOARD_CHANNEL);
            var message = (await channel.GetPinnedMessagesAsync()).First() as IUserMessage ??
                          throw new Exception("No pinned message");

            var users = _data.GetUsers();
            var msg = new EmbedBuilder()
                     .WithTitle("Leaderboard")
                     .WithColor(Color.Blue);

            msg.Description = $"```{"Username",-25}{"Solved",6}{"Score",10}\n\n";
            foreach (var user in users.TakeWhile((u, i) => i < 20))
            {
                if (user.Solved == 0) break;
                msg.Description += $"{user.Name,-25}{user.Solved,6}{user.Score,10}\n";
            }

            msg.Description += "```";

            await message.ModifyAsync(m => m.Embed = msg.Build());
        }

        public async Task SetSolvedChannelsViewPermissionAsync(IUser user, bool hide)
        {
            _data.UpdateUserPreference_Hide(user.Id.ToString(), hide);
            var solvedPuzzles = _data.GetUsersCompletedPuzzles(user.Id.ToString());

            foreach (var puzzle in solvedPuzzles)
            {
                await SetChannelViewPermissionAsync(user, puzzle.PuzzleCode, hide);
            }

            await _logger.LogAsync(new LogMessage(LogSeverity.Verbose,
                                                  "Bot",
                                                  $"{(hide ? "Hidden" : "Shown")} " +
                                                  $"solved puzzles for {user.Username}."));
        }

        public async Task PurgeChannel(ITextChannel channel)
        {
            var messages = await channel.GetMessagesAsync().FlattenAsync();
            await channel.DeleteMessagesAsync(messages);
        }

        public async Task SetChannelViewPermissionAsync(IUser user, SocketGuildChannel channel, bool hide)
        {
            var value = hide ? PermValue.Deny : PermValue.Allow;
            await channel.AddPermissionOverwriteAsync(user, new OverwritePermissions(viewChannel: value));
        }

        public async Task SendUptimeMessageAsync(IMessageChannel channel)
        {
            var uptime = _logger.GetUptime();
            await channel.SendMessageAsync("Jigsaw has been up for " +
                                           (uptime.Days > 0
                                                ? $@"**{uptime:d\d\ h\h\ mm\m\ ss\s}**"
                                                : $@"**{uptime:h\h\ mm\m\ ss\s}**"));
        }

        #region Private

        private async Task SetChannelViewPermissionAsync(IUser user, string channelId, bool hide)
        {
            var channel = _client.Guilds.First().GetChannel(ulong.Parse(channelId));
            await SetChannelViewPermissionAsync(user, channel, hide);
        }

        #endregion
    }
}
