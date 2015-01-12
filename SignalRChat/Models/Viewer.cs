using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DominoesWithCompadres.Models
{
    public class Viewer
    {
        [JsonProperty("connectionId")]
        public string ConnectionID { get; set; }
        public int ID { get; set; }
    }
}