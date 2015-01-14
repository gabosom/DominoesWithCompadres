using DominoesWithCompadres.Models.ViewModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace DominoesWithCompadres.Models
{
    public class DominoGame 
    {
        public int ID { get; set; }
        public string GameCode { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<Player> Players { get; set; }
        public List<Viewer> Viewers { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public GameState State { get; set; }
        public List<Tile> AvailableTiles { get; set; }
        [JsonProperty("currentRound")]
        public Round CurrentRound { get; set; }
        public int GameLimit { get; set; }
        
        private Random _RandomNumberGenerator { get; set; }
        
        private bool ReadyForRound { get; set; }
        private List<int> _IDsforTiles { get; set; }
        
        public DominoGame()
        {
            this.Players = new List<Player>();
            this.Viewers = new List<Viewer>();
            this.AvailableTiles = new List<Tile>();
            this.State = GameState.Created;
            this.ReadyForRound = false;
            this._IDsforTiles = new List<int>();
            this._RandomNumberGenerator = new Random((int)DateTime.Now.Ticks);
            this.GameLimit = 50;
            
            this.GenerateTiles();
        }

        private void GenerateTiles()
        {
            //TODO 20: shuffle tiles
            this.AvailableTiles.Clear();
            this._IDsforTiles.Clear();
            this._IDsforTiles.AddRange(Enumerable.Range(0, 28));

            //TODO 18: generate the right tile IDs
            for(int i = 0; i <= 6; i++)
            {
                for(int j = i; j <= 6; j++)
                {
                    this.AvailableTiles.Add(new Tile()
                    {
                         Value1 = i,
                         Value2 = j,
                         ID = this.GetNextIDForTile()
                    });
                }
            }
        }

        public void StartRound()
        {

            //randomly determine who starts 
            Random rand = new Random((int)DateTime.Now.Ticks);
            
            this.CurrentRound.PlayerInTurn = rand.Next(this.Players.Count);
        }

        public void StartRound(int playerPosition)
        {
            this.CurrentRound.PlayerInTurn = playerPosition;
        }

        private void InitializeRound()
        {
            if (this.CurrentRound != null)
                this.CurrentRound = null;

            this.CurrentRound = new Round();

            //make all players 0 points
            foreach (Player p in this.Players)
            {
                p.Tiles.Clear();
                p.IsReady = false;
            }

            this.GenerateTiles();
            this.ReadyForRound = false;

        }


        public int PlayerInTurn()
        {
            return this.CurrentRound.PlayerInTurn;
        }

        public bool AddPlayer(Player p)
        {
            if (this.Players.Count < 4)
            {
                this.Players.Add(p);
                return true;
            }
            else
            {
                return false;
            }

        }

        public bool IsEveryoneReady()
        {
            foreach(Player p in this.Players)
            {
                if (p.IsReady == false)
                    return false;
            }

            this.State = GameState.SelectingTiles;

            return true;
        }

        private void ClearRound()
        {
            //clean up previous selected tiles
            foreach(Player p in this.Players)
            {
                p.Tiles.Clear();
            }
        }

        public void PlayerReady(string ConnectionId)
        {
            if(this.State == GameState.RoundFinished || this.State == GameState.Finished || this.State == GameState.Created)
            {
                this.State = GameState.WaitingUsersReady;
                
                //when restarting, need to remove previous tiles 
                InitializeRound();
            }

            Player currentPlayer = this.Players.Single(p => p.ConnectionID.Equals(ConnectionId));
            currentPlayer.IsReady = true;
        }

        public Player GetPlayer(string ConnectionId)
        {
            return this.Players.SingleOrDefault<Player>(p => p.ConnectionID.Equals(ConnectionId));
        }

        private Player GetActivePlayer()
        {
            return this.Players.Single<Player>(p => p.ConnectionID.Equals(this.Players[this.CurrentRound.PlayerInTurn].ConnectionID));
        }

        private Tile GetAvailableTile(int TileId)
        {
            Tile toGet = this.AvailableTiles.SingleOrDefault<Tile>(t => t.ID == TileId);
            this.AvailableTiles.Remove(toGet);
            return toGet;
        }

        internal bool SelectTile(string playerConnectionId, int tileId)
        {
            Tile t = this.GetAvailableTile(tileId);

            //if the tile is already selected
            if (t == null)
                return false;
            //else, the tile is still available
            else
            {
                Player p = this.GetPlayer(playerConnectionId);

                
                if(p.Tiles.Count < 7)
                    p.AddTile(t);

                if (p.Tiles.Count == 7)
                    CheckIfRoundIsReady();

                return true;
            }
        }

        private void CheckIfRoundIsReady()
        {
            foreach (Player p in this.Players)
            {
                if (p.Tiles.Count != 7)
                {
                    this.ReadyForRound = false;
                    return;
                }
            }

            //if round ready initialize
            this.ReadyForRound = true;
            this.State = GameState.InProgress;
        }

        internal bool ReadyForRoundStart()
        {
            return this.ReadyForRound;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="playerConnectionId">Connectio ID for the user</param>
        /// <param name="tileId">Tile ID</param>
        /// <param name="listPosition">"first" or "last" for beginning or end of the list</param>
        /// <returns></returns>
        internal bool PlayedTile(string playerConnectionId, Tile tile, string listPosition)
        {
            if (!this.GetActivePlayer().ConnectionID.Equals(playerConnectionId))
                return false;

            Player curPlayer = this.GetPlayer(playerConnectionId);

            //TODO 23: null expception
            //get tile to be played
            Tile tilePlayed = curPlayer.Tiles.Single(t => t.ID == tile.ID);
            tilePlayed.Value1 = tile.Value1;
            tilePlayed.Value2 = tile.Value2;

            //TODO 24: make sure that the value we get from the client is updated here, the client can rotate the tiles if needed

            //remove tile from user's active tile
            curPlayer.Tiles.Remove(tilePlayed);

            //add it to Played Tiles
            switch(listPosition)
            {
                case "last":
                    {
                        this.CurrentRound.PlayedTiles.AddLast(tilePlayed);
                    }break;
                case "first":
                default:
                    {
                        this.CurrentRound.PlayedTiles.AddFirst(tilePlayed);
                    }break;
                }


            //set next player
            if(curPlayer.Tiles.Count > 0)
            {
                this.PlayerNextTurn();
                this.CurrentRound.PlayersThatPassed.Clear();
            }
            else
            {
                this.RoundFinished(curPlayer);
                //TODO 25: game finished, not just round
                //this.State = GameState.RoundFinished;
            }

            return true;
        }

        private void RoundFinished(Player winner)
        {
            this.CurrentRound.Results = new RoundResults();

            StringBuilder messageBuilder = new StringBuilder();

            //Make this more efficient
            //get player with 0 tiles
            messageBuilder.Append(winner.DisplayName);


            this.CurrentRound.Results.Winners.Add(winner);
            //if it's a team game, then add team mate
            if (this.Players.Count == 4)
            {
                Player otherWinner = this.Players[(this.Players.IndexOf(winner) + 2) % 4];
                this.CurrentRound.Results.Winners.Add(otherWinner);
                messageBuilder.Append(" & ").Append(otherWinner.DisplayName);
                this.CurrentRound.Results.Winners.Add(otherWinner);
            }


            //calculate winning points
            List<Player> losers = this.Players.Where<Player>(p => !this.CurrentRound.Results.Winners.Contains(p)).ToList<Player>();
            int totalPoints = 0;

            foreach (Player p in losers)
            {
                foreach (Tile t in p.Tiles)
                {
                    totalPoints += t.Value1;
                    totalPoints += t.Value2;
                }
            }
            
            foreach(Player p in this.CurrentRound.Results.Winners)
            {
                p.Points += totalPoints;
            }

            if((winner.Points + totalPoints) >= this.GameLimit)
            {
                this.State = GameState.Finished;
                messageBuilder.Append(" won the whole game with " + totalPoints + " points.");
            }
            else
            {
                this.State = GameState.RoundFinished;
                messageBuilder.Append(" won round with " + totalPoints + " points.");
            }
            this.CurrentRound.Results.Message = messageBuilder.ToString();
            this.CurrentRound.Results.PointsWon = totalPoints;
        }

        private void PlayerNextTurn()
        {
            this.CurrentRound.PlayerInTurn = ++this.CurrentRound.PlayerInTurn % this.Players.Count;
        }

        internal void PlayerPassTurn(string p)
        {
            if(this.Players[this.CurrentRound.PlayerInTurn].ConnectionID.Equals(p))
            {   
                this.CurrentRound.PlayersThatPassed.Add(p);
            
                //if everone has passed, then end game
                if (this.CurrentRound.PlayersThatPassed.Count == this.Players.Count)
                {
                    //TODO 25: need to make this a new state and add a correct message
                    this.State = GameState.RoundFinished;
                    InitializeRound();
                }
                else
                    this.PlayerNextTurn();
            }
            else
            {
                //TODO: trying to pass when it's not turn
            }
        }

        internal void AddViewer(Viewer newViewer)
        {
            this.Viewers.Add(newViewer);
        }

        internal Tile UserTakesTile(Player player)
        {
            Tile tileToTake = this.GetAvailableTile(this.AvailableTiles[_RandomNumberGenerator.Next(this.AvailableTiles.Count)].ID);
            
            if(tileToTake != null)
            {
                this.PlayerNextTurn();
                player.Tiles.Add(tileToTake);
            }
            return tileToTake;
        }

        private int GetNextIDForTile()
        {
            int numToReturn = this._IDsforTiles[this._RandomNumberGenerator.Next(this._IDsforTiles.Count)];
            this._IDsforTiles.Remove(numToReturn);
            return numToReturn;
        }
    }

    public enum GameState
    {
        Created,
        WaitingUsersReady,
        SelectingTiles,
        RoundFinished,
        InProgress,
        Finished,
    }
}