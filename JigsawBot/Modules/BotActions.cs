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
                     .AddField($"{prefix}leaderboard or {prefix}l",
                               "Displays the users with the most puzzles solved.")
                     .AddField($"{prefix}puzzle or {prefix}p",
                               "Displays informations regarding a specific puzzle.\n" +
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
                               $"`{prefix}getanswer` or `{prefix}getanswer all`");

                await channel.SendMessageAsync(embed: msg.Build());
            }

            await channel.CloseAsync();
        }
    }
}