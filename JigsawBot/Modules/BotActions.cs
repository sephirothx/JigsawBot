using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace JigsawBot
{
    public class BotActions
    {
        public static async Task SendHelpMessageAsync(SocketGuildUser user)
        {
            string prefix  = BotClient.Instance.Configuration["prefix"];
            var    channel = await user.GetOrCreateDMChannelAsync();

            var msg = new EmbedBuilder()
                     .WithTitle("User Commands")
                     .WithColor(Color.Blue)
                     .AddField($"{prefix}help or {prefix}h", "Shows a list of all possible commands.")
                     .AddField($"{prefix}answer or {prefix}a",
                               "Attempts to solve a puzzle. The answer can be marked in spoiler tags.\n" +
                               $"`{prefix}answer #channel-name answer`\n"                                +
                               $"`{prefix}answer #channel-name ||answer||`\n"                            +
                               $"`{prefix}answer ||#channel-name answer||`")
                     .AddField($"{prefix}hint",
                               "Gives you a hint about a specific puzzle.\n" +
                               $"`{prefix}hint #channel-name`")
                     .AddField($"{prefix}stats or {prefix}s",
                               "Shows some informations about your or another user's puzzle solving performances.\n" +
                               $"`{prefix}stats` : displays your stats\n"                                            +
                               $"`{prefix}stats @user` or `{prefix}stats user` : displays user's stats")
                     .AddField($"{prefix}puzzle or {prefix}p",
                               "Displays informations regarding all puzzles or a specific puzzle.\n" +
                               $"`{prefix}puzzle` or `{prefix}puzzle all`\n" +
                               $"`{prefix}puzzle #channel-name`");

            await channel.SendMessageAsync(embed: msg.Build());

            // ReSharper disable once PossibleNullReferenceException
            if (user.GuildPermissions.Administrator)
            {
                msg = new EmbedBuilder()
                     .WithTitle("Admin Commands")
                     .WithColor(Color.Blue)
                     .AddField($"{prefix}setanswer or {prefix}sa",
                               "Sets an answer for a puzzle.\n" +
                               $"`{prefix}setanswer #channel-name answer`")
                     .AddField($"{prefix}setcloseanswer or {prefix}sca",
                               "Sets a close answer for a puzzle.\n" +
                               $"`{prefix}setcloseanswer #channel-name answer`")
                     .AddField($"{prefix}sethint or {prefix}sh",
                               "Sets a hint for a puzzle.\n" +
                               $"`{prefix}sethint #channel-name answer`")
                     .AddField($"{prefix}getanswer or {prefix}ga\n"       +
                               $"{prefix}getcloseanswer or {prefix}gca\n" +
                               $"{prefix}gethint or {prefix}gh",
                               "Gets the Answer/CloseAnswer/Hint for one or every puzzle.\n" +
                               $"`{prefix}getanswer #channel-name`\n"                        +
                               $"`{prefix}getanswer` or `{prefix}getanswer all`")
                     .AddField($"{prefix}setquote or {prefix}sq",
                               "Adds a new quote of the chosen type to the database. Types:\n" +
                               "0 - CorrectAnswer\n"                                           +
                               "1 - WrongAnswer\n"                                             +
                               "2 - Greeting\n"                                                +
                               "`_NAME_` is used as a placeholder for user mention.\n"         +
                               $"`{prefix}setquote 2 Hello _NAME_.`");

                await channel.SendMessageAsync(embed: msg.Build());
            }

            await channel.CloseAsync();
        }

        public static async Task SendMessageToChannelAsync(string message, string channel)
        {
            var ch = GetChannelFromConfig(channel);
            await ch.SendMessageAsync(message);
        }

        public static SocketTextChannel GetChannelFromConfig(string channel)
        {
            var config = BotClient.Instance.Configuration;
            var client = BotClient.Instance.Client;

            var id = ulong.Parse(config[channel]);
            var ch = client.GetChannel(id) as SocketTextChannel ?? throw new Exception($"Channel {id} is not valid.");

            return ch;
        }

        public static async Task AddPuzzleDataAsync(PuzzleDataModel puzzle)
        {
            if (SqliteDataAccess.GetPuzzle(puzzle.PuzzleCode) == null)
            {
                SqliteDataAccess.AddOrUpdatePuzzle(new PuzzleModel
                                              {
                                                  Code   = puzzle.PuzzleCode,
                                                  Points = Constants.PUZZLE_STARTING_POINTS
                                              });
                await SendMessageToChannelAsync($"Added new puzzle: <#{puzzle.PuzzleCode}>",
                                                "notifications_channel");
            }

            SqliteDataAccess.AddPuzzleData(puzzle);
        }

        public static async Task UpdateLeaderboard()
        {
            var channel = GetChannelFromConfig("leaderboard_channel");
            var message = (await channel.GetPinnedMessagesAsync()).First() as IUserMessage ?? throw new Exception("No pinned message");

            var users = SqliteDataAccess.GetUsers();
            var msg = new EmbedBuilder()
                     .WithTitle("Leaderboard")
                     .WithColor(Color.Blue);

            msg.Description = $"```{"Username",-25}{"Solved",6}{"Score",10}\n\n";
            foreach (var user in users)
            {
                if (user.Solved == 0) break;
                msg.Description += $"{user.Name,-25}{user.Solved,6}{user.Score,10}\n";
            }

            msg.Description += "```";

            await message.ModifyAsync(m =>
                                      {
                                          m.Content = null;
                                          m.Embed = msg.Build();
                                      });
        }
    }
}