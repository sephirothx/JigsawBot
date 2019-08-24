using System.Threading.Tasks;
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
    }
}
