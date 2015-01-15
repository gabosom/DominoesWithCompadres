using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using DominoesWithCompadres.Utils;
using DominoesWithCompadres.Models;
using System.Threading;
using DominoesWithCompadres.Models.ViewModel;

namespace DominoesWithCompadres.Hubs
{
    public class GameHub : Hub
    {
        public void JoinGame(string displayName, string gameCode, UserType userType)
        {
            //add user to Game
            DominoGame game = GameService.Get(gameCode);
            
            switch(userType)
            {
                case UserType.Player:
                    {
                        Player newPlayer = new Player()
                        {
                            ConnectionID = Context.ConnectionId,
                            DisplayName = displayName,
                            ID = GameService.GeneratePlayerId()
                        };
                        if(game.AddPlayer(newPlayer))
                            Clients.OthersInGroup(gameCode).playerJoinedGame(newPlayer);
                        else
                        { 
                            //TODO: what do we show when player couldn't join game
                        }
                    }break;

                case UserType.Viewer:
                    {
                        Viewer newViewer = new Viewer()
                        {
                            ConnectionID = Context.ConnectionId,
                            ID = GameService.GeneratePlayerId()
                        };
                        game.AddViewer(newViewer);
                    }break;
            }
            
            

            

            //add user to SignalR group
            Groups.Add(Context.ConnectionId, gameCode);

            Thread.Sleep(500);

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
                Clients.Group(gameCode).initializeRound(game.CurrentRound);
                Clients.Group(gameCode).updateGameState(game.State.ToString());
            }

        }
    
        public void TakeTile(string gameCode)
        {
            GameService.UserTakesTile(Context.ConnectionId, gameCode, this);
        }

        public void SelectedTile(string gameCode, int tileId)
        {

            //TODO 15: try/catch
            DominoGame game = GameService.Get(gameCode);

            //TODO: this player logic should go in the GameService
            Player p = game.GetPlayer(Context.ConnectionId);

            if(p != null)
            {
                bool selectedTile = game.SelectTile(Context.ConnectionId, tileId);

                //if user took tile successfully
                if (selectedTile)
                {
                    Clients.OthersInGroup(gameCode).otherUserTookTile(tileId);
                }

                //always notify caller of what happened
                //TODO: shouldn't send this if the caller is not a player
                Clients.Caller.ITookTile(tileId, selectedTile);
            
                if(game.ReadyForRoundStart())
                {
                    game.StartRound();
                    Clients.Group(gameCode).updateGameState(game.State.ToString());
                    Clients.Group(gameCode).removeAvailableTiles(28 - (7 * game.Players.Count));
                    Clients.Group(gameCode).updatePlayerInTurn(game.CurrentRound.PlayerInTurn);
                }
            }
        }

        public void UserPlayedTile(string gameCode, Tile tilePlayed, string listPosition)
        {

            //TODO 16: try/catch
            DominoGame game = GameService.Get(gameCode);

            //TODO: this player logic should go in the GameService
            Player p = game.GetPlayer(Context.ConnectionId);

            if(p != null)
            { 
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
                            Clients.Group(gameCode).roundFinished(GameService.GetRoundResults(game), game.Players);
                            Clients.Group(gameCode).updateGameState(game.State.ToString());
                        } break;
                    case GameState.Finished: break;
                }
            }
        }
    }
}