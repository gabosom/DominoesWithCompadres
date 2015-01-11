using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DominoesWithCompadres.Models.ViewModel
{
    public class RoundResults
    {
        [JsonProperty("winners")]
        public List<Player> Winners { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }

        public RoundResults()
        {
            this.Winners = new List<Player>();
        }
    }
}