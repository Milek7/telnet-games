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
        private static bool breakLoop = false;
        private static Object addingPlayer = new Object();

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
                    VT100 vt = new VT100(tcpClient.Client);
                    vt.ClearScreen();
                    vt.WriteText("Welcome on TelnetGames!");
                    vt.Flush();
                    Game game;
                    lock (addingPlayer)
                    {
                        if ((games.Count != 0) && (games[games.Count - 1].PlayersCount() == 1))
                            game = games[games.Count - 1];
                        else
                        {
                            game = new Pong();
                            game.GameKilled += OnGameKilled;
                            games.Add(game);
                        }
                        games[games.Count - 1].AddPlayer(new Game.PlayerClass() { playerType = Game.PlayerType.Player, tcpClient = tcpClient, vt = vt });
                        Console.WriteLine("Client connected.");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Failed to accept client!");
                }
            }
        }

        static void OnGameKilled(Game game)
        {
            games.Remove(game);
            breakLoop = true;
        }

        static void OnPlayerLeft(Game.PlayerClass player, bool connectionKilled)
        {
            if (connectionKilled)
            {
                try
                {
                    player.vt.Close();
                    player.tcpClient.Close();
                }
                catch { }
            }
            else
            {
                try
                {
                    player.vt.WriteText("Goodbye.");
                    player.vt.Flush();
                    player.vt.Close();
                    player.tcpClient.Close();
                }
                catch { }
            }
        }

        static void GameThreadMethod()
        {
            Stopwatch stopwatch = new Stopwatch();
            while (true)
            {
                stopwatch.Restart();
                lock (addingPlayer)
                {
                    foreach (Game game in games)
                    {
                        try
                        {
                            game.Tick();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            Console.WriteLine(e.StackTrace);
                            Console.WriteLine("Game.Tick exception, killing game! (THIS SHOULD NOT HAPPEN!)");
                            games.Remove(game);
                            break;
                        }
                        if (breakLoop)
                        {
                            breakLoop = true;
                            break;
                        }
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
