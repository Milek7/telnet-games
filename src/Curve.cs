/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;

namespace TelnetGames
{
	class Curve : Game
	{
		private int playerCount;
		private List<PlayerClass> players = new List<PlayerClass> ();

		public override int MinPlayers { get { return 1; } }
		public override int MaxPlayers { get { return 4; } }
		public override int PlayerCount { get { return playerCount; } }

		private struct Point
		{
			public int X;
			public int Y;
		}

		private enum Direction
		{
			N,
			E,
			S,
			W
		}

		private new class PlayerClass : Game.PlayerClass
		{
			public int points;
			public Curve.Point position;
			public Curve.Direction direction;

			public PlayerClass (Game.PlayerClass player)
			{
				playerType = player.playerType;
				vt = player.vt;
				supportAixtermColors = player.supportAixtermColors;
			}
		}

		public override void AddPlayer (Game.PlayerClass player)
		{
			players.Add (new PlayerClass (player));
			playerCount++;
		}

		public override void KillGame()
		{
			GameKilledRaise();
		}

		bool[,] map = new bool[39, 22];

		public override void Tick()
		{
			foreach (PlayerClass player in players)
			{
				if (player.direction == Direction.N)
					player.position.Y--;
				else if (player.direction == Direction.E)
					player.position.X++;
				else if (player.direction == Direction.S)
					player.position.Y++;
				else if (player.direction == Direction.W)
					player.position.X--;
			}
		}
	}
}

