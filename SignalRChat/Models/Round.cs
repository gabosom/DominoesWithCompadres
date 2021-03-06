﻿using DominoesWithCompadres.Models.ViewModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DominoesWithCompadres.Models
{
    public class Round
    {
        [JsonProperty("playedTiles")]
        public LinkedList<Tile> PlayedTiles { get; set; }
        [JsonProperty("playerInTurn")]
        public int PlayerInTurn { get; set; }
        public List<string> PlayersThatPassed { get; set; }
        public RoundResults Results { get; set; }

        public Round()
        {
            this.PlayedTiles = new LinkedList<Tile>();
            this.PlayerInTurn = 0;
            this.PlayersThatPassed = new List<string>();
        }
    }
}
