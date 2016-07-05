/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelnetGames
{
    class Lobby : Game
    {
        private List<PlayerClass> players = new List<PlayerClass>();

        public override int MinPlayers { get { return 0; } }
        public override int MaxPlayers { get { return -1; } }
        public override int PlayerCount { get { return players.Count; } }

        private int keepAlive = 0;
        private int gameCount = 0;
        private int averageFrameProcessingTime = 0;

        public override void Tick()
        {
            HandleInput();
            if (keepAlive > 0)
                keepAlive--;
            else
            {
                keepAlive = 100;
                gameCount = Program.GameCount - 1;
                averageFrameProcessingTime = Program.AverageFrameProcessingTime;
                RenderFrame(false);
            }
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
                if (temp == '1')
                {
                    players.Remove(player);
                    PlayerHandoffRaise(typeof(Pong), player);
                    break;
                }
                if (temp == '2')
                {
                    players.Remove(player);
                    PlayerHandoffRaise(typeof(Breakout), player);
                    break;
                }
                if (temp == '3')
                {
                    players.Remove(player);
                    PlayerHandoffRaise(typeof(Tetris), player);
                    break;
                }
				/*
				if (temp == '4')
				{
					players.Remove(player);
					PlayerHandoffRaise(typeof(Curve), player);
					break;
				}
				*/
                if (temp == 'E' || temp == 'e')
                {
                    players.Remove(player);
                    PlayerLeftRaise(player, false);
                    break;
                }
                if (temp == 'C' || temp == 'c')
                {
                    player.supportAixtermColors = !player.supportAixtermColors;
                    RenderFrame(player, true);
                }
            }
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
                    players.Remove(player);
                    PlayerLeftRaise(player, true);
                    break;
            }
        }

        private void RenderFrame(bool full)
        {
            for (int i = players.Count - 1; i >= 0; i--)
                RenderFrame(players[i], full);
        }

        private void RenderFrame(PlayerClass player, bool full)
        {
            if (player.supportAixtermColors)
            {
                player.vt.SetBackgroundColor(new VT100.ColorClass() { Bright = true, Color = VT100.ColorEnum.Blue });
                player.vt.SetForegroundColor(new VT100.ColorClass() { Bright = true, Color = VT100.ColorEnum.Yellow });
            }
            else
            {
                player.vt.SetBackgroundColor(new VT100.ColorClass() { Bright = false, Color = VT100.ColorEnum.Black });
                player.vt.SetForegroundColor(new VT100.ColorClass() { Bright = false, Color = VT100.ColorEnum.White });
            }
            if (full)
            {
                player.vt.ClearScreen();
                player.vt.SetCursor(0, 0);
                player.vt.WriteText("Welcome on TelnetGames!");
                player.vt.SetCursor(0, 2);
                player.vt.WriteText("Currently opened games: " + gameCount);
                player.vt.SetCursor(0, 3);
                player.vt.WriteText("Average frame processing time: " + averageFrameProcessingTime + "ms");
                player.vt.SetCursor(0, 5);
                player.vt.WriteText("Select game:");
                player.vt.SetCursor(0, 6);
                player.vt.WriteText("1: Pong (multiplayer)\r\n");
                player.vt.WriteText("2: Breakout\r\n");
                player.vt.WriteText("3: Tetris\r\n");
				//player.vt.WriteText("4: Curve (WIP!)");
                player.vt.SetCursor(0, 10);
                player.vt.WriteText("E - Exit, C - ");
                if (player.supportAixtermColors)
                    player.vt.WriteText("Disable");
                else
                    player.vt.WriteText("Enable");
                player.vt.WriteText(" aixterm colors");
				player.vt.WriteText("");
				player.vt.WriteText("Should work on any VT100 compatibile terminal, but works best on PuTTY");
				player.vt.WriteText("Source: https://github.com/Milek7/telnet-games");
            }
            else
            {
                player.vt.SetCursor(24, 2);
                player.vt.ClearLine(VT100.ClearMode.CursorToEnd);
                player.vt.WriteText(gameCount.ToString());
                player.vt.SetCursor(31, 3);
                player.vt.ClearLine(VT100.ClearMode.CursorToEnd);
                player.vt.WriteText(averageFrameProcessingTime + "ms");
            }
            Flush(player);
        }

        public override void AddPlayer(PlayerClass player)
        {
            player.vt.SetCursorVisiblity(false);
            player.vt.Bell();
            players.Add(player);
            RenderFrame(player, true);
        }

        public override void KillGame()
        {
            throw new NotImplementedException();
        }
    }
}
