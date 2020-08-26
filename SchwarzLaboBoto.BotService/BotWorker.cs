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
using SchwarzLab.Clients.Events;
using SchwarzLaboBoto.Clients;

namespace SchwarzLaboBoto.BotService
{
    public class BotWorker : BackgroundService
    {
        private readonly ILogger<BotWorker> _logger;
        private readonly IConfiguration _configuration;
        private TwitchWSClient twitchClient;
        public BotWorker(ILogger<BotWorker> logger, IConfiguration configuration)
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
                _configuration["BotSettings:Channels"],
                bool.Parse(_configuration["BotSettings:IRCV3"])
                );

            twitchClient.OnMessage += MessageReceived;
            twitchClient.OnError += ClientError;
            return base.StartAsync(cancellationToken);
        }

        private void ClientError(object sender, OnErrorEventArgs e)
        {
            _logger.LogError(e.Exception,"Error");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Connecting to Twitch");
            await twitchClient.ConnectTwitchChat();
            while (!stoppingToken.IsCancellationRequested)
            {
               //do i need to put code in here or will Events work?
            }
        }

        private void MessageReceived(object sender,OnMessageEventArgs e)
        {
            _logger.LogInformation($"Message recieved: parsing");
            var parsedMessage = ParseMessage(e.Message);
        }

        private object ParseMessage(string message)
        {
            _logger.LogInformation($"In parser {message}");
            return new object();
        }
    }
}