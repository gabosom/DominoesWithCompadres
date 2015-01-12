﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using DominoesWithCompadres.Utils;
using DominoesWithCompadres.Models;
using System.Threading;

namespace DominoesWithCompadres.Hubs
{
    public class GameHub : Hub
    {
        public void JoinGame(string displayName, string gameCode)
        {
            //add user to Game
            DominoGame game = GameService.Get(gameCode);
            Player newPlayer = new Player()
            {
                ConnectionID = Context.ConnectionId,
                DisplayName = displayName,
                ID = GameService.GeneratePlayerId()
            };

            game.AddPlayer(newPlayer);

            //add user to SignalR group
            Groups.Add(Context.ConnectionId, gameCode);

            Thread.Sleep(500);
            Clients.OthersInGroup(gameCode).playerJoinedGame(newPlayer);
            Clients.Caller.setupGame(game);

            
        }
    
        public void UserReady(string gameCode)
        {
            //TODO 14 try/catch
            DominoGame game = GameService.Get(gameCode);

            //TODO #19 try/catch
            game.PlayerReady(Context.ConnectionId);


            if (game.IsEveryoneReady())
            {
                Clients.Group(gameCode).setAvailableTiles(game.AvailableTiles);
                Clients.Group(gameCode).updateGameState(game.State.ToString());
            }

        }
    
        public void SelectedTile(string gameCode, int tileId)
        {
            //TODO 15: try/catch
            DominoGame game = GameService.Get(gameCode);

            bool selectedTile = game.SelectTile(Context.ConnectionId, tileId);

            //if user took tile successfully
            if (selectedTile)
            {
                Clients.OthersInGroup(gameCode).otherUserTookTile(tileId);
            }

            //always notify caller of what happened
            Clients.Caller.ITookTile(tileId, selectedTile);
            
            if(game.ReadyForRoundStart())
            {
                game.StartRound();
                Clients.Group(gameCode).updateGameState(game.State.ToString());
                Clients.Group(gameCode).initializeRound(game.CurrentRound);
            }
        }

        public void UserPlayedTile(string gameCode, Tile tilePlayed, string listPosition)
        {

            //TODO 16: try/catch
            DominoGame game = GameService.Get(gameCode);

            if(tilePlayed != null)
            {
                
                bool playIsGood = game.PlayedTile(Context.ConnectionId, tilePlayed, listPosition);

                if(playIsGood)
                {
                    Clients.OthersInGroup(gameCode).userPlayedTile(tilePlayed, game.CurrentRound.PlayerInTurn, listPosition);
                }
                else
                {
                    //TODO 17: alert clients, what could go wrong here? maybe other clients trying to hack the game
                }
            }
            else
            {
                game.PlayerPassTurn(Context.ConnectionId);

                //TODO 21: send message to clients to show something like a pass message
                //TODO 22: Round over when all players pass
                Clients.Group(gameCode).updatePlayerInTurn(game.CurrentRound.PlayerInTurn);
            }


            //check what the game state is
            switch(game.State)
            {
                case GameState.InProgress: Clients.Caller.updatePlayerInTurn(game.CurrentRound.PlayerInTurn); break;
                case GameState.RoundFinished:
                    {
                        Clients.Group(gameCode).roundFinished(GameService.GetRoundResults(game));
                        Clients.Group(gameCode).updateGameState(game.State.ToString());
                    } break;
                case GameState.Finished: break;
            }
            
        }
    }
}