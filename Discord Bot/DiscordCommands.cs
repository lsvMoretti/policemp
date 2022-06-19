using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Discord;
using Discord.Commands;

namespace DiscordBot
{
    public class DiscordCommands : ModuleBase<SocketCommandContext>
    {
        [Command("donators"), RequireContext(ContextType.Guild)]
        public async Task DiscordCommandDonators()
        {
            var guildUser = Context.Guild.GetUser(Context.User.Id);

            if (guildUser is null) return;

            if (guildUser.Roles.All(x => x.Id != 616641490336219137)) return;

            var basicDonators = Context.Guild.Users.Where(x => x.Roles.Any(x => x.Id == 673911514004062208)).ToList();
            Console.WriteLine($"Found {basicDonators.Count()} Basic Donators");
            
            var donatorPros = Context.Guild.Users.Where(x => x.Roles.Any(x => x.Id == 793170213759746049)).ToList();
            Console.WriteLine($"Found {donatorPros.Count()} Donator Pros");

            string donators = "";

            if (basicDonators.Any())
            {
                foreach (var basicDonator in basicDonators)
                {
                    donators =
                        $"{donators}\nUser: {basicDonator.Username} - Nick: {basicDonator.Nickname} - Basic Donator";
                }
            }
            
            if (donatorPros.Any())
            {
                foreach (var proDonator in donatorPros)
                {
                    donators =
                        $"{donators}\nUser: {proDonator.Username} - Nick: {proDonator.Nickname} - Pro Donator";
                }
            }
            
            await File.WriteAllTextAsync($"{Directory.GetCurrentDirectory()}/donatorList-{DateTime.Now.Day}-{DateTime.Now.Month}.txt", donators);

            var channel = Context.Channel as ITextChannel;

            if (channel == null) return;
            
            await channel.SendFileAsync(
                $"{Directory.GetCurrentDirectory()}/donatorList-{DateTime.Now.Day}-{DateTime.Now.Month}.txt",
                $"Here is the latest donator list {Context.User.Mention}! {basicDonators.Count()} " +
                $"Basic Donators & {donatorPros.Count()} Pro Donators! That's {basicDonators.Count() + donatorPros.Count()} Donators!");

            File.Delete(
                $"{Directory.GetCurrentDirectory()}/donatorList-{DateTime.Now.Day}-{DateTime.Now.Month}.txt");

        }

        [Command("wipe"), RequireContext(ContextType.Guild), RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task DiscordCommandWipe(int wipeCount = 1)
        {
            wipeCount += 1;
            
            var messages = await Context.Channel.GetMessagesAsync(wipeCount, CacheMode.AllowDownload).FlattenAsync();

            foreach (var message in messages)
            {
                await message.DeleteAsync();
                await Task.Delay(50);
            }

            await Context.User.SendMessageAsync($"I've deleted the required messages sir!");
        }
    }
}