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

        public virtual void AddPlayer(PlayerClass player)
        {
            throw new NotImplementedException();
        }
        public virtual void RemovePlayer(PlayerClass player)
        {
            throw new NotImplementedException();
        }
        public virtual void Tick()
        {
            throw new NotImplementedException();
        }
        public virtual void KillGame()
        {
            throw new NotImplementedException();
        }
        public virtual int PlayersCount()
        {
            throw new NotImplementedException();
        }
    }
}
