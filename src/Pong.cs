using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TelnetGames
{
    class Pong : Game
    {
        private enum BallDirection
        {
            UpRight,
            DownRight,
            DownLeft,
            UpLeft
        }

        private enum PlayerEnum
        {
            None,
            Player1,
            Player2
        }

        private enum GameState
        {
            NotStarted,
            Training,
            Normal
        }

        private new class PlayerClass : Game.PlayerClass
        {
            public PlayerEnum playerEnum = PlayerEnum.None;
            public int points = 0;
            public int paddle = 8;

            public PlayerClass(Game.PlayerClass gamePlayer)
            {
                playerType = gamePlayer.playerType;
                tcpClient = gamePlayer.tcpClient;
                vt = gamePlayer.vt;
            }
        }

        private int ballX = 39;
        private int ballY = 11;
        private BallDirection ballDirection = BallDirection.DownLeft;
        private GameState gameState = GameState.NotStarted;
        private List<PlayerClass> players = new List<PlayerClass>();

        public override void Tick()
        {
            if (gameState == GameState.NotStarted)
                return;
            HandleInput();
            ComputeLogic();
            RenderFrame();
            Flush();
        }

        public override void AddPlayer(Game.PlayerClass player)
        {
            player.tcpClient.NoDelay = true;
            player.tcpClient.SendTimeout = 10;
            player.vt.SetForegroundColor(new VT100.ColorStruct { Bright = true, Color = VT100.ColorEnum.Blue });
            player.vt.SetCursorVisiblity(false);
            player.vt.Bell();
            players.Add(new PlayerClass(player));
            if (player.playerType == PlayerType.Player)
            {
                if (FindPlayerEnum(PlayerEnum.Player1) == null)
                {
                    players[players.Count - 1].playerEnum = PlayerEnum.Player1;
                    gameState = GameState.Training;
                    ResetPositions();
                    UpdateInfo(players[players.Count - 1], "STEERING: A and Z keys.                                  WAITING FOR PLAYER...");
                }
                else if (FindPlayerEnum(PlayerEnum.Player2) == null)
                {
                    players[players.Count - 1].playerEnum = PlayerEnum.Player2;
                    gameState = GameState.Normal;
                    ResetPositions();
                    UpdateInfo(players[players.Count - 1], "STEERING: A and Z keys.");
                }
            }
            player.vt.Flush();
        }

        public override void RemovePlayer(Game.PlayerClass player)
        {
            throw new NotImplementedException();
        }

        public override void KillGame()
        {
            while (players.Count != 0)
            {
                foreach (PlayerClass player in players)
                {
                    try
                    {
                        player.vt.ClearScreen();
                        player.vt.SetCursor(0, 0);
                        player.vt.WriteText("Partner disconnected.");
                        player.vt.Close();
                        player.tcpClient.Close();
                        players.Remove(player);
                        break;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Problem during disconnecting client!");
                        players.Remove(player);
                        break;
                    }
                }
            }
            ResetPositions();
            gameState = GameState.NotStarted;
            GameKilledRaise();
        }

        public override int PlayersCount()
        {
            return players.Count;
        }

        private PlayerClass FindPlayerEnum(PlayerEnum playerEnum)
        {
            foreach (PlayerClass player in players)
                if (player.playerEnum == playerEnum)
                    return player;
            return null;
        }

        private void UpdateInfo(PlayerClass player, string info)
        {
            player.vt.SetBackgroundColor(new VT100.ColorStruct { Bright = false, Color = VT100.ColorEnum.Yellow });
            player.vt.SetCursor(1, 24);
            player.vt.ClearLine();
            player.vt.WriteText(info);
        }

        private void Flush()
        {
            foreach (PlayerClass player in players)
                if (!Flush(player))
                    break;
        }

        private bool Flush(PlayerClass player)
        {
            try
            {
                player.vt.Flush();
                return true;
            }
            catch (TimeoutException)
            {
                Console.WriteLine("Flush timeout, skipping frame!");
                return false;
            }
            catch (Exception)
            {
                Console.WriteLine("Flush exception!");
                if (player.playerType == PlayerType.Player)
                {
                    KillGame();
                    Console.WriteLine("...killing game!");
                }
                else
                {
                    try
                    {
                        player.vt.Close();
                        player.tcpClient.Close();
                    }
                    catch { }
                }
                return false;
            }
        }

        private void ResetPositions()
        {
            ballX = 39;
            ballY = 11;
            PlayerClass player1 = FindPlayerEnum(PlayerEnum.Player1);
            PlayerClass player2 = FindPlayerEnum(PlayerEnum.Player2);
            if (player1 != null)
                player1.paddle = 8;
            if (player2 != null)
                player2.paddle = 8;
            ballDirection = BallDirection.DownLeft;
        }

        private void RenderFrame()
        {
            foreach (PlayerClass player in players)
                RenderFrame(player);
        }

        private void RenderFrame(PlayerClass player)
        {
            PlayerClass player1 = FindPlayerEnum(PlayerEnum.Player1);
            PlayerClass player2 = FindPlayerEnum(PlayerEnum.Player2);

            player.vt.SetBackgroundColor(new VT100.ColorStruct { Bright = true, Color = VT100.ColorEnum.Blue });
            player.vt.SetCursor(79, 22);
            player.vt.ClearScreen(VT100.ClearMode.BeginningToCursor);

            player.vt.SetBackgroundColor(new VT100.ColorStruct { Bright = false, Color = VT100.ColorEnum.Yellow });
            player.vt.SetCursor(0, 0);
            player.vt.ClearLine();

            if (gameState == GameState.Normal)
            {
                player.vt.SetCursor(0, 23);
                player.vt.ClearLine();
            }

            if (gameState == GameState.Training)
                player.vt.DrawLine(79, 1, VT100.Direction.Vertical, 22);

            if (gameState == GameState.Normal)
            {
                player.vt.SetCursor(36, 0);
                player.vt.WriteText((player1.points < 10 ? " " + player1.points.ToString() : player1.points.ToString()) + " : " + player2.points.ToString());
            }

            player.vt.SetBackgroundColor(new VT100.ColorStruct { Bright = true, Color = VT100.ColorEnum.Green });
            player.vt.DrawLine(0, FindPlayerEnum(PlayerEnum.Player1).paddle + 1, VT100.Direction.Vertical, 5);
            if (gameState == GameState.Normal)
                player.vt.DrawLine(79, FindPlayerEnum(PlayerEnum.Player2).paddle + 1, VT100.Direction.Vertical, 5);

            player.vt.SetBackgroundColor(new VT100.ColorStruct { Bright = true, Color = VT100.ColorEnum.Yellow });
            player.vt.DrawLine(ballX, ballY + 1, VT100.Direction.Horizontal, 2);
        }

        private void HandleInput()
        {
            foreach (PlayerClass player in players)
                if (player.playerType != PlayerType.Spectator)
                    HandleInput(player);
        }

        private void HandleInput(PlayerClass player)
        {
            char? temp;
            while ((temp = player.vt.ReadChar()) != null)
            {
                if (temp == 'A' || temp == 'a')
                    player.paddle--;
                if (temp == 'Z' || temp == 'z')
                    player.paddle++;
            }
        }

        private void ComputeLogic()
        {
            PlayerClass player1 = FindPlayerEnum(PlayerEnum.Player1);
            PlayerClass player2 = FindPlayerEnum(PlayerEnum.Player2);

            if (player1.paddle < 0)
                player1.paddle = 0;
            if (player1.paddle > 17)
                player1.paddle = 17;
            if (gameState == GameState.Normal)
            {
                if (player2.paddle < 0)
                    player2.paddle = 0;
                if (player2.paddle > 17)
                    player2.paddle = 17;
            }

            switch (ballDirection)
            {
                case BallDirection.UpRight:
                    if (ballY == 0)
                    {
                        ballDirection = BallDirection.DownRight;
                        ComputeLogic();
                        Bell();
                        break;
                    }
                    if (ballX == 77)
                    {
                        if (gameState == GameState.Training || (player2.paddle - 1 <= ballY && ballY < player2.paddle + 6))
                        {
                            ballDirection = BallDirection.UpLeft;
                            ComputeLogic();
                            Bell();
                            break;
                        }
                    }
                    if (ballX == 78)
                    {
                        ScorePoint(FindPlayerEnum(PlayerEnum.Player1));
                        break;
                    }
                    ballY--;
                    ballX++;
                    break;
                case BallDirection.DownRight:
                    if (ballY == 21)
                    {
                        ballDirection = BallDirection.UpRight;
                        ComputeLogic();
                        Bell();
                        break;
                    }
                    if (ballX == 77)
                    {
                        if (gameState == GameState.Training || (player2.paddle - 1 <= ballY && ballY < player2.paddle + 6))
                        {
                            ballDirection = BallDirection.DownLeft;
                            ComputeLogic();
                            Bell();
                            break;
                        }
                    }
                    if (ballX == 78)
                    {
                        ScorePoint(FindPlayerEnum(PlayerEnum.Player1));
                        break;
                    }
                    ballY++;
                    ballX++;
                    break;
                case BallDirection.DownLeft:
                    if (ballY == 21)
                    {
                        ballDirection = BallDirection.UpLeft;
                        ComputeLogic();
                        Bell();
                        break;
                    }
                    if (ballX == 1)
                    {
                        if (player1.paddle - 1 <= ballY && ballY < player1.paddle + 6)
                        {
                            ballDirection = BallDirection.DownRight;
                            ComputeLogic();
                            Bell();
                            break;
                        }
                    }
                    if (ballX == 0)
                    {
                        ScorePoint(FindPlayerEnum(PlayerEnum.Player2));
                        break;
                    }
                    ballY++;
                    ballX--;
                    break;
                case BallDirection.UpLeft:
                    if (ballY == 0)
                    {
                        ballDirection = BallDirection.DownLeft;
                        ComputeLogic();
                        Bell();
                        break;
                    }
                    if (ballX == 1)
                    {
                        if (player1.paddle - 1 <= ballY && ballY < player1.paddle + 6)
                        {
                            ballDirection = BallDirection.UpRight;
                            ComputeLogic();
                            Bell();
                            break;
                        }
                    }
                    if (ballX == 0)
                    {
                        ScorePoint(FindPlayerEnum(PlayerEnum.Player2));
                        break;
                    }
                    ballY--;
                    ballX--;
                    break;
            }
        }

        private void Bell()
        {
            foreach (PlayerClass item in players)
                item.vt.Bell();
        }

        private void ScorePoint(PlayerClass player)
        {
            Bell();
            ResetPositions();
            if (gameState == GameState.Normal)
                player.points++;
        }
    }
}
