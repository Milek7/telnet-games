using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TelnetGames
{
    class Program
    {
        static List<Game> games = new List<Game>();
        static Thread GameThread;

        static void Main(string[] args)
        {
            GameThread = new Thread(new ThreadStart(GameThreadMethod));
            GameThread.Start();

            IPAddress ipAddress = IPAddress.Any;
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 3000);
            TcpListener listener = new TcpListener(ipEndPoint);
            listener.Start();
            while (true)
            {
                try
                {
                    TcpClient tcpClient = listener.AcceptTcpClient();
                    NetworkStream stream = tcpClient.GetStream();
                    VT100 vt = new VT100(stream);
                    vt.ClearScreen();
                    vt.WriteText("Welcome on TelnetGames!");
                    vt.Flush();
                    Game game;
                    if ((games.Count != 0) && (games[games.Count - 1].PlayersCount() == 1))
                        game = games[games.Count - 1];
                    else
                    {
                        game = new Pong();
                        games.Add(game);
                    }
                    games[games.Count - 1].AddPlayer(new Game.PlayerClass() { playerType = Game.PlayerType.Player, tcpClient = tcpClient, vt = vt });
                    Console.WriteLine("Client connected.");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Failed to accept client!");
                }
            }
        }

        static void GameThreadMethod()
        {
            Stopwatch stopwatch = new Stopwatch();
            while (true)
            {
                stopwatch.Restart();
                foreach (Game game in games)
                {
                    try
                    {
                        game.Tick();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine("Game.Tick exception, killing game! (THIS SHOULD NOT HAPPEN!)");
                        games.Remove(game);
                        break;
                    }
                }
                stopwatch.Stop();
                int sleep = 50 - (int)stopwatch.ElapsedMilliseconds;
                if (sleep > 0)
                    Thread.Sleep(sleep);
            }
        }
    }
}
