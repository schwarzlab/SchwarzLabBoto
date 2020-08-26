using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;
using System.Collections.Concurrent;

namespace SchwarzLaboBoto.Clients
{
    public class TwitchWSClient : IDisposable
    {
        private ClientWebSocket client;
        private string ip;
        private string username;
        private string channel;
        private string password;
        private BlockingCollection<String> messageQueue;
        private CancellationTokenSource cancelTokenSource;
        private Task sender;
        private bool senderRunning;
        private bool disposing;
        public TwitchWSClient(string ip, string username, string password, string channel)
        {
            this.username = username;
            this.channel = channel;
            this.password = password;
            this.client = new ClientWebSocket();
            this.ip = ip;
            this.cancelTokenSource = new CancellationTokenSource();
            messageQueue = new BlockingCollection<String>();

        }

        public bool IsConnected
        {
            get
            {
                return (client.State == WebSocketState.Open);
            }
        }

        internal async Task CloseWS()
        {
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", cancelTokenSource.Token);
        }

        public async Task ConnectTwitchChat()
        {
            await client.ConnectAsync(new Uri(ip), cancelTokenSource.Token);

            await DirectIRC($"PASS {password}");
            await DirectIRC($"NICK {username}");
            await DirectIRC($"USER {username} 8 * :{username}");
            await DirectIRC("CAP REQ :twitch.tv/commands");
            await DirectIRC("CAP REQ :twitch.tv/tags");
            await DirectIRC($"JOIN #{channel}");
            Console.WriteLine(await ReadMessage());
            StartSender();
        }

        private Task DirectIRC(string message)
        {
            return client.SendAsync(GetMsgBytes(message), WebSocketMessageType.Text, true, cancelTokenSource.Token);

        }

        public void SendIRCMessage(string message)
        {
            Console.WriteLine($"Queuing message {message}");
            messageQueue.Add(message);
            //client.SendAsync(GetMsgBytes(message), WebSocketMessageType.Text, true, cancelTokenSource);
        }

        private  void StartSender()
        {
            sender = Task.Run(async () =>
            {
                senderRunning = true;
                try
                {
                    while(!disposing)
                    {
                        if(client.State == WebSocketState.Open)
                        {
                            try
                            {
                                var buff = GetMsgBytes(messageQueue.Take(cancelTokenSource.Token));
                                await client.SendAsync(buff, WebSocketMessageType.Text, true, cancelTokenSource.Token);
                            }
                            catch(Exception ex)
                            {
                                //todo: more error catching here
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    //do sender fail things here
                }
            });
        }

        public async Task<string> ReadMessage()
        {
            ArraySegment<byte> buff = new ArraySegment<byte>(new byte[1024]);
            await client.ReceiveAsync(buff, cancelTokenSource.Token);
            var msg = Encoding.UTF8.GetString(buff);
            return msg;
        }

        /// <summary>
        /// sets up prototype for sending twitch irc message
        /// </summary>
        /// <param name="message"></param>
        public void SendTwitchIRCMessage(string message)
        {
            SendIRCMessage($":{username}!{username}@{username}.tmi.twitch.tv PRIVMSG #{channel} : {message}");
        }

        /// <summary>
        /// Short hand for getting a buffer to send
        /// </summary>
        /// <param name="message">the string to convert to byte array</param>
        /// <returns></returns>
        private ArraySegment<byte> GetMsgBytes(string message)
        {
            return Encoding.UTF8.GetBytes(message);
        }

        public void Dispose()
        {
            disposing = true;
            if(messageQueue.Count > 0 && messageQueue .Count <25 && senderRunning)
            {
                while (messageQueue.Count > 0)
                {
                    Task.Delay(1000).Wait();
                }
            }
            CloseWS();
            cancelTokenSource.Cancel();
            Thread.Sleep(5000);
            cancelTokenSource.Dispose();
            client.Dispose();
            GC.Collect();           
            
        }
    }
}
