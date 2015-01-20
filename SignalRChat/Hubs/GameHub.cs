using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using DominoesWithCompadres.Utils;
using DominoesWithCompadres.Models;
using System.Threading;
using DominoesWithCompadres.Models.ViewModel;
using System.Threading.Tasks;

namespace DominoesWithCompadres.Hubs
{
    public class GameHub : Hub
    {
        public void JoinGame(string displayName, string gameCode, UserType userType)
        {

            bool userJoinedSuccessfully = false;
            switch(userType)
            {
                case UserType.Player:
                    {
                        userJoinedSuccessfully = GameService.PlayerJoined(gameCode, displayName, Context.ConnectionId, this);                        
                    }break;

                case UserType.Viewer:
                    {
                        GameService.ViewerJoined(gameCode, Context.ConnectionId, this);
                    }break;
            }

        }
    
        public void UserReady(string gameCode)
        {

            GameService.PlayerReady(gameCode, Context.ConnectionId, this);

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
                    Clients.Group(gameCode).removeAvailableTiles((7 * game.Players.Count));
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
                        Clients.Caller.error(new Exception("Tile played is no good"));
                    }
                }
                else
                {

                    Clients.Group(gameCode).userPasses(Context.ConnectionId);

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


        /** Deal with connection issues**/
        public override Task OnConnected()
        {
            // Add your own code here.
            // For example: in a chat application, record the association between
            // the current connection ID and user name, and mark the user as online.
            // After the code in this method completes, the client is informed that
            // the connection is established; for example, in a JavaScript client,
            // the start().done callback is executed.
            return base.OnConnected();
        }

        public override Task OnDisconnected()
        {
            // Add your own code here.
            // For example: in a chat application, mark the user as offline, 
            // delete the association between the current connection id and user name.
            
            //mark player that is not playing as disconnected
            GameService.UserDisconnected(Clients.Caller.gameCode, this.Context.ConnectionId, this);

            return base.OnDisconnected();
        }

        public override Task OnReconnected()
        {
            // Add your own code here.
            // For example: in a chat application, you might have marked the
            // user as offline after a period of inactivity; in that case 
            // mark the user as online again.
            return base.OnReconnected();
        }
    }
}