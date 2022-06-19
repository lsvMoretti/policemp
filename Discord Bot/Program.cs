using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;

namespace DiscordBot
{
    public class Program
    {
        private static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        private DiscordBot _discordBot;
        private TwitterBot _twitterBot;
        private SheetInterface _sheetInterface;
        
        private async Task MainAsync()
        {
            
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            
            _discordBot = new DiscordBot();
            await _discordBot.StartDiscordBot();

            //_sheetInterface = new SheetInterface();
            //await _sheetInterface.StartGoogleSheetInterface();

            //_twitterBot = new TwitterBot();
            //await _twitterBot.StartTwitterBot();
            
            await Task.Delay(-1);
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject.ToString());
            return;
        }
    }
}