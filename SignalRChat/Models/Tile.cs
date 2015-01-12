using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DominoesWithCompadres.Models
{
    public class Tile
    {
        [JsonProperty("value1")]
        public int Value1 { get; set; }
        [JsonProperty("value2")]
        public int Value2 { get; set; }
        [JsonProperty("id")]
        public int ID { get; set; }
        //TODO 26: Generate IDs so users can't inspect tiles when shuffling
    }
}
