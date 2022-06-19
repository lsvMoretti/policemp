using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BotWorker.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BotWorker
{
    public class BotWorker : BackgroundService
    {
        private readonly ILogger<BotWorker> _logger;
        private readonly IArgumentProviderService _args;
        private readonly IHostApplicationLifetime _lifetime;

        private Process BotService;

        public BotWorker(ILogger<BotWorker> logger, IArgumentProviderService args, IHostApplicationLifetime lifetime)
        {
            _logger = logger;
            _args = args;
            _lifetime = lifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (BotService == null || BotService.HasExited)
                {
                    StartBotApplication();
                }

                await Task.Delay(100);
            }
            
            BotService?.Kill(true);
        }

        private void StartBotApplication()
        {
            var args = _args.GetArguments();
            var processStartInfo = new ProcessStartInfo("DiscordBot.exe", string.Join(" ", args))
            {
                WorkingDirectory = Environment.CurrentDirectory
            };
            
            BotService = Process.Start(processStartInfo);
        }
    }
}