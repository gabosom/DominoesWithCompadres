using DominoesWithCompadres.Hubs;
using DominoesWithCompadres.Models;
using DominoesWithCompadres.Models.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;

namespace DominoesWithCompadres.Utils
{
    public static class GameService
    {
        private static List<DominoGame> CurrentGames = new List<DominoGame>();
        private static int GameIDLength = 4;
        private static int RandomSeed = (int)DateTime.Now.Ticks;
        private static Random RandomNumber = new Random(RandomSeed);
        private static int PlayerIDCount = 1;

        
        public static DominoGame CreateGame()
        {
            DominoGame NewGame = new DominoGame
            {
                CreatedDate = DateTime.UtcNow,
                GameCode = GameService.GenerateGameID(),
                ID = GameService.CurrentGames.Count + 1
            };

            GameService.CurrentGames.Add(NewGame);

            return NewGame;
        }

        public static bool PlayerJoined(string gameCode, string displayName, string userConnectionId, GameHub gameHub)
        {
            try
            {
                //add user to Game
                DominoGame game = GameService.Get(gameCode);

                Player newPlayer = new Player()
                {
                    ConnectionID = userConnectionId,
                    DisplayName = displayName,
                    ID = GameService.GeneratePlayerId()
                };

                bool couldAddPlayer = game.AddPlayer(newPlayer);
                if (couldAddPlayer)
                {
                    gameHub.Clients.OthersInGroup(gameCode).playerJoinedGame(newPlayer);
                    gameHub.Groups.Add(newPlayer.ConnectionID, gameCode);

                    //TODO-later: blog about this needed this for sync issues while setting up the game
                    Thread.Sleep(500);     
                    gameHub.Clients.Caller.setupGame(game);       
                    return true;
                }
                else
                {
                    //TODO: what do we show when player couldn't join game
                    gameHub.Clients.Group(gameCode).error(new Exception("Game is full"));
                    return false;
                }

            }
            catch(Exception e)
            {
                gameHub.Clients.Group(gameCode).error(e);
                return false;
            }
        }

        public static void ViewerJoined(string gameCode, string userConnectionId, GameHub gameHub)
        {
            try
            {
                //add user to Game
                DominoGame game = GameService.Get(gameCode);

                Viewer newViewer = new Viewer()
                {
                    ConnectionID = userConnectionId,
                    ID = GameService.GeneratePlayerId()
                };

                game.AddViewer(newViewer);
                gameHub.Groups.Add(newViewer.ConnectionID, gameCode);

                //needed this for sync issues while setting up the game
                Thread.Sleep(500);     
                gameHub.Clients.Caller.setupGame(game);       
                
                
            }
            catch (Exception e)
            {
                gameHub.Clients.Group(gameCode).error(e);
            }
        }

        private static string GenerateGameID()
        {
            string availableCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            StringBuilder newGameId = new StringBuilder();
            
            
            bool uniqueGameId = false;

            while(!uniqueGameId)
            {
                newGameId.Clear();

                for(int i = 0; i < GameService.GameIDLength; i++)
                {
                    bool addedLetter = false;
                    while(!addedLetter)
                    {
                        char charToAdd = availableCharacters[GameService.RandomNumber.Next(availableCharacters.Length)];
                        if (!newGameId.ToString().Contains(charToAdd))
                        {
                            newGameId.Append(charToAdd);
                            addedLetter = true;
                        }
                    }
                }

                //verify that the game ID is unique
                DominoGame GameExists = GameService.CurrentGames.SingleOrDefault<DominoGame>(g => g.GameCode.Equals(newGameId.ToString()));
                if(GameExists == null)
                {
                    uniqueGameId = true;
                }
            }
            return newGameId.ToString();
        }

        internal static DominoGame Get(string gameCode)
        {
            return GameService.CurrentGames.SingleOrDefault(g => g.GameCode.Equals(gameCode));
        }

        public static int GeneratePlayerId()
        {
            return GameService.PlayerIDCount++;
        }

        public static RoundResults GetRoundResults(DominoGame game)
        {
            return game.CurrentRound.Results;
        }

        public static List<string> GetViewersConnectionIds(string gameCode)
        {

            //TODO: try/catch
            DominoGame game = GameService.Get(gameCode);
            List<string> connections = new List<string>();

            foreach(Viewer viewer in game.Viewers)
            {
                connections.Add(viewer.ConnectionID);
            }

            return connections;
        }

        internal static void UserTakesTile(string playerConnectionId, string gameCode, Hubs.GameHub gameHub)
        {
            //find game and player
            //TODO: try/catch
            DominoGame game = GameService.Get(gameCode);

            if(game.Players[game.PlayerInTurn()].ConnectionID.Equals(playerConnectionId))
            {
                Tile t = game.UserTakesTile(game.GetPlayer(playerConnectionId));

                if(t != null)
                {
                    //alert client of the new tile
                    gameHub.Clients.Group(gameCode).addTakenTile(t, playerConnectionId);

                    //alert everyone of the new user
                    gameHub.Clients.Group(gameCode).updatePlayerInTurn(game.CurrentRound.PlayerInTurn);
                }
                else
                {
                    //TODO: there are no tiles left or couldn't get tile
                }
            }
            else
            {
                //TODO: user is not in turn, what to do here
            }
        }


        public static void SendExceptionToClients(Exception e, string gameCode, GameHub gameHub)
        {
            gameHub.Clients.Group(gameCode).error(e);
        }


        /** Deal with user connections **/

        public static void UserDisconnected(string gameCode, string userConnectionId, GameHub gamehub)
        {
            try
            {
                DominoGame game = GameService.Get(gameCode);

                Player p = game.GetPlayer(userConnectionId);

                //if a player was disconnected, need to notify. if its a viewer, who cares
                if(p != null)
                {
                    p.State = UserState.Disconnected;
                }
                else
                {
                    game.RemoveViewer(userConnectionId);
                }

                gamehub.Clients.Group(gameCode).error(new Exception(p.DisplayName + " was disconnected."));
            }
            catch(Exception e)
            {
                SendExceptionToClients(e, gameCode, gamehub);
            }
        }
    }
}