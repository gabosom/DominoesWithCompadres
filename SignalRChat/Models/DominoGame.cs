using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DominoesWithCompadres.Models
{
    public class DominoGame 
    {
        public int ID { get; set; }
        public string GameCode { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<Player> Players { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public GameState State { get; set; }
        public List<Tile> AvailableTiles { get; set; }
        [JsonProperty("currentRound")]
        public Round CurrentRound { get; set; }
        
        
        private bool ReadyForRound { get; set; }

        
        public DominoGame()
        {
            this.Players = new List<Player>();
            this.AvailableTiles = new List<Tile>();
            this.State = GameState.Created;
            this.ReadyForRound = false;
            this.GenerateTiles();
            //this.InitializeRound();
        }

        private void GenerateTiles()
        {
            //TODO 20: shuffle tiles
            this.AvailableTiles.Clear();

            //TODO 18: generate the right tile IDs
            for(int i = 0; i <= 6; i++)
            {
                for(int j = i; j <= 6; j++)
                {
                    this.AvailableTiles.Add(new Tile()
                    {
                         Value1 = i,
                         Value2 = j,
                         ID = this.AvailableTiles.Count+1
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

        public void AddPlayer(Player p)
        {
            this.Players.Add(p);
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

        private Player GetPlayer(string ConnectionId)
        {
            return this.Players.Single<Player>(p => p.ConnectionID.Equals(ConnectionId));
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
                //TODO 25: game finished, not just round
                this.State = GameState.RoundFinished;
                InitializeRound();
            }

            return true;
        }

        private void PlayerNextTurn()
        {
            this.CurrentRound.PlayerInTurn = ++this.CurrentRound.PlayerInTurn % this.Players.Count;
        }

        internal void PlayerPassTurn(string p)
        {
            this.CurrentRound.PlayersThatPassed.Add(p);
            
            //if everone has passed, then end game
            if (this.CurrentRound.PlayersThatPassed.Count == this.Players.Count)
            {
                //TODO 25: game finished, not just round
                this.State = GameState.RoundFinished;
                InitializeRound();
            }
            else
                this.PlayerNextTurn();
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