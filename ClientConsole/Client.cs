using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ClientConsole
{
    public class Client
    {
        private const string StrHelp = "Welcome to simple-chat-server.\n/connect - to connect to server\n/start - to start chatting\n/stop - to stop chatting\n/exit - to exit program";

        public Socket Socket { get; private set; }

        public IPAddress ServerIp { get; private set; }

        public int ServerPort { get; private set; }

        private bool IsConnected { get; set; }

        private Thread Thread { get; set; }

        private bool IsChatting { get; set; }

        public static bool IsSocketConnected(Socket s)
        {
            if (!s.Connected)
                return false;

            if (s.Available == 0)
                if (s.Poll(1000, SelectMode.SelectRead))
                    return false;

            return true;
        }

        public Client()
        {
            IsConnected = false;
            IsChatting = false;
        }

        public void Work()
        {
            Console.WriteLine(StrHelp);
            while (true)
            {
                string input = Console.ReadLine();
                if (!IsChatting)
                {
                    if (input.IndexOf("/connect") >= 0)
                    {
                        this.Connect();
                    }

                    if (input.IndexOf("/disconnect") >= 0)
                    {
                        this.Disconnect();
                    }

                    if (input.IndexOf("/exit") >= 0)
                    {
                        this.Disconnect();
                        return;
                    }

                    if (input.IndexOf("/start") >= 0)
                    {
                        StartChatting();
                    }

                    if (input.IndexOf("/help") >= 0)
                    {
                        Console.WriteLine(StrHelp);
                    }
                }
            }
        }

        private void Connect()
        {
            if (IsConnected) return;
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Console.Write("Enter server ipv4: ");
            string input = Console.ReadLine();
            if (IPAddress.TryParse(input, out IPAddress serverip))
            {
                ServerIp = serverip;
            }
            else
            {
                Console.WriteLine("You must enter valid IP. Try again.");
                return;
            }
            Console.Write("Enter server port: ");
            input = Console.ReadLine();
            if (Int32.TryParse(input, out int port))
            {
                ServerPort = port;
            }
            else
            {
                Console.WriteLine("You must enter valid port. Try again.");
                return;
            }
            try
            {
                Socket.Connect(ServerIp, ServerPort);
                IsConnected = true;
                Console.WriteLine("Connected.\n/start - to start chatting.");
            }
            catch
            {
                Console.WriteLine("Can't connect to server. Try again...");
                return;
            }
        }

        private void Disconnect()
        {
            if (!IsConnected) return;
            if (Socket != null && Thread != null)
            {
                Socket.Shutdown(SocketShutdown.Both);
                Socket.Close();
                Socket = null;
                Thread = null;
                IsConnected = false;
                IsChatting = false;
                Console.WriteLine("Disconnected.");
            }
        }

        private void StartChatting()
        {
            if (!IsConnected)
            {
                Console.WriteLine("You are not connected.\n/connect - to conect to server.");
                return;
            }
            Console.WriteLine("Start chatting.\n/stop - to stop chatting.");
            IsChatting = true;
            Thread = new Thread(GetMessages);
            Thread.Start();
            while (IsChatting)
            {
                string input = Console.ReadLine();
                if (!IsSocketConnected(Socket))
                {
                    Console.WriteLine("Connection lost.");
                    break;
                }
                if (input.IndexOf("/stop") >= 0)
                {
                    break;
                }
                else
                {
                    byte[] message = Encoding.Unicode.GetBytes(input);
                    Socket.Send(message);
                }
            }
            Disconnect();
        }

        private void GetMessages()
        {
            while (IsChatting && IsConnected)
            {
                try
                {
                    byte[] data = new byte[512];
                    int res = Socket.Receive(data);
                    if (res > 0)
                    {
                        string message = Encoding.Unicode.GetString(data);
                        Console.WriteLine(message.Trim('\0'));
                    }
                }
                catch (Exception)
                {
                    return;
                }
            }
        }
    }
}
