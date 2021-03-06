﻿using Newtonsoft.Json;
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
        [JsonProperty("id")]
        public string ID { get; set; }
        public bool IsReady { get; set; }
        public List<Tile> Tiles { get; set; }
        [JsonProperty("points")]
        public int Points { get; set; }
        [JsonProperty("state")]
        public UserState State { get; set; }

        public Player()
        {
            this.IsReady = false;
            this.Tiles = new List<Tile>();
            this.State = UserState.Connected;
        }

        internal void AddTile(Tile t)
        {
            this.Tiles.Add(t);
        }
    }

    public enum UserState
    {
        Connected,
        Disconnected
    }
}
