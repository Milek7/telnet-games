/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TelnetGames
{
    class Breakout : Game
    {
        private new class PlayerClass : Game.PlayerClass
        {
            public int points = 0;
            public int lives = 3;
            public int paddle = 0;
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
            Game
        }

        private class PlayerRemovedException : ApplicationException { }

        private PlayerClass currentPlayer;
        private List<PlayerClass> players = new List<PlayerClass>();

        public override int MinPlayers { get { return 1; } }
        public override int MaxPlayers { get { return 1; } }
        public override int PlayerCount { get { return (currentPlayer != null ? 1 : 0); } }

        private GameState gameState = GameState.Stopped;
        private int ballX = 10;
        private int ballY = 12;
        private int ballXmov = 0;
        private int ballYmov = 0;
        private int paddleSize = 6;
        private bool frame = false;
        private int[,] bricks = new int[22, 13]
        {
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 1 ,1, 1},
            {4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4 ,4, 4},
            {4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4 ,4, 4},
            {3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3 ,3, 3},
            {2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 ,2, 2},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
        };
        private int blocks = 65;

        public override void Tick()
        {
            if (gameState == GameState.Stopped)
                return;
            try
            {
                HandleInput();
                ComputeLogic();
                RenderFrame();
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
            currentPlayer.paddle = 19 - (paddleSize / 2);
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
            if (currentPlayer.paddle < 0)
                currentPlayer.paddle = 0;
            if (currentPlayer.paddle > 33)
                currentPlayer.paddle = 33;

            if (ballXmov == 0 && ballYmov == 0)
            {
                ballX = currentPlayer.paddle + (paddleSize / 2);
                ballY = 21;
            }

            if (frame == true)
            {
                frame = false;
                return;
            }
            else
                frame = true;

            if (ballX == 38 || ballX == 0)
            {
                ballXmov = -ballXmov;
                Bell();
            }
            if (ballY == 0)
            {
                ballYmov = -ballYmov;
                Bell();
            }
            if (ballY == 22)
            {
                BallLost();
            }

            CheckCollisionPaddle(ballX, ballY + ballYmov, false, true);
            CheckCollisionPaddle(ballX + ballXmov, ballY, true, false);
            CheckCollisionPaddle(ballX + ballXmov, ballY + ballYmov, true, true);

            CheckCollisionBrick(ballX / 3, ballY + ballYmov, false, true);
            CheckCollisionBrick((ballX + ballXmov) / 3, ballY, true, false);
            CheckCollisionBrick((ballX + ballXmov) / 3, ballY + ballYmov, true, true);
            
            ballX += ballXmov;
            ballY += ballYmov;
        }

        private bool CheckCollisionBrick(int X, int Y, bool invertX, bool invertY)
        {
            if (X >= 0 && X <= 12 && Y >= 0 && Y <= 21)
                if (bricks[Y, X] != 0)
                {
                    Bell();
                    if (bricks[Y, X] > 8)
                        bricks[Y, X]--;
                    else if (bricks[Y, X] != 7)
                    {
                        bricks[Y, X] = 0;
                        blocks--;
                        if (blocks == 0)
                            KillGame();
                    }
                    if (invertX)
                        ballXmov = -ballXmov;
                    if (invertY)
                        ballYmov = -ballYmov;
                    return true;
                }
            return false;
        }

        private bool CheckCollisionPaddle(int X, int Y, bool invertX, bool invertY)
        {
            if (X >= 0 && X <= 38 && Y >= 0 && Y <= 23)
                if (X >= currentPlayer.paddle && X < currentPlayer.paddle + paddleSize && Y == 22)
                {
                    Bell();
                    if (invertX)
                        ballXmov = -ballXmov;
                    if (invertY)
                        ballYmov = -ballYmov;
                    return true;
                }
            return false;
        }

        private void BallLost()
        {
            ballXmov = 0;
            ballYmov = 0;
            currentPlayer.lives--;
            if (currentPlayer.lives < 0)
            {
                currentPlayer.lives = 2;
            }
            Bell();
            throw new PlayerRemovedException();
        }

        private void RenderFrame(PlayerClass player)
        {
            player.vt.SetCursorVisiblity(false);
            if (frame == false)
            {
                player.vt.SetBackgroundColor(player.colorPalette.Background);
                player.vt.ClearLine();
                player.vt.SetCursor(0, 24);
                player.vt.SetBackgroundColor(player.colorPalette.Band);
                player.vt.WriteText(" ");
                player.vt.SetCursor(79, 24);
                player.vt.WriteText(" ");
                player.vt.SetBackgroundColor(player.colorPalette.Paddle);
                player.vt.DrawLine(currentPlayer.paddle * 2 + 1, 24, VT100.Direction.Horizontal, paddleSize * 2);
                return;
            }
            player.vt.SetBackgroundColor(player.colorPalette.Background);
            player.vt.ClearScreen();
            for (int y = 0; y < 22; y++)
            {
                for (int x = 0; x < 13; x++)
                {
                    if (bricks[y, x] != 0)
                    {
                        player.vt.SetBackgroundColor(player.colorPalette.Brick[(bricks[y, x] > 8 ? 8 : bricks[y, x]) - 1]);
                        player.vt.DrawLine(x * 6 + 1, y + 1, VT100.Direction.Horizontal, 6);
                    }
                }
            }
            player.vt.SetBackgroundColor(player.colorPalette.Band);
            player.vt.DrawLine(0, 0, VT100.Direction.Vertical, 24);
            player.vt.DrawLine(79, 0, VT100.Direction.Vertical, 24);
            player.vt.DrawLine(1, 0, VT100.Direction.Horizontal, 79);
            player.vt.SetBackgroundColor(player.colorPalette.Ball);
            player.vt.DrawLine(ballX * 2 + 1, ballY + 1, VT100.Direction.Horizontal, 2);
            player.vt.SetBackgroundColor(player.colorPalette.Paddle);
            player.vt.DrawLine(currentPlayer.paddle * 2 + 1, 24, VT100.Direction.Horizontal, paddleSize * 2);
        }

        private void HandleInput(PlayerClass player)
        {
            char? temp;
            while ((temp = player.vt.ReadChar()) != null)
            {
                if (player.playerType == PlayerType.Player)
                {
                    if (temp == 'Z' || temp == 'z')
                        player.paddle--;
                    if (temp == 'X' || temp == 'x')
                        player.paddle++;
                    if (temp == 'A' || temp == 'a')
                        if (ballXmov == 0 && ballYmov == 0)
                        {
                            ballXmov = 1;
                            ballYmov = 1;
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