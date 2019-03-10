using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    class Server
    {
        private const string StrWelcome = "Welcome to simple-chat-server.\n/start - to start server\n/stop - to stop server\n/exit - to exit program";

        private const string MsgHelp = "Welcome to chat.\n/users - to print users online.\n/msgto [user] [message] - to send message.\n/chname - to change name.";

        public Thread thread { get; private set; }

        public bool IsServerActive { get; private set; }

        public int UserIdCounter { get; set; }

        public int Port { get; set; }

        public Socket ListeningSocket { get; private set; }

        public List<Client> LstClients { get; private set; }

        public IPEndPoint EndPoint { get; private set; }

        public static bool IsSocketConnected(Socket s)
        {
            if (!s.Connected)
                return false;

            if (s.Available == 0)
                if (s.Poll(1000, SelectMode.SelectRead))
                    return false;

            return true;
        }

        public Server()
        {
            Port = 0;
            UserIdCounter = 0;
            IsServerActive = false;
            LstClients = new List<Client>();
        }

        private bool SendMessage(Client client, string msg)
        {
            try
            {
                client.Socket.Send(Encoding.Unicode.GetBytes(msg));
                return true;
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Can't send message to {0}", client.Username);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            return false;
        }

        private void WaitConnections()
        {
            while (IsServerActive)
            {
                Client client = new Client
                {
                    ID = UserIdCounter,
                    Username = "User" + UserIdCounter.ToString(),
                };
                try
                {
                    client.Socket = ListeningSocket.Accept();
                    client.thread = new Thread(() => ProcessMessaging(client)); //?????????
                    Console.WriteLine("{0} connected.", client.Username);
                    UserIdCounter += 1;
                    LstClients.Add(client);
                    client.thread.Start();
                }
                catch (Exception)
                {
                    Console.WriteLine("Error in waiting to connections");
                }
            }
        }

        public void ProcessMessaging(Client client)
        {
            try
            {
                SendMessage(client, MsgHelp);
            }
            catch
            {
                Console.WriteLine("Error in user connecting");
                return;
            }
            while (IsServerActive)
            {
                try
                {
                    byte[] buff = new byte[512]; //256 Unicode symbols :)
                    if (!IsSocketConnected(client.Socket))
                    {
                        LstClients.Remove(client);
                        Console.WriteLine("{0} disconnected.", client.Username);
                        client.Dispose();
                        return;
                    }
                    int res = client.Socket.Receive(buff);
                    if (res > 0)
                    {
                        string response = string.Empty;
                        string strMessage = Encoding.Unicode.GetString(buff);
                        Console.WriteLine(client.Username + ": " + strMessage.Trim('\0'));
                        if (strMessage.Substring(0, 7) == "/chname")
                        {
                            strMessage = strMessage.Trim('\0');
                            int pos = strMessage.IndexOf(" ");
                            if (pos > 0)
                            {
                                string username = strMessage.Substring(pos + 1);
                                bool IsFree = true;
                                foreach (Client user in LstClients)
                                {
                                    if (user.Username == username)
                                    {
                                        IsFree = false;
                                        response = "Such name is used.";
                                        break;
                                    }
                                }
                                if (IsFree)
                                {
                                    client.Username = username;
                                    response = "Your new name: " + username;
                                }
                            }
                            else
                            {
                                response = "Illegal name";
                            }
                        }
                        if (strMessage.Substring(0, 6) == "/users")
                        {
                            response = "Users online:\n";
                            foreach (Client user in LstClients)
                            {
                                response = response + "[" + user.Username + "]" + "\n";
                            }
                        }
                        if (strMessage.Substring(0, 6) == "/msgto")
                        {
                            bool IsSend = false;
                            strMessage = strMessage.Trim('\0');
                            strMessage = strMessage.Replace("/msgto ", "");
                            int pos = strMessage.IndexOf(" ");
                            if (pos > 0)
                            {
                                string msgto = client.Username + ":" + strMessage.Substring(pos);
                                string username = strMessage.Substring(0, pos);
                                foreach (Client user in LstClients)
                                {
                                    if (user.Username == username)
                                    {
                                        IsSend = SendMessage(user, msgto);
                                    }
                                }
                                if (!IsSend)
                                {
                                    response = "Can't send message to " + username;
                                }
                            }
                            else
                            {
                                response = "/msgto [user] [message] - to send message";
                            }
                        }
                        SendMessage(client, response);
                    }
                }
                catch (SocketException)
                {
                    // The exchange goes while there is a connection with the client's socket
                    LstClients.Remove(client);
                    Console.WriteLine("{0} disconnected.", client.Username);
                    client.Dispose();
                    return;
                }
            }
        }

        public void Work()
        {
            Console.WriteLine(StrWelcome);
            while (true)
            {
                string input = Console.ReadLine();

                if (input.IndexOf("/users") >= 0)
                {
                    foreach (Client client in LstClients)
                    {
                        Console.WriteLine("id = " + client.ID + " [" + client.Username + "]");
                    }
                }

                if (input.IndexOf("/start") >= 0)
                {
                    Console.Write("Enter port to listening: ");
                    input = Console.ReadLine();
                    if (Int32.TryParse(input, out int port))
                    {
                        Port = port;
                    }
                    this.Start();
                    if (!IsServerActive)
                    {
                        Console.WriteLine("Server is not running. Try again");
                    }
                }

                if (input.IndexOf("/stop") >= 0)
                {
                    this.Stop();
                }

                if (input.IndexOf("/exit") >= 0)
                {
                    this.Stop();
                    return;
                }
            }
        }

        private void Start()
        {
            if (IsServerActive) return;
            ListeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                EndPoint = new IPEndPoint(IPAddress.Any, Port);
            }
            catch (Exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error in creating EndPoint.");
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }
            try
            {
                ListeningSocket.Bind(EndPoint);
            }
            catch (Exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Can't bind to local address.");
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }
            ListeningSocket.Listen(5);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Server was started.\nWait for connections...");
            Console.ForegroundColor = ConsoleColor.Gray;
            thread = new Thread(WaitConnections);
            thread.Start();
            IsServerActive = true;
        }

        private void Stop()
        {
            if (!IsServerActive) return;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Stopping server...");
            IsServerActive = false;
            //disconnect clients
            while (LstClients.Count != 0)
            {
                Client client = LstClients[0];
                LstClients.Remove(client);
                client.Dispose();
            }
            ListeningSocket.Close();
            ListeningSocket = null;
            Console.WriteLine("Server stopped.");
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
