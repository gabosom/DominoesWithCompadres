using DominoesWithCompadres.Models;
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
    }
}