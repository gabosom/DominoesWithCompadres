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
            RoundResults results = new RoundResults();
            StringBuilder messageBuilder = new StringBuilder();

            messageBuilder.Append("Winner: ");
            
            //Make this more efficient
            //get player with 0 tiles
            Player winningPlayer = game.Players.Single<Player>(p => p.Tiles.Count == 0);
            
            messageBuilder.Append(winningPlayer.DisplayName);
            

            //if it's a team game, then add team mate
            if(game.Players.Count == 4)
            {
                Player otherWinner = game.Players[(game.Players.IndexOf(winningPlayer) + 2) % 4]; 
                results.Winners.Add(otherWinner);
                messageBuilder.Append(" ").Append(otherWinner.DisplayName);
            }


            //calculate winning points
            List<Player> losers = game.Players.Where<Player>(p => !results.Winners.Contains(p)).ToList<Player>();
            int totalPoints = 0;

            foreach(Player p in losers)
            {
                foreach(Tile t in p.Tiles)
                {
                    totalPoints += t.Value1;
                    totalPoints += t.Value2;
                }
            }
            winningPlayer.Points += totalPoints;
            results.Winners.Add(winningPlayer);


            //TODO 39: better message
            messageBuilder.Append(" won " + totalPoints + ".");


            //TODO 40: remove this until I use knockoutJS mapping for these things
            results.Winners.Clear();
            results.Winners = game.Players;


            results.Message = messageBuilder.ToString();
            return results;
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
    }
}