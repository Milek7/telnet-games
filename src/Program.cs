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
                    VT100 vt = new VT100(tcpClient);
                    vt.Bell();
                    if (vt.Flush() != VT100.FlushReturnState.Success)
                        continue;
                    Game.PlayerClass player = new Game.PlayerClass() { playerType = Game.PlayerType.Player, vt = vt, compatibilityMode = true };
                    HandlePlayer(typeof(Lobby), player);
                    Console.WriteLine("Client connected.");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
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
                        if (game.MinPlayers <= game.PlayerCount)
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
                    if (item.PlayerCount < item.MaxPlayers || item.MaxPlayers == -1 || player.playerType == Game.PlayerType.Spectator)
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
                game.PlayerHandoff += OnPlayerHandoff;
                games.Add(game);
                Console.WriteLine("Game created.");
            }
            game.AddPlayer(player);
        }

        static void OnPlayerLeft(Game game, Game.PlayerClass player, bool connectionKilled)
        {
            if (connectionKilled)
            {
                Console.WriteLine("Client disconnected. (connection killed)");
                player.vt.Close();
            }
            else
            {
                if (game.GetType() == typeof(Lobby))
                {
                    Console.WriteLine("Client disconnected. (leaved)");
                    player.vt.ClearScreen();
                    player.vt.WriteText("Goodbye.\r\n");
                    player.vt.Flush();
                    player.vt.Close();
                }
                else
                    HandlePlayer(typeof(Lobby), player);
            }
        }

        static void OnPlayerHandoff(Game game, Type destination, Game.PlayerClass player)
        {
            HandlePlayer(destination, player);
        }

        static void OnGameKilled(Game game)
        {
            game.GameKilled -= OnGameKilled;
            game.PlayerLeft -= OnPlayerLeft;
            game.PlayerHandoff -= OnPlayerHandoff;
            games.Remove(game);
        }
    }
}
