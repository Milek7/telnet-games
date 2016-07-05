using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TelnetGames
{
    class Tetris : Game
    {
        private new class PlayerClass : Game.PlayerClass
        {
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
            public VT100.ColorClass Text;
            public VT100.ColorClass[] Pieces;
        }

        private class StandardColorPalette : ColorPalette
        {
            public StandardColorPalette()
            {
                Background = new VT100.ColorClass { Bright = true, Color = VT100.ColorEnum.Blue };
                Band = new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.Yellow };
                Text = new VT100.ColorClass { Bright = true, Color = VT100.ColorEnum.Yellow };
                Pieces = new VT100.ColorClass[7]
                {
                    new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.Cyan },
                    new VT100.ColorClass { Bright = true, Color = VT100.ColorEnum.Cyan },
                    new VT100.ColorClass { Bright = true, Color = VT100.ColorEnum.Green },
                    new VT100.ColorClass { Bright = true, Color = VT100.ColorEnum.Red },
                    new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.White },
                    new VT100.ColorClass { Bright = true, Color = VT100.ColorEnum.Magneta },
                    new VT100.ColorClass { Bright = true, Color = VT100.ColorEnum.Yellow }
                };
            }
        }

        private class CompatibilityColorPalette : ColorPalette
        {
            public CompatibilityColorPalette()
            {
                Background = new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.Black };
                Band = new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.Yellow };
                Text = new VT100.ColorClass { Bright = false, Color = VT100.ColorEnum.White };
                Pieces = new VT100.ColorClass[7]
                {
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
            Game,
            Highscore
        }

        private class PlayerRemovedException : ApplicationException { }

        private PlayerClass currentPlayer;
        private List<PlayerClass> players = new List<PlayerClass>();

        public override int MinPlayers { get { return 1; } }
        public override int MaxPlayers { get { return 1; } }
        public override int PlayerCount { get { return (currentPlayer != null ? 1 : 0); } }

        private GameState gameState = GameState.Stopped;
        private byte[,] board = new byte[20, 10];

        private static byte[][][,] blocks = new byte[][][,]
        {
            new byte[][,]
            {
                new byte[4, 4]
                {
                    {0, 0, 0, 0},
                    {0, 1, 1, 0},
                    {0, 1, 1, 0},
                    {0, 0, 0, 0}
                }
            },
            new byte[][,]
            {
                new byte[4, 4]
                {
                    {0, 0, 0, 0},
                    {2, 2, 2, 2},
                    {0, 0, 0, 0},
                    {0, 0, 0, 0}
                },
                new byte[4, 4]
                {
                    {0, 0, 2, 0},
                    {0, 0, 2, 0},
                    {0, 0, 2, 0},
                    {0, 0, 2, 0}
                }
            },
            new byte[][,]
            {
                new byte[4, 4]
                {
                    {0, 0, 0, 0},
                    {0, 0, 3, 3},
                    {0, 3, 3, 0},
                    {0, 0, 0, 0}
                },
                new byte[4, 4]
                {
                    {0, 0, 3, 0},
                    {0, 0, 3, 3},
                    {0, 0, 0, 3},
                    {0, 0, 0, 0}
                }
            },
            new byte[][,]
            {
                new byte[4, 4]
                {
                    {0, 0, 0, 0},
                    {0, 4, 4, 0},
                    {0, 0, 4, 4},
                    {0, 0, 0, 0}
                },
                new byte[4, 4]
                {
                    {0, 0, 0, 4},
                    {0, 0, 4, 4},
                    {0, 0, 4, 0},
                    {0, 0, 0, 0}
                }
            },
            new byte[][,]
            {
                new byte[4, 4]
                {
                    {0, 0, 0, 0},
                    {0, 5, 5, 5},
                    {0, 5, 0, 0},
                    {0, 0, 0, 0}
                },
                new byte[4, 4]
                {
                    {0, 0, 5, 0},
                    {0, 0, 5, 0},
                    {0, 0, 5, 5},
                    {0, 0, 0, 0}
                },
                 new byte[4, 4]
                {
                    {0, 0, 0, 5},
                    {0, 5, 5, 5},
                    {0, 0, 0, 0},
                    {0, 0, 0, 0}
                },
                new byte[4, 4]
                {
                    {0, 5, 5, 0},
                    {0, 0, 5, 0},
                    {0, 0, 5, 0},
                    {0, 0, 0, 0}
                }
            },
            new byte[][,]
            {
                new byte[4, 4]
                {
                    {0, 0, 0, 0},
                    {0, 6, 6, 6},
                    {0, 0, 0, 6},
                    {0, 0, 0, 0}
                },
                new byte[4, 4]
                {
                    {0, 0, 6, 6},
                    {0, 0, 6, 0},
                    {0, 0, 6, 0},
                    {0, 0, 0, 0}
                },
                new byte[4, 4]
                {
                    {0, 6, 0, 0},
                    {0, 6, 6, 6},
                    {0, 0, 0, 0},
                    {0, 0, 0, 0}
                },
                new byte[4, 4]
                {
                    {0, 0, 6, 0},
                    {0, 0, 6, 0},
                    {0, 6, 6, 0},
                    {0, 0, 0, 0}
                }
            },
            new byte[][,]
            {
                new byte[4, 4]
                {
                    {0, 0, 0, 0},
                    {0, 7, 7, 7},
                    {0, 0, 7, 0},
                    {0, 0, 0, 0}
                },
                new byte[4, 4]
                {
                    {0, 0, 7, 0},
                    {0, 0, 7, 7},
                    {0, 0, 7, 0},
                    {0, 0, 0, 0}
                },
                 new byte[4, 4]
                {
                    {0, 0, 7, 0},
                    {0, 7, 7, 7},
                    {0, 0, 0, 0},
                    {0, 0, 0, 0}
                },
                new byte[4, 4]
                {
                    {0, 0, 7, 0},
                    {0, 7, 7, 0},
                    {0, 0, 7, 0},
                    {0, 0, 0, 0}
                }
            },
        };

        private int? block;
        private int blockRotation;
        private int blockX;
        private int blockY;
        private int wait;
        private int lines;
        private int level;
        private int points;
        private int freefall;
        private Random random = new Random();
        private StringBuilder input = new StringBuilder();
        private StringBuilder name = new StringBuilder();

        private int LevelCompute(int lines)
        {
            if (lines <= 0)
                return 1;
            else if ((lines >= 1) && (lines <= 90))
                return 1 + ((lines - 1) / 10);
            else if (lines >= 91)
                return 10;
            return 0;
        }

        public override void Tick()
        {
            if (gameState == GameState.Stopped)
                return;
            try
            {
                HandleInput();
                if (gameState == GameState.Game)
                {
                    level = LevelCompute(lines);
                    if (wait == 0)
                    {
                        wait = 10 - level;
                        ComputeLogic();
                    }
                    else
                        wait--;
                    RenderFrame();
                }
                else if (gameState == GameState.Highscore)
                {
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
                                if (name.Length == 0)
                                    name.Append("ANONYM");
                                SaveHighscore(new KeyValuePair<string, int[]>(name.ToString(), new int[] { lines, points }));
                                gameState = GameState.Stopped;
                                KillGame();
                            }
                        }
                    }
                    currentPlayer.vt.WriteText("\x00");
                }
                Flush();
            }
            catch (PlayerRemovedException) { }
        }

        static private int Compare(KeyValuePair<string, int[]> a, KeyValuePair<string, int[]> b)
        {
            return b.Key[1].CompareTo(a.Key[1]);
        }
        
        private void SaveHighscore(KeyValuePair<string, int[]> highscore)
        {
            FileStream fileR = new FileStream("tetris-highscores", FileMode.Open);
            StreamReader reader = new StreamReader(fileR);
            List<KeyValuePair<string, int[]>> table = JsonConvert.DeserializeObject<List<KeyValuePair<string, int[]>>>(reader.ReadToEnd());
            reader.Close();
            table.Add(highscore);
            table.Sort(Compare);
            FileStream fileW = new FileStream("tetris-highscores", FileMode.Create);
            StreamWriter writer = new StreamWriter(fileW);
            writer.Write(JsonConvert.SerializeObject(table));
            writer.Close();
        }

        public override void AddPlayer(Game.PlayerClass player)
        {
            currentPlayer = new PlayerClass(player);
            players.Add(currentPlayer);
            currentPlayer.vt.Bell();
            currentPlayer.vt.SetCursorVisiblity(false);
            Flush();
            gameState = GameState.Game;
        }

        public override void KillGame()
        {
            if (gameState == GameState.Game)
                SaveHighscore(new KeyValuePair<string, int[]>("ANONYM", new int[] { lines, points }));
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
            if (block == null || freefall++ < 0 || !LineDown())
            {
                block = random.Next(blocks.Length);
                blockRotation = 0;
                blockX = 5;
                blockY = 0;
                freefall = 0;
                if (CheckCollision())
                {
                    RenderHighscore();
                    //KillGame();
                    Flush();
                    throw new PlayerRemovedException();
                }
            }
        }

        private void RenderHighscore()
        {
            FileStream file = new FileStream("tetris-highscores", FileMode.Open);
            StreamReader reader = new StreamReader(file);
            List<KeyValuePair<string, int[]>> table = JsonConvert.DeserializeObject<List<KeyValuePair<string, int[]>>>(reader.ReadToEnd());
            reader.Close();
            currentPlayer.vt.SetBackgroundColor(currentPlayer.colorPalette.Background);
            currentPlayer.vt.SetForegroundColor(currentPlayer.colorPalette.Text);
            currentPlayer.vt.ClearScreen();
            currentPlayer.vt.SetCursor(30, 4);
            currentPlayer.vt.WriteText("TETRIS HIGHSCORES");
            currentPlayer.vt.SetCursor(15, 6);
            currentPlayer.vt.WriteText("NICKNAME            LEVEL     LINES     SCORE");
            for (int i = 0; i < table.Count; i++)
            {
                if (i >= 10)
                    break;
                currentPlayer.vt.SetCursor(15, 7 + i);
                currentPlayer.vt.WriteText(table[i].Key);
                currentPlayer.vt.SetCursor(35, 7 + i);
                currentPlayer.vt.WriteText(LevelCompute(table[i].Value[0]).ToString());
                currentPlayer.vt.SetCursor(45, 7 + i);
                currentPlayer.vt.WriteText(table[i].Value[0].ToString());
                currentPlayer.vt.SetCursor(55, 7 + i);
                currentPlayer.vt.WriteText(table[i].Value[1].ToString());
            }
            currentPlayer.vt.SetCursor(15, 19);
            currentPlayer.vt.WriteText("Enter nickname (leave empty for ANONYM):");
            currentPlayer.vt.SetCursor(35, 20);
            currentPlayer.vt.WriteText(level.ToString());
            currentPlayer.vt.SetCursor(45, 20);
            currentPlayer.vt.WriteText(lines.ToString());
            currentPlayer.vt.SetCursor(55, 20);
            currentPlayer.vt.WriteText(points.ToString());
            currentPlayer.vt.SetCursor(15, 20);
            currentPlayer.vt.SetCursorVisiblity(true);
            gameState = GameState.Highscore;
        }

        private bool LineDown()
        {
            blockY++;
            if (CheckCollision())
            {
                blockY--;
                HardenizeBlock();
                return false;
            }
            return true;
        }

        private void HardenizeBlock()
        {
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    if (blocks[(int)block][blockRotation][y, x] != 0)
                    {
                        int mX = blockX + x - 2;
                        int mY = blockY + y - 1;
                        board[mY, mX] = blocks[(int)block][blockRotation][y, x];
                    }
                }
            }
            points += ((21 + (3 * level)) - freefall);
            block = null;
            for (int y = 19; y >= 0; y--)
            {
                for (int x = 9; x >= 0; x--)
                {
                    if (board[y, x] == 0)
                        goto nextLine;
                }
                for (int yc = y; yc > 0; yc--)
                {
                    for (int x = 0; x < 10; x++)
                        board[yc, x] = board[yc - 1, x];
                }
                lines++;
                y++;
            nextLine:;
            }
        }

        private bool CheckCollision()
        {
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                {
                    if (blocks[(int)block][blockRotation][y, x] != 0)
                    {
                        int mX = blockX + x - 2;
                        int mY = blockY + y - 1;
                        if (mX < 0 || mX > 9 || mY < 0 || mY > 19 || board[mY, mX] != 0)
                            return true;
                    }
                }
            return false;
        }

        private void RenderFrame(PlayerClass player)
        {
            player.vt.SetBackgroundColor(player.colorPalette.Background);
            player.vt.ClearScreen();
            player.vt.SetBackgroundColor(player.colorPalette.Band);
            player.vt.DrawLine(4, 1, VT100.Direction.Horizontal, 22);
            player.vt.DrawLine(4, 22, VT100.Direction.Horizontal, 22);
            player.vt.DrawLine(3, 1, VT100.Direction.Vertical, 22);
            player.vt.DrawLine(4, 1, VT100.Direction.Vertical, 22);
            player.vt.DrawLine(25, 1, VT100.Direction.Vertical, 22);
            player.vt.DrawLine(26, 1, VT100.Direction.Vertical, 22);

            const int offsetX = 5;
            const int offsetY = 2;
            int lastColor = -2;

            for (int y = 0; y < 20; y++)
            {
                int freespace = 0;
                player.vt.SetCursor(offsetX, offsetY + y);
                for (int x = 0; x < 10; x++)
                {
                    int val;
                    if (block != null)
                    {
                        int mX = x - (blockX - 2);
                        int mY = y - (blockY - 1);
                        if (mX >= 0 && mX <= 3 && mY >= 0 && mY <= 3 && (val = blocks[(int)block][blockRotation][mY, mX]) != 0)
                        {
                            lastColor = DrawTile(player, lastColor, val, freespace, x, y);
                            freespace = 0;
                            continue;
                        }
                    }
                    if ((val = board[y, x]) != 0)
                    {
                        lastColor = DrawTile(player, lastColor, val, freespace, x, y);
                        freespace = 0;
                        continue;
                    }
                    freespace++;
                }
            }

            player.vt.SetBackgroundColor(player.colorPalette.Background);
            player.vt.SetForegroundColor(player.colorPalette.Text);
            player.vt.SetCursor(33, 2);
            player.vt.WriteText("Score: " + points);
            player.vt.SetCursor(33, 3);
            player.vt.WriteText("Level: " + level);
            player.vt.SetCursor(33, 4);
            player.vt.WriteText("Lines: " + lines);
        }

        private int DrawTile(PlayerClass player, int lastColor, int color, int freespace, int x, int y)
        {
            if (freespace == 1)
            {
                if (lastColor != -1)
                {
                    lastColor = -1;
                    player.vt.SetBackgroundColor(player.colorPalette.Background);
                }
                player.vt.WriteText("  ");
            }
            if (freespace > 1)
                player.vt.SetCursor(5 + x * 2, 2 + y);
            if (lastColor != color)
            {
                lastColor = color;
                player.vt.SetBackgroundColor(player.colorPalette.Pieces[color - 1]);
            }
            player.vt.WriteText("  ");
            return lastColor;
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
                        if ((temp == 'Z' || temp == 'z') && block != null)
                        {
                            blockX--;
                            if (CheckCollision())
                                blockX++;
                        }
                        if ((temp == 'X' || temp == 'x') && block != null)
                        {
                            blockX++;
                            if (CheckCollision())
                                blockX--;
                        }
                        if ((temp == 'A' || temp == 'a' || temp == 'N' || temp == 'n') && block != null)
                        {
                            blockRotation++;
                            if (blockRotation > blocks[(int)block].Length - 1)
                                blockRotation = 0;
                            if (CheckCollision())
                                blockRotation--;
                            if (blockRotation == -1)
                                blockRotation = blocks[(int)block].Length - 1;
                        }
                        if ((temp == 'S' || temp == 's' || temp == 'M' || temp == 'm') && block != null)
                        {
                            while (LineDown()) ;
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