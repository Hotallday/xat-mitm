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
                    var client = await listen.AcceptTcpClientAsync();
                    ThreadPool.QueueUserWorkItem(async state => await HandleClient(client));
                }
            } catch {
            }
        }
        public static void ProxyServer() {
            try {
                WriteLine("[INFO] - Starting proxy server...");
                FiddlerApplication.Startup(8888, false, true, true);
                FiddlerApplication.BeforeRequest += delegate (Session oS) {
                    oS.bBufferResponse = true;
                };
                FiddlerApplication.BeforeResponse += delegate (Session oS) {
                    oS.utilDecodeResponse();
                    var Body = Encoding.UTF8.GetString(oS.responseBodyBytes);
                    var regex = new Regex("sockDomain\":\"([\\w.]+)\",\"sockPort\":\"([\\w.]+)\"");
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
                    var oRegEx = @"<img[^>]*(.*?)/>";
                    var Output = Regex.Replace(Body, oRegEx, "<p><big>Using Mark MITM. Well Thank you for using it.</big></p>");
                    oS.utilSetResponseBody(Output);
                };
                WriteLine("[INFO] - Proxy server listening on IP:127.0.0.1 and Port:8888");
            } catch {
            }
        }
        public static async Task HandleClient(TcpClient Client) {
            var server = new TcpClient();
            var serverstream = server.GetStream();
            var stream = Client.GetStream();
            try {
                WriteLine("[INFO] - CHAT.SWF connected.");
                WriteLine("[INFO] - Connecting to server...");
                await server.ConnectAsync(IPAddress.Parse(IP), Port);
                new Thread(async () => {
                    try { 
                    var data = new Byte[4546];
                        while (true) {
                            var responseData = String.Empty;
                            var bytes = await stream.ReadAsync(data, 0, data.Length);
                            responseData = Encoding.UTF8.GetString(data, 0, bytes);
                            if (!string.IsNullOrWhiteSpace(responseData)) {
                                Send(serverstream, responseData);
                            } else {
                                stream.Close();
                                serverstream.Close();
                                server.GetStream().Close();
                                server.Close();
                                Client.GetStream().Close();
                                Client.Close();
                                WriteLine("[ERROR] - Please reload the ixat.");
                                break;
                            }
                        }
                        
                    } catch {

                    }
                }).Start();
                new Thread(async () => {
                    try {
                        var data = new Byte[4546];
                        while (true) {
                            var responseData = string.Empty;
                            var bytes = await serverstream.ReadAsync(data, 0, data.Length);
                            responseData = Encoding.UTF8.GetString(data, 0, bytes);
                            if (!string.IsNullOrWhiteSpace(responseData)) {
                                Send(stream, responseData);
                            } else {
                                stream.Close();
                                serverstream.Close();
                                server.GetStream().Close();
                                server.Close();
                                Client.GetStream().Close();
                                Client.Close();
                                WriteLine("[ERROR] - Please reload the ixat.");
                                break;
                           }
                        }
                    } catch {

                    }
                }).Start();
            } catch (Exception) {
                stream.Close();
                serverstream.Close();
                server.GetStream().Close();
                server.Close();
                Client.GetStream().Close();
                Client.Close();
                Write("[ERROR] - Please reload the ixat.");
            }
        }
        public static async void Send(NetworkStream stream, string message) {
            try {
                var data = Encoding.ASCII.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
                WriteLine($"[INFO] - Send packet -> {message}");
            }
            catch(Exception) {
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
