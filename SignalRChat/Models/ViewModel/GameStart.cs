using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DominoesWithCompadres.Models.ViewModel
{
    public class GameStart
    {
        private string _GameCode;
        public string UserAction { get; set; }
        public string UserDisplayName { get; set; }
        public string GameCode { 
            get{
                return this._GameCode;
            }
            set {
                this._GameCode = (string)value.ToUpper();
            }
        }
    }
}