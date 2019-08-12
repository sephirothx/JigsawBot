using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace JigsawBot
{
    public class GeneralModule : ModuleBase<SocketCommandContext>
    {
        [Command("say")]
        [Summary("Makes the bot say something.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Say([Remainder] string text)
        {
            var message = Context.Message;
            await message.DeleteAsync();

            var regex = new Regex(@"<#(?<channel>\d+)>\s+(?<text>.*)");
            var match = regex.Match(text);

            if (!match.Success)
            {
                await ReplyAsync(text);
                return;
            }

            var c = ulong.Parse(match.Groups["channel"].Value);
            text = match.Groups["text"].Value;

            var channel = BotClient.Instance.Client.GetChannel(c) as SocketTextChannel ??
                          throw new Exception($"Channel {c} is not valid.");
            await channel.SendMessageAsync(text);
        }

        [Command("update")]
        [Summary("Updates the user database.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Update()
        {
            var users = Context.Guild.Users;
            foreach (var user in users)
            {
                if (user.IsBot || SqliteDataAccess.GetUserById(user.Id.ToString()) != null)
                    continue;

                var u = new UserModel
                        {
                            Id     = user.Id.ToString(),
                            Name   = user.Username,
                            Solved = 0
                        };

                SqliteDataAccess.AddNewUser(u);
            }

            await ReplyAsync("User database updated.");
        }

        [Command("help"), Alias("h")]
        [Summary("Posts a description of the usable commands.")]
        public async Task Help()
        {
            var user    = Context.User as SocketGuildUser;
            var message = Context.Message;
            await message.DeleteAsync();

            await BotActions.SendHelpMessageAsync(user);
        }
    }
}
