﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DominoesWithCompadres.Utils
{
    public static class SiteSettings
    {
        static public bool DebugDominoes { get; set; }

        static SiteSettings()
        {
            DebugDominoes = Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["DebugDominoes"]);
        }
    }
}