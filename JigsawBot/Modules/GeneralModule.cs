using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace JigsawBot
{
    public class GeneralModule : ModuleBase<SocketCommandContext>
    {
        private readonly BotActions _actions;

        public GeneralModule(BotActions actions)
        {
            _actions = actions;
        }

        [Command("help"), Alias("h")]
        [Summary("Posts a description of the usable commands.")]
        public async Task Help()
        {
            var user    = Context.User as SocketGuildUser;
            var message = Context.Message;
            await message.DeleteAsync();

            await _actions.SendHelpMessageAsync(user);
        }
    }
}
