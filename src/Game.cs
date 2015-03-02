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
            public VT100 vt;
            public bool supportAixtermColors;
        }

        public enum PlayerType
        {
            Player,
            Spectator
        }

        public event Action<Game> GameKilled;
        public event Action<Game, PlayerClass, bool> PlayerLeft;
        public event Action<Game, Type, PlayerClass> PlayerHandoff;

        public abstract int MinPlayers { get; }
        public abstract int MaxPlayers { get; }
        public abstract int PlayerCount { get; }

        public abstract void Tick();
        public abstract void AddPlayer(PlayerClass player);
        public abstract void KillGame();

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

        protected void PlayerHandoffRaise(Type destination, PlayerClass player)
        {
            if (PlayerHandoff != null)
                PlayerHandoff(this, destination, player);
        }
    }
}
