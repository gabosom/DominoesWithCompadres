using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DominoesWithCompadres.Models
{
    public class Player
    {
        public string DisplayName { get; set; }
        [JsonProperty("connectionId")]
        public string ConnectionID { get; set; }
        public int ID { get; set; }
        public bool IsReady { get; set; }
        public List<Tile> Tiles { get; set; }

        public Player()
        {
            this.IsReady = false;
            this.Tiles = new List<Tile>();
        }

        internal void AddTile(Tile t)
        {
            this.Tiles.Add(t);
        }
    }
}
