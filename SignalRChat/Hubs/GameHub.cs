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
            string userId = (string)this.Clients.Caller.userId;
            switch(userType)
            {
                case UserType.Player:
                    {
                        GameService.PlayerJoined(gameCode, displayName, Context.ConnectionId, userId,  this);                        
                    }break;

                case UserType.Viewer:
                    {
                        GameService.ViewerJoined(gameCode, Context.ConnectionId, userId, this);
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
            GameService.UserSelectedTile(gameCode, Context.ConnectionId, tileId, this);
        }

        public void UserPlayedTile(string gameCode, Tile tilePlayed, string listPosition)
        {
            GameService.UserPlaysTile(gameCode, Context.ConnectionId, tilePlayed, listPosition, this);
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