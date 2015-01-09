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
        }

        private void GenerateTiles()
        {
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
            InitializeRound();

            //randomly determine who starts 
            Random rand = new Random((int)DateTime.Now.Ticks);
            
            //TODO: check if this will also get the last player to be first, not sure if it will
            this.CurrentRound.PlayerInTurn = rand.Next(this.Players.Count);
        }

        public void StartRound(int playerPosition)
        {
            InitializeRound();

            this.CurrentRound.PlayerInTurn = playerPosition;
        }

        private void InitializeRound()
        {
            if (this.CurrentRound != null)
                this.CurrentRound = null;

            this.CurrentRound = new Round();
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

        public void playerReady(string ConnectionId)
        {
            Player currentPlayer = this.Players.Single(p => p.ConnectionID.Equals(ConnectionId));
            currentPlayer.IsReady = true;

            //TODO: add variable to have isEveryoneReady as a variable instead of a method that always runs
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

            this.ReadyForRound = true;
            this.State = GameState.InProgress;
        }

        //TODO: make this a variable per user so it's more efficient
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

            //TODO: null expception
            //get tile to be played
            Tile tilePlayed = curPlayer.Tiles.Single(t => t.ID == tile.ID);

            //TODO; make sure that values from the tile played are passed to server tile, it can switch on the client

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
            this.PlayerNextTurn();

            return true;
        }

        private void PlayerNextTurn()
        {
            this.CurrentRound.PlayerInTurn = ++this.CurrentRound.PlayerInTurn % this.Players.Count;
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