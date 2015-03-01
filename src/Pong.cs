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

        private abstract class ColorPalette
        {
            public abstract VT100.ColorClass Background { get; }
            public abstract VT100.ColorClass Band { get; }
            public abstract VT100.ColorClass Paddle { get; }
            public abstract VT100.ColorClass Ball { get; }
            public abstract VT100.ColorClass Text { get; }
        }

        private class BrightColorPalette : ColorPalette
        {
            public override VT100.ColorClass Background { get { return new VT100.ColorClass { Bright = true, Color = VT100.ColorEnum.Blue }; } }
            public override VT100.ColorClass Band { get { return new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.Yellow }; } }
            public override VT100.ColorClass Paddle { get { return new VT100.ColorClass { Bright = true, Color = VT100.ColorEnum.Green }; } }
            public override VT100.ColorClass Ball { get { return new VT100.ColorClass { Bright = true, Color = VT100.ColorEnum.Yellow }; } }
            public override VT100.ColorClass Text { get { return new VT100.ColorClass { Bright = true, Color = VT100.ColorEnum.Blue }; } }
        }

        private class ClassicColorPalette : ColorPalette
        {
            public override VT100.ColorClass Background { get { return new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.Black }; } }
            public override VT100.ColorClass Band { get { return new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.Yellow }; } }
            public override VT100.ColorClass Paddle { get { return new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.Green }; } }
            public override VT100.ColorClass Ball { get { return new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.White }; } }
            public override VT100.ColorClass Text { get { return new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.Black }; } }
        }

        private class PlayerRemovedException : ApplicationException { }

        private new class PlayerClass : Game.PlayerClass
        {
            public PlayerEnum playerEnum = PlayerEnum.None;
            public int points = 0;
            public int paddle = 8;
            public ColorPalette colorPalette;

            public PlayerClass(Game.PlayerClass gamePlayer)
            {
                playerType = gamePlayer.playerType;
                vt = gamePlayer.vt;
                compatibilityMode = gamePlayer.compatibilityMode;
                if (compatibilityMode)
                    colorPalette = new ClassicColorPalette();
                else
                    colorPalette = new BrightColorPalette();
            }
        }

        private int ballX = 39;
        private int ballY = 11;
        private BallDirection ballDirection = BallDirection.UpLeft;
        private GameState gameState = GameState.NotStarted;
        private List<PlayerClass> players = new List<PlayerClass>();
        private int holdTicks;
        private int playerCount;

        public override int MinPlayers { get { return 1; } }
        public override int MaxPlayers { get { return 2; } }
        public override string Name { get { return "Pong (multiplayer)"; } }
        public override string Description { get { return ""; } }
        public override int PlayerCount { get { return playerCount; } }

        public override void Tick()
        {
            if (gameState == GameState.NotStarted)
                return;
            try
            {
                HandleInput();
                VerifyPositions();
                if (holdTicks == 0)
                    ComputeLogic();
                else
                    holdTicks--;
                RenderFrame();
                Flush();
            }
            catch (PlayerRemovedException) { }
        }

        public override void AddPlayer(Game.PlayerClass player)
        {
            player.vt.SetCursorVisiblity(false);
            player.vt.Bell();
            players.Add(new PlayerClass(player));
            if (player.playerType == PlayerType.Player)
                playerCount++;
            if (player.playerType == PlayerType.Player)
            {
                if (gameState == GameState.NotStarted)
                {
                    players[players.Count - 1].playerEnum = PlayerEnum.Player1;
                    gameState = GameState.Training;
                    ResetPositions();
                    UpdateInfo(PlayerType.Player, "CONTROLS: A and Z keys, E to exit.                       WAITING FOR PLAYER...");
                }
                else if (gameState == GameState.Training)
                {
                    players[players.Count - 1].playerEnum = PlayerEnum.Player2;
                    gameState = GameState.Normal;
                    ResetPositions();
                    UpdateInfo(PlayerType.Player, "CONTROLS: A and Z keys, E to exit.");
                }
            }
            if (player.playerType == PlayerType.Spectator)
                UpdateInfo(players[players.Count - 1], "Spectating.      Press E to exit.");
            player.vt.Flush();
        }

        public override void KillGame()
        {
            Console.WriteLine("Killing game!");
            for (int i = players.Count - 1; i >= 0; i--)
            {
                RemovePlayer(players[i]);
            }
            GameKilledRaise();
            throw new PlayerRemovedException();
        }

        private void UpdateInfo(PlayerType type, string info)
        {
            for (int i = players.Count - 1; i >= 0; i--)
                if (players[i].playerType == type)
                    UpdateInfo(players[i], info);
        }

        private void UpdateInfo(PlayerClass player, string info)
        {
            player.vt.SetBackgroundColor(player.colorPalette.Band);
            player.vt.SetForegroundColor(player.colorPalette.Text);
            player.vt.SetCursor(1, 24);
            player.vt.ClearLine();
            player.vt.WriteText(info);
        }

        private void Flush()
        {
            for (int i = players.Count - 1; i >= 0; i--)
            {
                Flush(players[i]);
            }
        }

        private void Flush(PlayerClass player)
        {
            switch (player.vt.Flush())
            {
                case VT100.FlushReturnState.Success:
                    break;
                case VT100.FlushReturnState.Timeout:
                    Console.WriteLine("Flush timeout, skipping frame!");
                    break;
                case VT100.FlushReturnState.Error:
                    Console.WriteLine("Flush exception!");
                    RemovePlayer(player);
                    throw new PlayerRemovedException();
            }
        }

        private void ResetPositions()
        {
            ResetPositions(PlayerEnum.None);
        }

        private void ResetPositions(PlayerEnum playerEnum)
        {
            ballX = 39;
            ballY = 11;
            holdTicks = 10;
            PlayerClass player1 = FindPlayerEnum(PlayerEnum.Player1);
            PlayerClass player2 = FindPlayerEnum(PlayerEnum.Player2);
            ballDirection = BallDirection.UpLeft;
            if (player1 != null)
            {
                player1.paddle = 8;
                if (player1.playerEnum == playerEnum)
                    ballDirection = BallDirection.UpLeft;
            }
            if (player2 != null)
            {
                player2.paddle = 8;
                if (player2.playerEnum == playerEnum)
                    ballDirection = BallDirection.UpRight;
            }
        }

        private void RenderFrame()
        {
            for (int i = players.Count - 1; i >= 0; i--)
                RenderFrame(players[i]);
        }

        private void RenderFrame(PlayerClass player)
        {
            PlayerClass player1 = FindPlayerEnum(PlayerEnum.Player1);
            PlayerClass player2 = FindPlayerEnum(PlayerEnum.Player2);

            player.vt.SetBackgroundColor(player.colorPalette.Background);
            player.vt.SetCursor(79, 22);
            player.vt.ClearScreen(VT100.ClearMode.BeginningToCursor);

            player.vt.SetBackgroundColor(player.colorPalette.Band);
            player.vt.SetCursor(0, 0);
            player.vt.ClearLine();

            if (gameState == GameState.Training)
                player.vt.DrawLine(79, 1, VT100.Direction.Vertical, 22);

            if (gameState == GameState.Normal)
            {
                player.vt.SetCursor(36, 0);
                player.vt.WriteText((player1.points < 10 ? " " + player1.points.ToString() : player1.points.ToString()) + " : " + player2.points.ToString());
            }

            player.vt.SetBackgroundColor(player.colorPalette.Paddle);
            player.vt.DrawLine(0, FindPlayerEnum(PlayerEnum.Player1).paddle + 1, VT100.Direction.Vertical, 4);
            if (gameState == GameState.Normal)
                player.vt.DrawLine(79, FindPlayerEnum(PlayerEnum.Player2).paddle + 1, VT100.Direction.Vertical, 4);

            player.vt.SetBackgroundColor(player.colorPalette.Ball);
            player.vt.DrawLine(ballX, ballY + 1, VT100.Direction.Horizontal, 2);
        }

        private void HandleInput()
        {
            for (int i = players.Count - 1; i >= 0; i--)
            {
                HandleInput(players[i]);
            }
        }

        private void HandleInput(PlayerClass player)
        {
            char? temp;
            while ((temp = player.vt.ReadChar()) != null)
            {
                if (player.playerType == PlayerType.Player)
                {
                    if (temp == 'A' || temp == 'a')
                        player.paddle--;
                    if (temp == 'Z' || temp == 'z')
                        player.paddle++;
                    if (temp == 'E' || temp == 'e')
                    {
                        RemovePlayer(player);
                        throw new PlayerRemovedException();
                    }
                }
                else if (temp == 'E' || temp == 'e')
                {
                    RemovePlayer(player);
                    throw new PlayerRemovedException();
                }
            }
        }

        private void ComputeLogic()
        {
            PlayerClass player1 = FindPlayerEnum(PlayerEnum.Player1);
            PlayerClass player2 = FindPlayerEnum(PlayerEnum.Player2);

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
                        if (gameState == GameState.Training || (player2.paddle - 1 <= ballY && ballY < player2.paddle + 5))
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
                        if (gameState == GameState.Training || (player2.paddle - 1 <= ballY && ballY < player2.paddle + 5))
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
                        if (player1.paddle - 1 <= ballY && ballY < player1.paddle + 5)
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
                        if (player1.paddle - 1 <= ballY && ballY < player1.paddle + 5)
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

        private void VerifyPositions()
        {
            PlayerClass player1 = FindPlayerEnum(PlayerEnum.Player1);
            PlayerClass player2 = FindPlayerEnum(PlayerEnum.Player2);

            if (player1.paddle < 0)
                player1.paddle = 0;
            if (player1.paddle > 18)
                player1.paddle = 18;
            if (gameState == GameState.Normal)
            {
                if (player2.paddle < 0)
                    player2.paddle = 0;
                if (player2.paddle > 18)
                    player2.paddle = 18;
            }
        }

        private void Bell()
        {
            for (int i = players.Count - 1; i >= 0; i--)
                players[i].vt.Bell();
        }

        private void ScorePoint(PlayerClass player)
        {
            Bell();
            if (gameState == GameState.Normal)
            {
                player.points++;
                ResetPositions(player.playerEnum);
            }
            else
                ResetPositions();
        }

        private void RemovePlayer(PlayerClass player)
        {
            players.Remove(player);
            if (player.playerType == PlayerType.Player)
            {
                playerCount--;
                ResetPositions();
            }
            player.vt.SetBackgroundColor(new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.Black });
            player.vt.SetForegroundColor(new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.White });
            player.vt.SetCursor(0, 0);
            player.vt.SetCursorVisiblity(true);
            player.vt.ClearScreen();
            if (player.vt.Flush() == VT100.FlushReturnState.Success)
                PlayerLeftRaise(player, false);
            else
                PlayerLeftRaise(player, true);
            if (player.playerType == PlayerType.Player)
            {
                if (playerCount == 1)
                {
                    gameState = GameState.Training;
                    UpdateInfo(PlayerType.Player, "CONTROLS: A and Z keys, E to exit. WAITING FOR PLAYER...");
                    if (player.playerEnum == PlayerEnum.Player1)
                        FindPlayerEnum(PlayerEnum.Player2).playerEnum = PlayerEnum.Player1;
                }
                if (playerCount == 0)
                {
                    gameState = GameState.NotStarted;
                    KillGame();
                }
            }
        }

        private PlayerClass FindPlayerEnum(PlayerEnum playerEnum)
        {
            foreach (PlayerClass player in players)
                if (player.playerEnum == playerEnum)
                    return player;
            return null;
        }
    }
}
