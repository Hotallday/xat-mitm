using Fiddler;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static System.Console;
namespace xat_MITM_ {
    class Program {
        private const int MF_BYCOMMAND = 0x00000000;
        public const int SC_CLOSE = 0xF060;

        [DllImport("user32.dll")]
        public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();
        private static TcpListener listen;
        private static string IP = "";
        private static int Port = 0;
        static void Main(string[] args) {
            //Well ty iiegor for this functions and methods
            Title = "Xat MITM - By Mark";
            DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), SC_CLOSE, MF_BYCOMMAND);
            CancelKeyPress += (object sender, ConsoleCancelEventArgs e) => {
                PerformShutDown();
                e.Cancel = true;
            };
            CursorVisible = false;
            Initialize();
            Read();
        }
        public static async Task MITMServer() {
            try {
                WriteLine("[INFO] - Starting MITM server...");
                listen = new TcpListener(IPAddress.Any, 1337);
                listen.Start();
                WriteLine("[INFO] - MITM server started. Waiting for connection...");
                while (true) {
                    try {
                        var client = await listen.AcceptTcpClientAsync();
                        ThreadPool.QueueUserWorkItem(async state => await HandleClient(client));
                    } catch {
                        break;
                    }
                }
            } catch (Exception ex) {
                WriteLine(ex.Message);
            }
        }
        /// <summary>
        /// TODO 
        /// MAKE IT WORK WITH XAT
        /// </summary>
        public static void ProxyServer() {
            try {
                WriteLine("[INFO] - Starting proxy server...");
                var regex = new Regex("sockDomain\":\"([\\w.]+)\",\"sockPort\":\"([\\w.]+)\"");
                FiddlerApplication.Startup(8888, false, true, true);
                FiddlerApplication.BeforeRequest += delegate (Session oS) {
                    oS.bBufferResponse = true;
                };
                FiddlerApplication.BeforeResponse += delegate (Session oS) {
                    oS.utilDecodeResponse();
                    var Body = Encoding.UTF8.GetString(oS.responseBodyBytes);
                    if (oS.url.Contains("ip2")) {
                        Body = Body.Replace("s.xat.com", "127.0.0.1");
                        oS.utilSetResponseBody(Body);
                        return;
                    }
                    var check = regex.Match(Body);
                    if (check.Success) {
                        IP = check.Groups[1].Value;
                        Port = Convert.ToInt16(check.Groups[2].Value);
                        WriteLine($"[INFO] - Found config file replacing {IP}:{Port} with 127.0.0.1:1337...");
                        Body = Body.Replace(IP, "127.0.0.1");
                        Body = Body.Replace(Port.ToString(), "1337");
                        oS.utilSetResponseBody(Body);
                        WriteLine($"[INFO] - CHAT.SWF is now using 127.0.0.1:1337.");
                        return;
                    }
                    var Output  = Body + "<a href=\"http://xat.com/markian\"><p><big>Using Mark MITM. Well Thank you for using it.</big></p></a>";
                    oS.utilSetResponseBody(Output);
                };
                WriteLine("[INFO] - Proxy server listening on IP:127.0.0.1 and Port:8888");
            } catch (Exception) {
                WriteLine("[ERROR] - Something went wrong with the proxy server.");
            }
        }
        public static async Task HandleClient(TcpClient Client) {
            if(string.IsNullOrWhiteSpace(IP) || Port == 0) {
                WriteLine("[ERROR] - We need to fetch the server ip and port. Please reload the page.");
                Client.GetStream().Close();
                Client.Close();
                return;
            }
            var server = new TcpClient();
            NetworkStream serverstream = null;
            NetworkStream stream = null;
            try {
                WriteLine("[INFO] - CHAT.SWF connected.");
                WriteLine("[INFO] - Connecting to server...");
                await server.ConnectAsync(IPAddress.Parse(IP), Port);
                stream = Client.GetStream();
                serverstream = server.GetStream();
                new Thread(async () => {
                    var data = new byte[4546];
                    while (true) {
                        try {
                            var responseData = string.Empty;
                            var bytes = await stream.ReadAsync(data, 0, data.Length);
                            responseData = Encoding.UTF8.GetString(data, 0, bytes);
                            if (!string.IsNullOrWhiteSpace(responseData)) {
                                Send(serverstream, responseData);
                            } else {
                                throw new Exception();
                            }
                        } catch (Exception) {
                            stream.Close();
                            serverstream.Close();
                            server.Close();
                            Client.Close();
                            WriteLine("[ERROR] - If you're having errors connecting, please reload the ixat.");
                            break;
                        }
                    }
                }).Start();
                new Thread(async () => {
                    var data = new Byte[4546];
                    while (true) {
                        try {
                            var responseData = string.Empty;
                            var bytes = await serverstream.ReadAsync(data, 0, data.Length);
                            responseData = Encoding.UTF8.GetString(data, 0, bytes);
                            if (!string.IsNullOrWhiteSpace(responseData)) {
                                Send(stream, responseData);
                            } else {
                                throw new Exception();
                            }
                        } catch (Exception) {
                            stream.Close();
                            serverstream.Close();
                            server.Close();
                            Client.Close();
                            WriteLine("[ERROR] - If you're having errors connecting, please reload the ixat.");
                            break;
                        }
                    }
                }).Start();
            } catch (Exception) {
                stream.Close();
                serverstream.Close();
                server.Close();
                Client.Close();
                WriteLine("[ERROR] - If you're having errors connecting, please reload the ixat.");
            }
        }
        public static async void Send(NetworkStream stream, string message) {
            try {
                var data = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
                WriteLine($"[INFO] - Send packet -> {message}");
            } catch (Exception) {
                WriteLine("[ERROR] - If you're having errors connecting, please reload the ixat.");
            }
        }
        public static void PerformShutDown() {
            try {
                WriteLine("[INFO] - Shutting down...");
                listen.Stop();
                FiddlerApplication.Shutdown();
            } finally {
                Environment.Exit(0);
            }
        }
        private static Random _random = new Random();
        public static string GetRandomIp() => $"{_random.Next(0, 255)}.{_random.Next(0, 255)}.{_random.Next(0, 255)}.{_random.Next(0, 255)}";
        
        public static async void Initialize() {
            ForegroundColor = ConsoleColor.Green;
            WriteLine(" _____          _______                            ");
            WriteLine("|  __ \\        |__   __|                           ");
            WriteLine("| |__) |  ___     | |      ___   __      __  _ __  ");
            WriteLine(@"|  _  /  / __|    | |     / _ \  \ \ /\ / / | '_ \ ");
            WriteLine(@"| | \ \  \__ \    | |    | (_) |  \ V  V /  | | | |");
            WriteLine(@"|_|  \_\ |___/    |_|     \___/    \_/\_/   |_| |_|");
            WriteLine("                               Welcome to the ship!");
            new Thread(ProxyServer).Start();
            await MITMServer();
        }
    }
}
