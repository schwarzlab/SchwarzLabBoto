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
using SchwarzLaboBoto.BotService.Extensions;
using SchwarzLaboBoto.BotService.Models;

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
            HandleMessage(parsedMessage);
        }

        private void HandleMessage(ITwitchMessage parsedMessage)
        {
            if (string.IsNullOrEmpty(parsedMessage.Message) ||
                 string.IsNullOrWhiteSpace(parsedMessage.Message))
            {
                return;
            }
            if (parsedMessage.Message.StartsWith("!") 
                && parsedMessage.MessageType == TwtichMessageTypes.privmessage)
            {
                string msg = parsedMessage.Message;
                //splitting command from args
                //if there insn't an arg then it is a command that needs the sending username
                string command = msg.Contains(" ") ?
                    msg.Substring(1, msg.IndexOf(" ")).Trim() : msg.Substring(1).Trim();
                string arg = msg.Contains(" ") ?
                    msg.Substring( msg.IndexOf(" ")+1).Trim() : parsedMessage.Username.Trim();
                HandleCommand(command, arg);
            }
        }

        private void HandleCommand(string command, string arg)
        {
            switch(command)
            {
                case "dice":
                    var rand = new Random();
                    int roll = rand.Next(1, 7);
                    twitchClient.SendTwitchIRCMessage($"{arg} you rolled a {roll}");
                    break;
            }
        }

        private ITwitchMessage ParseMessage(string message)
        {
            _logger.LogInformation($"In parser {message}");
            var indexTagStart = message.IndexOf('@');
            var indexTagEnd = message.IndexOf(' ');
            Dictionary<string, string> tags = new Dictionary<string, string>();
            string tail;
            ITwitchMessage retval = new PrivMessageModel();
            if (indexTagStart > -1 && message.Contains("PRIVMSG"))
            {
                var rawTags = message.Substring(indexTagStart + 1, indexTagEnd - 1).Split(';');
                foreach(var rawTag in rawTags)
                {
                    var tag = rawTag.Split('=');
                    var key = tag[0];
                    var value = tag[1];
                    if(!string.IsNullOrEmpty(key))
                        tags.Add(key, value);
                }
                tail = message.Substring(indexTagEnd + 1);
                retval = new PrivMessageModel {
                    Username = tags["display-name"],
                    MessageId = tags["id"],
                    MessageType = TwtichMessageTypes.privmessage
                };
                //why doesn't this work in the intializer???
                var msg = tail.Substring(tail.IndexOf(" :") + 2);
                var endIdx = msg.IndexOf(Environment.NewLine);
                msg = msg.Remove(endIdx);
                retval.Message = msg;
            }
            //todo: figure how to actually parse since dict to obj isn't going to work here.
            //PrivMessageModel privMessage = tags.ToObject<PrivMessageModel>();
            
            return retval;
        }
    }
}
