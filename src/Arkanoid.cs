using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TelnetGames
{
    class Arkanoid : Game
    {
        private new class PlayerClass : Game.PlayerClass
        {
            public int points = 0;
            public int lives = 2;
            public int paddle = 14;
            public ColorPalette colorPalette;

            private static readonly ColorPalette compatibilityColorPalette = new CompatibilityColorPalette();
            private static readonly ColorPalette standardColorPalette = new StandardColorPalette();

            public PlayerClass(Game.PlayerClass gamePlayer)
            {
                playerType = gamePlayer.playerType;
                vt = gamePlayer.vt;
                supportAixtermColors = gamePlayer.supportAixtermColors;
                UpdatePalette();
            }

            public void UpdatePalette()
            {
                if (supportAixtermColors)
                    colorPalette = standardColorPalette;
                else
                    colorPalette = compatibilityColorPalette;
            }
        }

        private abstract class ColorPalette
        {
            public VT100.ColorClass Background;
            public VT100.ColorClass Band;
            public VT100.ColorClass Paddle;
            public VT100.ColorClass Ball;
            public VT100.ColorClass Text;
            public VT100.ColorClass[] Brick;
        }

        private class StandardColorPalette : ColorPalette
        {
            public StandardColorPalette()
            {
                Background = new VT100.ColorClass { Bright = true, Color = VT100.ColorEnum.Blue };
                Band = new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.Yellow };
                Paddle = new VT100.ColorClass { Bright = true, Color = VT100.ColorEnum.Green };
                Ball = new VT100.ColorClass { Bright = true, Color = VT100.ColorEnum.Yellow };
                Text = new VT100.ColorClass { Bright = true, Color = VT100.ColorEnum.Yellow };
                Brick = new VT100.ColorClass[8]
                {
                    new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.Yellow },
                    new VT100.ColorClass { Bright = true, Color = VT100.ColorEnum.Cyan },
                    new VT100.ColorClass { Bright = true, Color = VT100.ColorEnum.Green },
                    new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.Red },
                    new VT100.ColorClass { Bright = true, Color = VT100.ColorEnum.Blue },
                    new VT100.ColorClass { Bright = true, Color = VT100.ColorEnum.Magneta },
                    new VT100.ColorClass { Bright = true, Color = VT100.ColorEnum.Yellow },
                    new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.White }
                };
            }
        }

        private class CompatibilityColorPalette : ColorPalette
        {
            public CompatibilityColorPalette()
            {
                Background = new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.Black };
                Band = new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.Yellow };
                Paddle = new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.Green };
                Ball = new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.White };
                Text = new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.White };
                Brick = new VT100.ColorClass[8]
                {
                    new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.Cyan },
                    new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.Cyan },
                    new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.Cyan },
                    new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.Cyan },
                    new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.Cyan },
                    new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.Cyan },
                    new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.Cyan },
                    new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.Cyan }
                };
            }
        }

        private enum GameState
        {
            Stopped,
            Intro,
            Game,
            Outro,
            Highscore
        }

        private class PlayerRemovedException : ApplicationException { }

        private static readonly string Texts = "\nTHE TIME AND ERA OF THIS STORY IS UNKNOWN.\nAFTER THE MOTHERSHIP \"ARKANOID\" WAS DESTROYED,\nA SPACECRAFT \"VAUS\" SCRAMBLED AWAY FROM IT.\nBUT ONLY TO BE TRAPPED IN SPACE WARPED BY SOMEONE........\x00\nDIMENSION-CONTROLLING FORT \"DOH\" HAS NOW BEEN DEMOLISHED,\nAND TIME STARTED FLOWING REVERSELY.\n\"VAUS\" MANAGED TO ESCAPE FROM THE DISTORTED SPACE.\nBUT THE REAL VOYAGE OF \"ARKANOID\" IN THE GALAXY HAS ONLY STARTED......\x00";

        private PlayerClass currentPlayer;
        private List<PlayerClass> players = new List<PlayerClass>();

        public override int MinPlayers { get { return 1; } }
        public override int MaxPlayers { get { return 1; } }
        public override int PlayerCount { get { return (currentPlayer != null ? 1 : 0); } }

        private GameState gameState = GameState.Stopped;
        private int textPos = 0;
        private int textY = 7;
        private int ballX = 2;
        private int ballY = 2;
        private int ballXmov = 1;
        private int ballYmov = 1;
        private int[,] bricks = new int[,]
        {
            {6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 6},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0},
            {0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0},
            {0, 0, 1, 0, 3, 3, 3, 0, 1, 0, 0},
            {0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0},
            {6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 6},
            {0, 0, 0, 0, 8, 8, 8, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 6}
        };

        private StringBuilder input = new StringBuilder();
        private StringBuilder name = new StringBuilder();

        public override void Tick()
        {
            if (gameState == GameState.Stopped)
                return;
            try
            {
                HandleInput();
                switch (gameState)
                {
                    case GameState.Intro:
                    case GameState.Outro:
                        char letter = Texts[textPos];
                        if (letter == '\n')
                        {
                            currentPlayer.vt.SetCursor(3, textY);
                            textY++;
                        }
                        if (letter == 0)
                        {
                            if (gameState == GameState.Intro)
                                gameState = GameState.Game;
                            if (gameState == GameState.Outro)
                                gameState = GameState.Highscore;
                            break;
                        }
                        currentPlayer.vt.WriteText(letter.ToString());
                        textPos++;
                        break;
                    case GameState.Game:
                        ComputeLogic();
                        RenderFrame();
                        break;
                    case GameState.Highscore:
                        if (input.Length != 0)
                        {
                            string s = input.ToString();
                            foreach (char c in s)
                            {
                                if (c >= 65 && c <= 122 && name.Length < 10)
                                {
                                    name.Append(c);
                                    currentPlayer.vt.WriteText(c.ToString());
                                }
                                if ((c == 8 || c == 127) && name.Length > 0)
                                {
                                    currentPlayer.vt.WriteText("\x08 \x08");
                                    name.Remove(name.Length - 1, 1);
                                }
                                if (c == 13)
                                {
                                    gameState = GameState.Game;
                                    currentPlayer.lives = 2;
                                    if (name.Length == 0)
                                        name.Append("ANONYM");
                                    Console.WriteLine(name + ": " + currentPlayer.points);
                                }
                            }
                        }
                        currentPlayer.vt.WriteText("\x00");
                        break;
                }
                Flush();
            }
            catch (PlayerRemovedException) { }
        }

        public override void AddPlayer(Game.PlayerClass player)
        {
            currentPlayer = new PlayerClass(player);
            players.Add(currentPlayer);
            currentPlayer.vt.Bell();
            currentPlayer.vt.SetCursorVisiblity(true);
            currentPlayer.vt.SetBackgroundColor(currentPlayer.colorPalette.Background);
            currentPlayer.vt.SetForegroundColor(currentPlayer.colorPalette.Text);
            currentPlayer.vt.ClearScreen();
            Flush();
            gameState = GameState.Game;
        }

        public override void KillGame()
        {
            for (int i = players.Count - 1; i >= 0; i--)
            {
                RemovePlayer(players[i]);
            }
            GameKilledRaise();
            throw new PlayerRemovedException();
        }

        private void RemovePlayer(PlayerClass player)
        {
            players.Remove(player);
            if (player.playerType == PlayerType.Player)
            {
                currentPlayer = null;
                gameState = GameState.Stopped;
            }
            if (player.vt.Flush() == VT100.FlushReturnState.Success)
                PlayerLeftRaise(player, false);
            else
                PlayerLeftRaise(player, true);
            if (player.playerType == PlayerType.Player)
                if (currentPlayer == null)
                    KillGame();
        }

        private void ComputeLogic()
        {
            if (currentPlayer.paddle < 1)
                currentPlayer.paddle = 1;
            if (currentPlayer.paddle > 33)
                currentPlayer.paddle = 33;

            ballX += ballXmov;
            ballY += ballYmov;

            if (ballX == 38 || ballX == 1)
            {
                ballXmov = -ballXmov;
                Bell();
            }
            if (ballY == 1 || (ballY == 22 && ballX > currentPlayer.paddle - 1 && ballX < currentPlayer.paddle + 7))
            {
                ballYmov = -ballYmov;
                Bell();
            }
            if (ballY == 23)
            {
                BallLost();
            }
        }

        private void BallLost()
        {
            ballX = 2;
            ballY = 2;
            ballXmov = 1;
            ballYmov = 1;
            currentPlayer.paddle = 14;
            currentPlayer.lives--;
            if (currentPlayer.lives < 0)
            {
                gameState = GameState.Highscore;
                name.Clear();
                for (int i = players.Count - 1; i >= 0; i--)
                {
                    players[i].vt.SetBackgroundColor(players[i].colorPalette.Background);
                    players[i].vt.ClearScreen();
                    players[i].vt.SetForegroundColor(players[i].colorPalette.Text);
                    players[i].vt.SetCursor(0, 0);
                    players[i].vt.SetCursorVisiblity(true);
                }
            }
            Bell();
            throw new PlayerRemovedException();
        }

        private void RenderFrame(PlayerClass player)
        {
            player.vt.SetCursorVisiblity(false);
            player.vt.SetBackgroundColor(player.colorPalette.Background);
            player.vt.ClearScreen();
            for (int y = 0; y < 22; y++)
            {
                for (int x = 0; x < 11; x++)
                {
                    if (bricks[y, x] != 0)
                    {
                        player.vt.SetBackgroundColor(player.colorPalette.Brick[(bricks[y, x] > 8 ? 8 : bricks[y, x]) - 1]);
                        player.vt.DrawLine(x * 6 + 2, y + 1, VT100.Direction.Horizontal, 6);
                    }
                }
            }
            player.vt.SetBackgroundColor(player.colorPalette.Band);
            player.vt.DrawLine(0, 0, VT100.Direction.Vertical, 24);
            player.vt.DrawLine(1, 0, VT100.Direction.Vertical, 24);
            player.vt.DrawLine(78, 0, VT100.Direction.Vertical, 24);
            player.vt.DrawLine(79, 0, VT100.Direction.Vertical, 24);
            player.vt.DrawLine(2, 0, VT100.Direction.Horizontal, 78);
            player.vt.SetBackgroundColor(player.colorPalette.Ball);
            player.vt.DrawLine(ballX * 2, ballY, VT100.Direction.Horizontal, 2);
            player.vt.SetBackgroundColor(player.colorPalette.Paddle);
            player.vt.DrawLine(currentPlayer.paddle * 2, 24, VT100.Direction.Horizontal, 12);
        }

        private void HandleInput(PlayerClass player)
        {
            input.Clear();
            char? temp;
            while ((temp = player.vt.ReadChar()) != null)
            {
                if (player.playerType == PlayerType.Player)
                {
                    if (gameState != GameState.Highscore)
                    {
                        if (temp == 'Z' || temp == 'z')
                            player.paddle--;
                        if (temp == 'X' || temp == 'x')
                            player.paddle++;
                        if (temp == ' ')
                        {
                            if (gameState == GameState.Intro)
                                gameState = GameState.Game;
                            if (gameState == GameState.Outro)
                                gameState = GameState.Highscore;
                        }
                        if (temp == 'C' || temp == 'c')
                        {
                            player.supportAixtermColors = !player.supportAixtermColors;
                            player.UpdatePalette();
                        }
                        if (temp == 'E' || temp == 'e')
                        {
                            RemovePlayer(player);
                            throw new PlayerRemovedException();
                        }
                    }
                    else
                        input.Append(temp);
                }
                else if (temp == 'E' || temp == 'e')
                {
                    RemovePlayer(player);
                    throw new PlayerRemovedException();
                }
            }
        }

        private void RenderFrame()
        {
            for (int i = players.Count - 1; i >= 0; i--)
                RenderFrame(players[i]);
        }


        private void HandleInput()
        {
            for (int i = players.Count - 1; i >= 0; i--)
                HandleInput(players[i]);
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
                    break;
                case VT100.FlushReturnState.Error:
                    RemovePlayer(player);
                    throw new PlayerRemovedException();
            }
        }

        private void Bell()
        {
            for (int i = players.Count - 1; i >= 0; i--)
                players[i].vt.Bell();
        }
    }
}