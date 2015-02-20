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
                    VT100 vt = new VT100(tcpClient.Client);
                    vt.ClearScreen();
                    vt.WriteText("Welcome on TelnetGames!");
                    vt.Flush();
                    Game.PlayerClass player = new Game.PlayerClass() { playerType = Game.PlayerType.Player, tcpClient = tcpClient, vt = vt };
                    HandlePlayer(typeof(Pong), player);
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
                for (int i = games.Count - 1; i >= 0; i--)
                {
                    Game game = games[i];
                    try
                    {
                        if (game.minPlayers <= game.PlayerCount)
                            game.Tick();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
                        Console.WriteLine("Game.Tick exception, killing game! (THIS SHOULD NOT HAPPEN!)");
                        OnGameKilled(game);
                    }
                }
                stopwatch.Stop();
                int sleep = 50 - (int)stopwatch.ElapsedMilliseconds;
                if (sleep > 0)
                    Thread.Sleep(sleep);
            }
        }

        static void HandlePlayer(Type type, Game.PlayerClass player)
        {
            Game game = null;
            foreach (Game item in games)
            {
                if (item.GetType() == type)
                {
                    if (item.PlayerCount < item.maxPlayers || player.playerType == Game.PlayerType.Spectator)
                    {
                        game = item;
                        break;
                    }
                }
            }
            if (game == null)
            {
                game = (Game)Activator.CreateInstance(type);
                game.GameKilled += OnGameKilled;
                game.PlayerLeft += OnPlayerLeft;
                games.Add(game);
            }
            game.AddPlayer(player);
        }

        static void OnPlayerLeft(Game game, Game.PlayerClass player, bool connectionKilled)
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

        static void OnGameKilled(Game game)
        {
            game.GameKilled -= OnGameKilled;
            game.PlayerLeft -= OnPlayerLeft;
            games.Remove(game);
        }
    }
}
