﻿$(function () {
    //**** Knockout Data ***///

    var gameCode = $("#gameCode").val();
    var userInTurn = false;
    var tilesInRound = new Array();
    var tilesInRoundClient = 0;
    var list_firstDirectionMovement = ["up", "right", "down", "right"];
    var list_firstDirectionIndex = 0;
    var list_lastDirectionMovement = ["down", "left", "up", "left"];
    var list_lastDirectionIndex = 0;
    

    var debug = true;

    function WriteConsole(message)
    {
        if(debug == true)
        {
            console.log(message)
        }
    }


    function ViewModel() {
        var self = this;

        self.gameCode= ko.observable();
        self.id = ko.observable();
        self.createdDate = ko.observable();
        self.players = ko.observableArray();
        self.state = ko.observable();
        self.availableTiles = ko.observableArray();
        self.myRoundTiles = ko.observableArray();
        self.playedTiles = ko.observableArray();
        self.playerInTurn = ko.observable();
        self.mySelectedTile = ko.observable();
        self.firstPlayedTile = ko.observable();
        

        this.initializeViewModel = function (game) {
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
            self.setPlayerInTurn(round.playerInTurn);

            //TODO: need a way to clean up the old values before just assigning empty
            self.playedTiles([]);
        };

        self.selectMyTile = function (selectedTile, event) {

            if (userInTurn)
            {
                $(".myTileContainer > .tile").removeClass("selected");
                
                self.mySelectedTile(selectedTile);

                //TODO: if user clicks a selected tile, it should unselect and hide possible plays
                $(".myTileContainer > div[data-tileId='" + selectedTile.id+ "'").addClass("selected");

                //show where the selected tile can be placed
                //TODO: pass the tileIndex instead of the tileId

                
                showPossibleTilePlays(selectedTile);
            }
        };

        self.getTilesDropClasses = function (tile) {
            
            return getDropClassForValue(tile.value1) + " " + getDropClassForValue(tile.value2);
            
        };

        self.getWordNumbersFromValue = function(value) {
            var classWord = "";
            switch (value) {
                case "0": case 0:
                    classToAdd = "zero"; break;
                case "1": case 1:
                    classToAdd = "one"; break;
                case "2": case 2:
                    classToAdd = "two"; break;
                case "3": case 3:
                    classToAdd = "three"; break;
                case "4": case 4:
                    classToAdd = "four"; break;
                case "5": case 5:
                    classToAdd = "five"; break;
                case "6": case 6:
                    classToAdd = "six"; break;
            }
            return classToAdd;
        }

        self.setPlayerInTurn = function (playerPositon) {
            this.playerInTurn(playerPositon);
        }
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

    gameHub.client.setupGame = function (game) {
        viewModel.initializeViewModel(game);
        changeGameState(game.State);
    };

    gameHub.client.updateGameState = function (state) {
        changeGameState(state);
    };

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

    gameHub.client.updatePlayerInTurn = function (playerPosition) {
        //TODO: update view model and subscribe to changes to make stuff happen there
        updatePlayerInTurn(playerPosition);

    };

    gameHub.client.initializeRound = function (currentRound) {
        viewModel.initializeRound(currentRound);
        initializeRound();

        //TODO: viewModel changes to current player should trigger update user, for now, forcing manually
        updatePlayerInTurn(viewModel.playerInTurn());
    };

    gameHub.client.userPlayedTile = function (tile, nextPlayerInTurn, listPosition) {
        playTileOnBoard(tile, listPosition);

        updatePlayerInTurn(nextPlayerInTurn);
    };




    

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


    function updatePlayerInTurn(playerPosition)
    {
        viewModel.setPlayerInTurn(playerPosition);

        WriteConsole("Update Player In Turn: " + playerPosition);
        if(isUsersTurn())
        {
            WriteConsole("It's MY turn");
            //need to generate possible options for plays
            Debug_OutputArray(viewModel.playedTiles(), "Played tiles array");
            
            generateDroppableZonesForPlays();
        }
    }

    //check if it's users turn
    function isUsersTurn()
    {
        //TODO: need better way of doing this
        //if it's my turn then true, else false
        if (gameHub.connection.id == viewModel.players()[viewModel.playerInTurn()].connectionId) {
            userInTurn = true;
            return true;
        }
        else
        {
            userInTurn = false;
            return false;
        }
            
    }

    function generateDroppableZonesForPlays() {

        //if the board is empty
        if (viewModel.playedTiles().length == 0) {
            
            //generate tile droppable zone and add it
            var dropTarget = createDroppableTarget([0,1,2,3,4,5,6], "first")

            $(".roundTileBoard").append(dropTarget);


            //put it in the middle
            $(dropTarget).position({
                of: $(".roundTileBoard"),
                my: "center",
                at: "center"
            });

        }
        //there are tiles in play
        //there are always two: first/last
        else {
            var firstTile = viewModel.playedTiles()[0];
            var lastTile = viewModel.playedTiles()[viewModel.playedTiles().length - 1];

            var firstOpenValue = getOpenValue("first");
            var lastOpenValue = getOpenValue("last");

            var firstDropTarget = createDroppableTarget([firstOpenValue], "first");
            var lastDropTarget = createDroppableTarget([lastOpenValue], "last");

            $(".roundTileBoard").append(firstDropTarget).append(lastDropTarget);

            //set positioning
            //TODO: make this lanes turn as needed

            positionTileOnBoard(firstDropTarget, ".roundTileBoard > .tile[data-tileId='" + firstTile.id + "']", "first");

            positionTileOnBoard(lastDropTarget, ".roundTileBoard > .tile[data-tileId='" + lastTile.id + "']", "last");
        }
    }


    function getOpenValue(listPosition)
    {
        if(listPosition == "first"){
            if (list_firstDirectionIndex == 0 || list_firstDirectionIndex == 3)
                return viewModel.playedTiles()[0].value1;
            else
                return viewModel.playedTiles()[0].value2;
        }
        else {
            if (list_lastDirectionIndex == 0)
                return viewModel.playedTiles()[viewModel.playedTiles().length - 1].value2;
            else
                return viewModel.playedTiles()[viewModel.playedTiles().length - 1].value1;
        }
    }

    //accepts values is an array of numbers
    //listPosition is a string with 2 possibel values "first" or "last"
    function createDroppableTarget(acceptsValues, listPosition) {

        //TODO: validate that listPosition is a valid place

        //generate tile droppable zone 
        var drop = document.createElement("div");
        $(drop).addClass("tile").addClass("droppableTileZone").attr("data-listPosition", listPosition);

        for(i = 0; i < acceptsValues.length; i++)
        {
            var dropClass = getDropClassForValue(acceptsValues[i]);

            $(drop).addClass(dropClass);
        }

        //attach events to the drop target
        $(drop).click(function () {

            var listPosition = $(this).attr("data-listposition"); 
            //TODO: find a better way to find if the drop target should be actionable
            //maybe check if the selectedTile's class matches the drop targets class
            if ($(this).css("visibility") == "visible")
            {
                //TODO: validate that the domino is droppable in the dropzone

                //remove the selectedTile from myTiles and add it to played game
                viewModel.myRoundTiles.remove(viewModel.mySelectedTile());

                playTileOnBoard(viewModel.mySelectedTile(), listPosition);


                //TODO: alert server of play
                gameHub.server.userPlayedTile(gameCode, viewModel.mySelectedTile(), listPosition)
                userInTurn = false;
                

                //TODO: enforce turn bool during moves
                //empty selection, destroy droppables, & endturn
                viewModel.mySelectedTile({});                
                $(".droppableTileZone").remove();
                
            }
        });

        return drop;
    }

    
    function positionTileOnBoard(selector, anchorSelector, listPosition)
    {

        var allTileContainer = ".roundTileBoard";

        //start loop until tile is correct
        var isTilePlacedCorrectly = false;

        //TODO: work out that this doesn't cycle forever 
        var tries = 0;
        while(!isTilePlacedCorrectly)
        {

            //NOTE: I tried making this generic, but every position for first and last is different. 
            //need to have this completely independent
            if (listPosition == "first")
            {
                switch(list_firstDirectionIndex)
                {
                    case 0:
                        {
                            $(selector).removeClass("horizontal");

                            $(selector).position({
                                of: $(anchorSelector),
                                my: "center bottom",
                                at: "center top"
                            });
                        } break;
                    case 1:
                        {
                            $(selector).addClass("horizontal");

                            $(selector).position({
                                of: $(anchorSelector),
                                my: "left top",
                                at: "right top"
                            });
                        } break;
                    case 2:
                        {
                            $(selector).removeClass("horizontal");

                            $(selector).position({
                                of: $(anchorSelector),
                                my: "right top",
                                at: "right bottom"
                            });
                        } break;

                    case 3:
                        {
                            $(selector).addClass("horizontal");

                            $(selector).position({
                                of: $(anchorSelector),
                                my: "left bottom",
                                at: "right bottom"
                            });
                        } break;
                }
            }

            else
            {
                switch (list_lastDirectionIndex)
                {
                    case 0:
                        {
                            $(selector).removeClass("horizontal");

                            $(selector).position({
                                of: $(anchorSelector),
                                my: "center top",
                                at: "center bottom"
                            });
                        } break;
                    case 1:
                        {
                            $(selector).addClass("horizontal");

                            $(selector).position({
                                of: $(anchorSelector),
                                my: "right bottom",
                                at: "left bottom"
                            });
                        } break;
                    case 2:
                        {
                            $(selector).removeClass("horizontal");

                            $(selector).position({
                                of: $(anchorSelector),
                                my: "center bottom",
                                at: "center top"
                            });
                        } break;

                    case 3:
                        {
                            $(selector).addClass("horizontal");

                            $(selector).position({
                                of: $(anchorSelector),
                                my: "right top",
                                at: "left top"
                            });
                        } break;
                }
            }

        
            var containerOffset = $(allTileContainer).offset();
            var containerPosition = $(allTileContainer).position();
            var containerLeft = $(allTileContainer).css("left");
            var containerTOp = $(allTileContainer).css("top");
            var tileOffset = $(selector).offset();
            var tilePosition = $(selector).position();
            var tileLeft = $(selector).css("left");
            var tileTop = $(selector).css("top");



            //check if the piece is not inside
            if (
                tilePosition.left < 0 ||
                tilePosition.left + $(selector).outerWidth(false) > containerPosition.left + $(allTileContainer).innerWidth() ||
                tilePosition.top < containerPosition.top ||
                tilePosition.top + $(selector).outerHeight(false) > containerPosition.top + $(allTileContainer).innerHeight()
                )
            {
                //turning logic

                //turns up, right, down, right
                if(listPosition == "first")
                {
                    list_firstDirectionIndex = ++list_firstDirectionIndex%4;
                }

                    //turns down left up left
                else
                {
                    list_lastDirectionIndex = ++list_lastDirectionIndex % 4;
                }
            }

                //it's inside
            else
            {
                isTilePlacedCorrectly = true;
            }

            tries++;
            if(tries == 3)
            {
                alert('salio con error')
                isTilePlacedCorrectly = true;
            }
            //isTilePlacedCorrectly = true;
        }
        
    }

    //tile is a tile object, listPosition is either "first" or "last"
    function playTileOnBoard(tile, listPosition)
    {
        //TODO: need to do this better when I figure out how to paint
        //TODO: need to figure out what happens when joining mid game
        //when it's the first, need to anchor it to the middle
        if (viewModel.playedTiles().length == 0) {

            //add straight to the view model
            viewModel.playedTiles.push(tile);

            //set as the first, then anchor it
            viewModel.firstPlayedTile(viewModel.mySelectedTile());

            $(".roundTileBoard > .tile[data-tileId='" + tile.id + "']").addClass("firstTilePlayed");

            //put it in the middle
            $(".firstTilePlayed").position({
                of: $(".roundTileBoard"),
                my: "center",
                at: "center"
            });
        }

        else
        {
            //if first in the list, then shift. if end of list, then push
            switch (listPosition) {
                case "first": {

                    //check if the values that connect go together, if not, make sure they are
                    var firstOpenValue = getOpenValue(listPosition);

                    //shift viewModel when appropriate
                    if ((list_firstDirectionIndex == 0 && firstOpenValue != tile.value2) ||
                        (list_firstDirectionIndex == 3 && firstOpenValue != tile.value2) ||
                        (list_firstDirectionIndex == 1 && firstOpenValue != tile.value1) ||
                        (list_firstDirectionIndex == 2 && firstOpenValue != tile.value1)
                        )
                    {
                        WriteConsole("Need to invert tile values");
                        var tempValue = tile.value1;
                        tile.value1 = tile.value2;
                        tile.value2 = tempValue;
                    }

                    //add tile to game
                    viewModel.playedTiles.unshift(tile);

                    //TODO: need to turn if past the top
                    //TODO: delete commented code if tiles now turn
                    //$(".roundTileBoard > .tile[data-tileid='" + tile.id + "']").position({
                    //    of: $(".roundTileBoard > .tile[data-tileid='" + viewModel.playedTiles()[1].id + "']"),
                    //    my: "center bottom",
                    //    at: "center top"
                    //});

                    positionTileOnBoard(".roundTileBoard > .tile[data-tileid='" + tile.id + "']", ".roundTileBoard > .tile[data-tileid='" + viewModel.playedTiles()[1].id + "']", listPosition);
                } break;

                case "last": {
                    //check if the values that connect go together, if not, make sure they are
                    var lastOpenValue = getOpenValue(listPosition);

                    //shift viewModel when appropriate
                    if ((list_lastDirectionIndex == 0 && lastOpenValue != tile.value1) ||
                        (list_lastDirectionIndex == 3 && lastOpenValue != tile.value2) ||
                        (list_lastDirectionIndex == 1 && lastOpenValue != tile.value1) ||
                        (list_lastDirectionIndex == 2 && lastOpenValue != tile.value1)
                        )
                    {
                        WriteConsole("Need to invert tile values");
                        var tempValue = tile.value1;
                        tile.value1 = tile.value2;
                        tile.value2 = tempValue;
                    }

                    viewModel.playedTiles.push(tile);

                    //TODO: need to turn if past the bottom
                    //$(".roundTileBoard > .tile[data-tileid='" + tile.id + "']").position({
                    //    of: $(".roundTileBoard > .tile[data-tileid='" + viewModel.playedTiles()[viewModel.playedTiles().length-2].id + "']"),
                    //    my: "center top",
                    //    at: "center bottom"
                    //});

                    positionTileOnBoard(".roundTileBoard > .tile[data-tileid='" + tile.id + "']", ".roundTileBoard > .tile[data-tileid='" + viewModel.playedTiles()[viewModel.playedTiles().length - 2].id + "']", listPosition);
                } break;
            }
        }
    }

    //section "round for gameplay, select for shuffle"
    function getTileForId(section, tileId)
    {
        //TODO: finish select for section
        switch(section)
        {
            case "round":
                {
                    return $(".roundTileBoard > .tile[data-tileid='" + tileId + "']");
                } break;

            case "select":
                {
                    return $("#selectTiles > .tile[data-tileid='" + tileId + "']");
                } break;
        }
    }

    function getDropClassForValue(value)
    {
        var classToAdd = "";
        switch (value) {
            case "0": case 0:
                classToAdd = "0drop"; break;
            case "1": case 1:
                classToAdd = "1drop"; break;
            case "2": case 2:
                classToAdd = "2drop"; break;
            case "3": case 3:
                classToAdd = "3drop"; break;
            case "4": case 4:
                classToAdd = "4drop"; break;
            case "5": case 5:
                classToAdd = "5drop"; break;
            case "6": case 6:
                classToAdd = "6drop"; break;
        }
        return classToAdd;
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

   
    /***** gameplay *****/
    function initializeRound()
    {
        //TODO: set the correct size for roundTileBoard, for now it's hardcoded


    }

    //this is sending the tile object {id, value1, value2}
    function showPossibleTilePlays(tile)
    {
        //hide all current options
        $(".droppableTileZone").css("visibility", "hidden");


        $(".droppableTileZone[class*='" + getDropClassForValue(tile.value1) + "']").css("visibility", "visible");
        $(".droppableTileZone[class*='" + getDropClassForValue(tile.value2) + "']").css("visibility", "visible");
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

                    //TODO: will need to refactor this once I have a table/viewer mode only
                    initializeRound();
                } break;
        }

        viewModel.updateGameState(state);
    }



    /***** DEBUG FUNCTIONS *****/
    function Debug_OutputArray(array, firstMessage)
    {
        WriteConsole(firstMessage)
        for(i = 0; i < array.length; i++)
        {
            WriteConsole(JSON.stringify(array[i]));
        }
    }

});
// This optional function html-encodes messages for display in the page.
function htmlEncode(value) {
    var encodedValue = $('<div />').text(value).html();
    return encodedValue;
}

function removePxTextFromMeasure(measure)
{
    return eval(measure.substr(0, measure.length - 2));
}