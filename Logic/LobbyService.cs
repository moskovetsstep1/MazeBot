﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MazeGenerator.MazeLogic;
using MazeGenerator.Models;

namespace MazeGenerator.Logic
{
    public class LobbyService
    {
        public static MazeObjectType CheckLobbyCoordinate(Coordinate coord, Lobby lobby)
        {
            if (lobby.Events.Any(e => Equals(e.Position, coord)))
            {
                return MazeObjectType.Event;
            }
            if (lobby.Players.Any(e => Equals(e.UserCoordinate, coord)))
            {
                return MazeObjectType.Player;
            }            
            if (lobby.Maze[coord.X, coord.Y] == 0)
                return MazeObjectType.Void;
            if (lobby.Maze[coord.X, coord.Y] == 1)
                return MazeObjectType.Wall;

            //TODO: create own exeption
            throw new Exception("CheckCoord");
        }
    }
}