using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchwarzLaboBoto.Clients;

namespace SchwarzLaboBoto.BotService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private TwitchWSClient twitchClient;
        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Service");
            twitchClient = new TwitchWSClient(
                _configuration["BotSettings:TwitchUrl"],
                _configuration["BotSettings:BotUsername"],
                _configuration["BotSettings:AuthToken"],
                _configuration["BotSettings:Channels"]
                );
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Connecting to Twitch");
            await twitchClient.ConnectTwitchChat();
            while (!stoppingToken.IsCancellationRequested)
            {
                var rcvMsg = await twitchClient.ReadMessage();
                if (!string.IsNullOrEmpty(rcvMsg) && !string.IsNullOrWhiteSpace(rcvMsg))
                {
                    _logger.LogInformation($"Message recieved: {rcvMsg}");
                }
            }
        }
    }
}
