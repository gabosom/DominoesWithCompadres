using DominoesWithCompadres.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DominoesWithCompadres.Utils
{
    public interface IGameService
    {
        DominoGame CreateGame();

    }
}