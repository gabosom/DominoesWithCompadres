using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace DominoesWithCompadres.Models.ViewModel
{
    public class GameStart
    {
        private string _GameCode;
        public string UserAction { get; set; }
        [Required]
        [StringLength(15, MinimumLength=1)]
        public string UserDisplayName { get; set; }
        public string GameCode { 
            get{
                return this._GameCode;
            }
            set {
                this._GameCode = (string)value.ToUpper();
            }
        }
        [Required]
        public UserType UserType { get; set; }

    }

    public class JoinGame
    {
        private string _GameCode;
        [Required]
        [StringLength(15, MinimumLength = 1)]
        [DisplayName("Name")]
        public string UserDisplayName { get; set; }
        [Required]
        [StringLength(4)]
        public string GameCode
        {
            get
            {
                return this._GameCode;
            }
            set
            {
                this._GameCode = (string)value.ToUpper();
            }
        }
        [Required]
        public UserType UserType { get; set; }
    }

    public class CreateGame
    {
        [Required]
        [StringLength(15, MinimumLength = 1)]
        [DisplayName("Name")]
        public string Create_UserDisplayName { get; set; }
        [Required]
        public UserType UserType { get; set; }
    }

    public class JoinOrCreateGameModel
    {
        public JoinGame joinGame { get; set; }
        public CreateGame createGame { get; set; }
    }

    public enum UserType
    {
        Player,
        Viewer        
    }

}