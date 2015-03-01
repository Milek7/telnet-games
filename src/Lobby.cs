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
        public override string Name { get { return "Lobby"; } }
        public override string Description { get { return ""; } }
        public override int PlayerCount { get { return players.Count; } }

        public override void Tick()
        {
            HandleInput();
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
                    if (temp == '1')
                    {
                        players.Remove(player);
                        PlayerHandoffRaise(typeof(Pong), player);
                        break;
                    }
                    if (temp == 'E' || temp == 'e')
                    {
                        players.Remove(player);
                        PlayerLeftRaise(player, false);
                        break;
                    }
                    if (temp == 'C' || temp == 'c')
                    {
                        player.compatibilityMode = !player.compatibilityMode;
                        RenderFrame(player);
                    }
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
                    Console.WriteLine("Flush timeout, skipping frame!");
                    break;
                case VT100.FlushReturnState.Error:
                    Console.WriteLine("Flush exception!");
                    players.Remove(player);
                    PlayerLeftRaise(player, true);
                    break;
            }
        }

        private void RenderFrame(PlayerClass player)
        {
            player.vt.ClearScreen();
            player.vt.SetCursor(0, 0);
            if (player.compatibilityMode)
                player.vt.WriteText("Welcome on TelnetGames!\r\n\r\n1. Pong (multiplayer)\r\n\r\nE - Exit, C - Enable aixterm colors");
            else
                player.vt.WriteText("Welcome on TelnetGames!\r\n\r\n1. Pong (multiplayer)\r\n\r\nE - Exit, C - Disable aixterm colors");
            Flush(player);
        }

        public override void AddPlayer(PlayerClass player)
        {
            player.vt.SetCursorVisiblity(false);
            player.vt.Bell();
            players.Add(player);
            RenderFrame(player);
        }

        public override void KillGame()
        {
            throw new NotImplementedException();
        }
    }
}
