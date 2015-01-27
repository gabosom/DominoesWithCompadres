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

        public static bool PlayerJoined(string gameCode, string displayName, string userConnectionId, string userId, GameHub gameHub)
        {
            try
            {
                //add user to Game
                DominoGame game = GameService.Get(gameCode);

                Player existingPlayer = game.Players.SingleOrDefault(p => p.ID.Equals(userId));
                
                ///need to see if there is a player with the userId first, if not, create one
                if (existingPlayer != null)
                {
                    existingPlayer.ConnectionID = userConnectionId;
                    gameHub.Groups.Add(userConnectionId, gameCode);
                    return true;
                }
                else
                {
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
                        gameHub.Clients.Caller.setupGame(game, newPlayer.ID);
                        return true;
                    }
                    else
                    {
                        //TODO: what do we show when player couldn't join game
                        gameHub.Clients.Group(gameCode).error(new Exception("Game is full"));
                        return false;
                    }
                }

            }
            catch(Exception e)
            {
                gameHub.Clients.Group(gameCode).error(e);
                return false;
            }
        }

        public static void ViewerJoined(string gameCode, string userConnectionId, string userId, GameHub gameHub)
        {
            try
            {
                //add user to Game
                DominoGame game = GameService.Get(gameCode);

                Viewer existingViewer = game.Viewers.SingleOrDefault(p => p.ID.Equals(userId));
                
                ///need to see if there is a player with the userId first, if not, create one
                if (existingViewer != null)
                {
                    existingViewer.ConnectionID = userConnectionId;
                    gameHub.Groups.Add(userConnectionId, gameCode);
                }
                else
                {
                    Viewer newViewer = new Viewer()
                    {
                        ConnectionID = userConnectionId,
                        ID = GameService.GeneratePlayerId()
                    };

                    game.AddViewer(newViewer);
                    gameHub.Groups.Add(newViewer.ConnectionID, gameCode);

                    //needed this for sync issues while setting up the game
                    Thread.Sleep(500);
                    gameHub.Clients.Caller.setupGame(game, newViewer.ID);
                }
                
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

        public static string GeneratePlayerId()
        {
            return Guid.NewGuid().ToString();
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
            try
            {
                DominoGame game = GameService.Get(gameCode);

                if (game.Players[game.PlayerInTurn()].ConnectionID.Equals(playerConnectionId))
                {
                    Tile t = game.UserTakesTile(game.GetPlayer(playerConnectionId));

                    if (t != null)
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
            catch(Exception e)
            {
                GameService.SendExceptionToClients(e, gameCode, gameHub);
            }
        }


        public static void SendExceptionToClients(Exception e, string gameCode, GameHub gameHub)
        {
            try
            {
                gameHub.Clients.Group(gameCode).error(e);
            }
            catch
            {
                //log at some point
            }
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

                if (game.Players.Count > 0 || game.Viewers.Count > 0)
                    gamehub.Clients.Group(gameCode).error(new Exception(p.DisplayName + " was disconnected."));
                else
                    GameService.CurrentGames.RemoveAll(m => m.GameCode.Equals(gameCode));
            }
            catch(Exception e)
            {
                SendExceptionToClients(e, gameCode, gamehub);
            }
        }

        internal static void PlayerReady(string gameCode, string playerConnectionId, GameHub gameHub)
        {
            try
            {
                DominoGame game = GameService.Get(gameCode);

                game.PlayerReady(playerConnectionId);


                if (game.IsEveryoneReady())
                {
                    game.InitializeRound();
                    Thread.Sleep(500);
                    gameHub.Clients.Group(gameCode).setAvailableTiles(game.AvailableTiles);
                    gameHub.Clients.Group(gameCode).initializeRound(game.CurrentRound);
                    gameHub.Clients.Group(gameCode).updateGameState(game.State.ToString());
                }

            }
            catch(Exception e)
            {
                SendExceptionToClients(e, gameCode, gameHub);
            }
        }

        internal static void UserSelectedTile(string gameCode, string playerConnectionId, int tileId, GameHub gameHub)
        {
            try
            {
                DominoGame game = GameService.Get(gameCode);

                //TODO: this player logic should go in the GameService
                Player p = game.GetPlayer(playerConnectionId);

                if (p != null)
                {
                    bool selectedTile = game.SelectTile(playerConnectionId, tileId);

                    //if user took tile successfully
                    if (selectedTile)
                    {
                        gameHub.Clients.OthersInGroup(gameCode).otherUserTookTile(tileId);
                    }

                    //always notify caller of what happened
                    //TODO: shouldn't send this if the caller is not a player
                    gameHub.Clients.Caller.ITookTile(tileId, selectedTile);

                    if (game.ReadyForRoundStart())
                    {
                        game.StartRound();
                        gameHub.Clients.Group(gameCode).updateGameState(game.State.ToString());
                        gameHub.Clients.Group(gameCode).removeAvailableTiles((7 * game.Players.Count));
                        gameHub.Clients.Group(gameCode).updatePlayerInTurn(game.CurrentRound.PlayerInTurn);
                    }
                }
            }
            catch(Exception e)
            {
                SendExceptionToClients(e, gameCode, gameHub);
            }
        }

        internal static void UserPlaysTile(string gameCode, string playerConnectionId, Tile tilePlayed, string listPosition, GameHub gameHub)
        {
            try
            {
                DominoGame game = GameService.Get(gameCode);

                Player p = game.GetPlayer(playerConnectionId);

                if (p == game.Players[game.CurrentRound.PlayerInTurn])
                {
                    if (p != null)
                    {
                        if (tilePlayed != null)
                        {

                            bool playIsGood = game.PlayedTile(playerConnectionId, tilePlayed, listPosition);

                            if (playIsGood)
                            {
                                gameHub.Clients.OthersInGroup(gameCode).userPlayedTile(tilePlayed, game.CurrentRound.PlayerInTurn, listPosition);
                            }
                            else
                            {
                                gameHub.Clients.Caller.error(new Exception("Tile played is no good"));
                            }
                        }
                        else
                        {

                            gameHub.Clients.Group(gameCode).userPasses(playerConnectionId);

                            game.PlayerPassTurn(playerConnectionId);

                            //TODO 21: send message to clients to show something like a pass message
                            //TODO 22: Round over when all players pass

                            gameHub.Clients.Group(gameCode).updatePlayerInTurn(game.CurrentRound.PlayerInTurn);
                        }


                        //check what the game state is
                        switch (game.State)
                        {
                            case GameState.InProgress: gameHub.Clients.Caller.updatePlayerInTurn(game.CurrentRound.PlayerInTurn); break;
                            case GameState.RoundFinished:
                                {
                                    gameHub.Clients.Group(gameCode).roundFinished(GameService.GetRoundResults(game), game.Players);
                                    gameHub.Clients.Group(gameCode).updateGameState(game.State.ToString());
                                } break;
                            case GameState.Finished: break;
                        }
                    }
                }
                else
                {
                    GameService.SendExceptionToClients(new Exception("User passing is not the user in turn!"), gameCode, gameHub);
                }
            }
            catch(Exception e)
            {
                SendExceptionToClients(e, gameCode, gameHub);
            }
        }
    }
}