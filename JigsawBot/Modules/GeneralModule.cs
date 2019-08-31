using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace JigsawBot
{
    public class GeneralModule : ModuleBase<SocketCommandContext>
    {
        [Command("help"), Alias("h")]
        [Summary("Posts a description of the usable commands.")]
        public async Task Help()
        {
            var user    = Context.User as SocketGuildUser;
            var message = Context.Message;
            await message.DeleteAsync();

            await BotActions.SendHelpMessageAsync(user);
        }

        [Command("hide")]
        [Summary("Hides solved puzzle channels.")]
        public async Task Hide()
        {
            var user    = Context.User;
            var message = Context.Message;
            await message.DeleteAsync();

            await BotActions.SetSolvedChannelsViewPermissionAsync(user, true);
        }

        [Command("show")]
        [Summary("Shows solved puzzle channels.")]
        public async Task Show()
        {
            var user    = Context.User;
            var message = Context.Message;
            await message.DeleteAsync();

            await BotActions.SetSolvedChannelsViewPermissionAsync(user, false);
        }
    }
}
