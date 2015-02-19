using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TelnetGames
{
    abstract class Game
    {
        public class PlayerClass
        {
            public PlayerType playerType;
            public TcpClient tcpClient;
            public VT100 vt;
        }

        public enum PlayerType
        {
            Player,
            Spectator
        }

        public event Action<Game> GameKilled;
        public event Action<Game, PlayerClass, bool> PlayerLeft;

        protected void GameKilledRaise()
        {
            if (GameKilled != null)
                GameKilled(this);
        }

        protected void PlayerLeftRaise(PlayerClass player, bool connectionKilled)
        {
            if (PlayerLeft != null)
                PlayerLeft(this, player, connectionKilled);
        }

        public abstract void Tick();
        public abstract void AddPlayer(PlayerClass player);
        public abstract void KillGame();
        public abstract int PlayersCount();
    }
}
