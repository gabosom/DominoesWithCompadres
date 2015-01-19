using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DominoesWithCompadres.Models.ViewModel
{
    public class GameException : Exception
    {
        public string Code { get; set; }
    }
}