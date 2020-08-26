using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;
using System.Collections.Concurrent;
using SchwarzLab.Clients.Events;


namespace SchwarzLaboBoto.Clients
{
    //a lot of this code is based on project "twitchlib" at https://github.com/TwitchLib
    public class TwitchWSClient : IDisposable
    {
        private ClientWebSocket client;
        private string ip;
        private string username;
        private string channel;
        private readonly bool ircV3;
        private string password;
        private BlockingCollection<String> messageQueue;
        private CancellationTokenSource cancelTokenSource;
        private Task sender;
        private Task listener;
        private bool senderRunning;
        private bool disposing;
        private bool listenerRunning;

        public event EventHandler<OnMessageEventArgs> OnMessage;
        public event EventHandler<OnErrorEventArgs> OnError;
        public TwitchWSClient(string ip, string username, string password, string channel, bool v3)
        {
            this.username = username;
            this.channel = channel;
            this.ircV3 = v3;
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
            //TODO: figure out ircv3 parsing later(only really needed for mod commands and bit cheering)
            if (ircV3)
            {
                await DirectIRC("CAP REQ :twitch.tv/commands");
                await DirectIRC("CAP REQ :twitch.tv/tags");
            }
            await DirectIRC($"JOIN #{channel}");
            Console.WriteLine(await ReadMessage());
            StartListener();
            StartSender();
        }

        /// <summary>
        /// Sends IRC directly without queuing
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private Task DirectIRC(string message)
        {
            return client.SendAsync(GetMsgBytes(message), WebSocketMessageType.Text, true, cancelTokenSource.Token);

        }

        /// <summary>
        /// Send IRC Message to server
        /// </summary>
        /// <param name="message"></param>
        public void SendIRCMessage(string message)
        {
            Console.WriteLine($"Queuing message {message}");
            messageQueue.Add(message);
            //client.SendAsync(GetMsgBytes(message), WebSocketMessageType.Text, true, cancelTokenSource);
        }

        private void StartListener()
        {
            listener = Task.Run(async () =>
            {
                listenerRunning = false;
                try
                {
                    while (!disposing)
                    {
                        //1meg buffer to read into
                        
                        if (client.State == WebSocketState.Open)
                        {

                            var message = string.Empty;
                            WebSocketReceiveResult result = null;

                            while (!cancelTokenSource.IsCancellationRequested)
                            {
                                var buffer = new ArraySegment<byte>(new byte[512]);
                                result = await client.ReceiveAsync(buffer, cancelTokenSource.Token);
                                if (result == null)
                                    continue;
                                if (result.MessageType == WebSocketMessageType.Close)
                                {
                                    await CloseWS();
                                }
                                if (result.MessageType == WebSocketMessageType.Text)
                                {
                                    if (!result.EndOfMessage)
                                    {
                                        //message.Append(Encoding.UTF8.GetString(buffer));
                                        message += Encoding.UTF8.GetString(buffer);
                                        continue;
                                    }
                                    //message.Append(Encoding.UTF8.GetString(buffer));
                                    message += Encoding.UTF8.GetString(buffer);


                                    if (message.ToString().Contains("PING :tmi.twitch.tv", StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        SendIRCMessage("pong");
                                    }
                                    else
                                    {
                                        Task.Run(() => OnMessage?.Invoke(this, new OnMessageEventArgs { Message = message.ToString() })).Wait(50);
                                    }
                                }
                                //message.Clear();
                                message = string.Empty;

                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    listenerRunning = false;
                    await client.CloseAsync(WebSocketCloseStatus.Empty, "Close on error", cancelTokenSource.Token);
                    Task.Run(() => { this.OnError?.Invoke(this, new OnErrorEventArgs { Exception = ex }); }).Wait(50);
                    
                }
            });
        }

        private void StartSender()
        {
            sender = Task.Run(async () =>
            {
                senderRunning = true;
                try
                {
                    while (!disposing)
                    {
                        if (client.State == WebSocketState.Open)
                        {
                            try
                            {
                                var buff = GetMsgBytes(messageQueue.Take(cancelTokenSource.Token));
                                await client.SendAsync(buff, WebSocketMessageType.Text, true, cancelTokenSource.Token);
                            }
                            catch (Exception ex)
                            {
                                //todo: more error catching here
                            }
                        }
                    }
                }
                catch (Exception ex)
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
            if (messageQueue.Count > 0 && messageQueue.Count < 25 && senderRunning)
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
