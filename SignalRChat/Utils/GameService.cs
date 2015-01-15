using DominoesWithCompadres.Models;
using DominoesWithCompadres.Models.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    }
}