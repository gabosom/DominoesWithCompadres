$(function () {
    //**** Knockout Data ***///

    var gameCode = $("#gameCode").val();
    var userInTurn = false;
    var tilesInRound = new Array();
    var tilesInRoundClient = 0;

    var debug = true;

    function WriteConsole(message)
    {
        if(debug == true)
        {
            console.log(message)
        }
    }

    function Round()
    {
        this.playedTiles = ko.observableArray();
        this.playerInTurn = ko.observable();

        this.setPlayerInTurn  = function(playerPositon)
        {
            this.playerInTurn(playerPositon);
        }

        //TODO: find a way to add played tiles
    };

    function ViewModel() {
        this.gameCode= ko.observable();
        this.id= ko.observable();
        this.createdDate= ko.observable();
        this.players= ko.observableArray();
        this.state= ko.observable();
        this.availableTiles = ko.observableArray();
        this.myRoundTiles = ko.observableArray();
        this.currentRound = null;
        

        this.initializeViewModel = function (game) {
            this.currentRound = new Round();
            this.gameCode(game.GameCode);
            this.id(game.ID);
            this.createdDate(game.CreatedDate);

            for (i = 0; i < game.Players.length; i++) {
                this.players.push(game.Players[i]);
            }

            this.state(game.State);


            for (i = 0; i < game.AvailableTiles.length; i++) {
                this.availableTiles.push(game.AvailableTiles[i]);
            }
        };

        this.addPlayer = function (player) {
            this.players.push(player);
        };

        this.updateGameState = function (state) {
            this.state(state);
        };

        this.addRoundTile = function (tileId) {
            this.myRoundTiles.push(this.getTile(tileId));
        };

        this.getTile = function (tileId) {
            for(i = 0; i < this.availableTiles().length; i++)
            {
                var curTile = this.availableTiles()[i];
                if (curTile.id == tileId)
                    return this.availableTiles()[i];
            }
        };

        this.initializeRound = function(round){
            this.currentRound.setPlayerInTurn(round.playerInTurn);

            this.currentRound.playedTiles([]);
        };
    };

    var viewModel = new ViewModel();

    ko.applyBindings(viewModel);


    /************* 
    SignalR Communication Layer 
    ******************/
    var gameHub = $.connection.gameHub;

    gameHub.client.playerJoinedGame = function (player) {
        viewModel.addPlayer(player);
    };

    gameHub.client.setupGame = function(game)
    {
        viewModel.initializeViewModel(game);
        changeGameState(game.State);
    }

    gameHub.client.updateGameState = function(state)
    {
        changeGameState(state);
    }

    gameHub.client.otherUserTookTile = function (tileId) {
        $("div[data-tileId='" + tileId + "'").addClass("otherSelected");
    };

    //selected tile result
    gameHub.client.iTookTile = function (tileId, success) {
        if(success)
        {
            $("div[data-tileId='" + tileId + "'").addClass("selected");
            viewModel.addRoundTile(tileId);
        }
        else
        {
            //TODO: animation for taken
            tilesInRoundClient--;
            $("div[data-tileId='" + tileId + "'").addClass("otherSelected");
        }
    };

    gameHub.client.updatePlayerInTurn = function(playerPosition)
    {
        //TODO: set classes for current player
        WriteConsole("Update Player In Turn: " + playerPosition);
    }

    gameHub.client.initializeRound = function(currentRound)
    {
        viewModel.initializeRound(currentRound);

        //check if it's users turn
        isUsersTurn();
    }




    

    // Start the connection.
    $.connection.hub.start().done(function () {

        //make user join game
        gameHub.server.joinGame($("#displayName").val(), $("#gameCode").val());
    });


    
    /********* 
    Game Interaction 
    ******/
    ///user getting ready for round
    $("#btnRoundReady").click(function () {
        $(".readyInfoHidden").addClass("readyInfoShown").removeClass("readyInfoHidden");
        //TODO: make ready button like its pressed and disabled

        gameHub.server.userReady($("#gameCode").val());
    });


    //check if it's users turn
    function isUsersTurn()
    {
        //TODO: need better way of doing this
        var a = viewModel.players()[viewModel.currentRound.playerInTurn()].connectionId;
        var b = gameHub.connection.id;
        if(gameHub.connection.id == viewModel.players()[viewModel.currentRound.playerInTurn()].connectionId)
        {
            //then it's my turn
            userInTurn = true;
        }
    }

    ///selecting a tile when shuffle
    ///need to check with server if no one else has selected that one
    function setSelectableTiles() {

        $("#selectTiles > .tile").draggable();

        $("#selectTiles > .tile").click(function () {
            if (!$(this).hasClass("selected") && !$(this).hasClass("otherSelected"))
            {
                if (viewModel.myRoundTiles().length < 7 && tilesInRoundClient < 7) {
                    var selectedTileId = $(this).attr("data-tileId");
                    gameHub.server.selectedTile(gameCode, selectedTileId);
                }

                tilesInRoundClient++;
            }            
        });
    }

    function setMyTiles() {

        $(".myTileContainer > .tile").each(function (index, item) {
            setupMyTile(item);
        });
        
    }

    function setupMyTile(tile)
    {

        $(tile).click(function () {
            $(".myTileContainer > .tile").removeClass("selected");
            $(this).addClass("selected");
        });
    }


    /***** gameplay *****/
    function initializeRound()
    {
        //TODO: set the correct size for roundTileBoard, for now it's hardcoded


    }

    ///general functions
    function changeGameState(state)
    {
        $(".state").hide();
        switch(state)
        {
            case "WaitingUsersReady":
                {
                    $(".selectReadyContainer").show();
                } break;

            case "SelectingTiles":
                {
                    $(".selectTileContainer").show();
                    setSelectableTiles();
                } break;
            case "InProgress":
                {
                    $(".gameInProgress").show();
                    setMyTiles();

                    //TODO: will need to refactor this once I have a table/viewer mode only
                    initializeRound();
                } break;
        }

        viewModel.updateGameState(state);
    }

});
// This optional function html-encodes messages for display in the page.
function htmlEncode(value) {
    var encodedValue = $('<div />').text(value).html();
    return encodedValue;
}
