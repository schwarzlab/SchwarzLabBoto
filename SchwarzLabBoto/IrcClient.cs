using System;
using System.IO;
using System.Net.Sockets;


namespace SchwarzLaboBoto.Clients
{
    public class IrcClient
    {
        private readonly string userName;
        private readonly string channel;
        private TcpClient tcpClient;
        private StreamReader inputStream;
        private StreamWriter outputStream;
        public IrcClient(string ip, int port, string userName, 
                         string password, string channel)
        {
            this.userName = userName;
            this.channel = channel;

            tcpClient = new TcpClient(ip, port);
            inputStream = new StreamReader(tcpClient.GetStream());
            outputStream = new StreamWriter(tcpClient.GetStream());

            ConnectToChat(password);
        }

        private void ConnectToChat(string password)
        {
            outputStream.WriteLine($"PASS {password}");
            outputStream.WriteLine($"NICK {userName}");
            outputStream.WriteLine($"USER {userName} 8 * :{userName}");
            outputStream.WriteLine($"JOIN #{channel}");
            outputStream.Flush();
        }

        public void SendIrcMessage(string message)
        {
            outputStream.Write(message);
            outputStream.Flush();
        }

        public string ReadMessage()
        {
            try
            {
                return inputStream.ReadLine();

            }
            catch(Exception ex)
            {
                System.Console.WriteLine($"An exception happened {ex.Message}");
                return string.Empty;
            }
        }


        public void SendTwitchChatMessage(string message)
        {
            SendIrcMessage($":{userName}!{userName}@{userName}.tmi.twitch.tv PRIVMSG #{channel} : {message}");
        }


    }
}
