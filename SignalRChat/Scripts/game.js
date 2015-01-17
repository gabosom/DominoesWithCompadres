$(function () {
    //**** Knockout Data ***///

    var gameCode = $("#gameCode").val();
    var userType = $("#userType").val();
    var displayName = $("#displayName").val();
    var userInTurn = false;
    var tilesInRound = new Array();
    var tilesInRoundClient = 0;
    var list_firstDirectionMovement = ["up", "right", "down", "right"];
    var list_firstDirectionIndex = 0;
    var list_lastDirectionMovement = ["down", "left", "up", "left"];
    var list_lastDirectionIndex = 0;
    var droppableTargetAnimationTimer = null;    

    var debug = true;


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
        self.playerInTurnDisplayName = ko.observable();
        self.mySelectedTile = ko.observable();
        self.firstPlayedTile = ko.observable();
        self.message = ko.observable();
        self.isUserSmallScreen = ko.observable();
        self.screenOrientation = ko.observable();
        self.mobile_lastPlayedTile = ko.observableArray();
        self.mobile_firstPlayedTile = ko.observableArray();

        this.initializeViewModel = function (game) {
            this.gameCode(game.GameCode);
            this.id(game.ID);
            this.createdDate(game.CreatedDate);

            for (i = 0; i < game.Players.length; i++) {
                this.players.push(game.Players[i]);
            }

            this.state(game.State);
        };

        self.setMessage = function (message) {
            self.message(message);
        };

        this.addPlayer = function (player) {
            this.players.push(player);
        };

        self.updatePlayers = function (players) {
            self.players.removeAll();

            //one loop to add it all back
            for (i = 0; i < players.length; i++)
                self.players.push(players[i]);
        };

        self.setAvailableTiles = function (tiles) {
            self.availableTiles.removeAll();

            for (i = 0; i < tiles.length; i++)
                self.availableTiles.push(tiles[i]);

            if(isUserPlayer())
                setSelectableTiles();
        };

        this.updateGameState = function (state) {
            this.state(state);
        };

        this.addRoundTile = function (tileId) {
            this.myRoundTiles.push(this.getTile(tileId));
        };

        this.addRoundTileWithTile = function (tile) {
            this.myRoundTiles.push(tile);
        };

        this.getTile = function (tileId) {
            for(i = 0; i < this.availableTiles().length; i++)
            {
                var curTile = this.availableTiles()[i];
                if (curTile.id == tileId)
                    return this.availableTiles()[i];
            }
        };

        self.initializeRound = function(round){
            self.setPlayerInTurn(round.playerInTurn);
            self.playedTiles.removeAll();
            self.myRoundTiles.removeAll();
        };

        self.selectMyTile = function (selectedTile, event) {
            if(isUserPlayer())
            {
                if (userInTurn)
                {

                    //finish any animations going
                    finishAllAnimations();


                    $(".myTileContainer > .tile").removeClass("selected");
                
                    self.mySelectedTile(selectedTile);

                    //TODO 2: if user clicks a selected tile, it should unselect and hide possible plays
                    $(".myTileContainer > div[data-tileid='" + selectedTile.id+ "']").addClass("selected");


                    //show where the selected tile can be placed                
                    showPossibleTilePlays(selectedTile);
                }
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
        };

        self.setPlayerInTurn = function (playerPositon) {
            self.playerInTurn(playerPositon);
            self.playerInTurnDisplayName(self.players()[playerPositon].DisplayName);
        };

        self.mobile_addTile = function (tile, listPosition)        {
            switch(listPosition)
            {
                case "first":
                    {
                        if (self.mobile_firstPlayedTile.length == 3)
                            self.mobile_firstPlayedTile.shift();
                        self.mobile_firstPlayedTile.push(tile);
                    }; break;

                case "last":
                    {
                        if (self.mobile_lastPlayedTile.length == 3)
                            self.mobile_lastPlayedTile.shift();
                        self.mobile_lastPlayedTile.push(tile);
                    }break;
            }
        };
    };

    var viewModel = new ViewModel();

    ko.applyBindings(viewModel);


    /************* 
    SignalR Communication Layer 
    ******************/
    var gameHub = $.connection.gameHub;

    gameHub.client.error = function(errorObject)
    {
        var toShow = errorObject.Code + ": " + errorObject.Message;
        alert(toShow);
    };

    gameHub.client.playerJoinedGame = function (player) {
        WriteConsole("GameHub: Player joined game: " + player.DisplayName);
        viewModel.addPlayer(player);
    };

    gameHub.client.setupGame = function (game) {
        WriteConsole("GameHub: Setting up game...");
        viewModel.initializeViewModel(game);
        changeGameState(game.State);
    };

    gameHub.client.setAvailableTiles = function (tiles) {
        viewModel.setAvailableTiles(tiles);
    };

    gameHub.client.removeAvailableTiles = function (howMany) {
        //removes available tiles so it has the count, they don't represent the actual available tiles left.
        for (i = 0; i < howMany; i++)
            viewModel.availableTiles.pop();
    };

    gameHub.client.updateGameState = function (state) {
        changeGameState(state);
    };

    gameHub.client.addTakenTile = function (tile, playerId) {
        //TODO: make animation for taken tile

        if (gameHub.connection.id == playerId)
            viewModel.addRoundTileWithTile(tile);
        
        viewModel.availableTiles.pop();
    };

    gameHub.client.otherUserTookTile = function (tileId) {
        $("div[data-tileid='" + tileId + "']").addClass("otherSelected");
    };

    //selected tile result
    gameHub.client.iTookTile = function (tileId, success) {
        var tileSelector = "#selectTiles > div[data-tileid='" + tileId + "']";

        if (success)
        {
            
            $(tileSelector).addClass("selected");
            viewModel.addRoundTile(tileId);

            
        }
        else
        {
            //TODO 3: animation for taken
            tilesInRoundClient--;
            $(tileSelector).addClass("otherSelected");
        }
    };

    gameHub.client.updatePlayerInTurn = function (playerPosition) {
        //TODO 4: update view model and subscribe to changes to make stuff happen there
        updatePlayerInTurn(playerPosition);

    };

    gameHub.client.initializeRound = function (currentRound) {
        viewModel.initializeRound(currentRound);
        initializeRound();
    };

    gameHub.client.userPlayedTile = function (tile, nextPlayerInTurn, listPosition) {
        WriteConsole("Gamehub: User Played Tile");
        playTileOnBoard(tile, listPosition);

        updatePlayerInTurn(nextPlayerInTurn);
    };

    gameHub.client.roundFinished = function (roundResults, players) {

        WriteConsole("Round over");
        WriteConsole(JSON.stringify(roundResults));
        //TODO: need to use knockout mapping to fix this better, for now, I will just send all the players and update the array
        //gamehub service should only inclue the winners in the round results
        viewModel.updatePlayers(players);

        //set all the data in the view model
        viewModel.setMessage(roundResults.message);

        //the message will be shown when clients get the update game state event
    };


    /*** Connection issues ****/
    
    

    // Start the connection.
    $.connection.hub.start().done(function () {

        gameHub.state.gameCode = gameCode;

        //make user join game
        gameHub.server.joinGame(displayName, gameCode, userType);
    });


    
    /********* 
    Game Interaction 
    ******/
    ///user getting ready for round
    $("#btnRoundReady").click(function () {
        $(".readyInfoHidden").addClass("readyInfoShown").removeClass("readyInfoHidden");
        //TODO 6: make ready button like its pressed and disabled

        gameHub.server.userReady($("#gameCode").val());
    });


    function updatePlayerInTurn(playerPosition)
    {
        viewModel.setPlayerInTurn(playerPosition);

        WriteConsole("Update Player In Turn: " + playerPosition);

        if (isUsersTurn())
        {
            WriteConsole("It's MY turn");
            //need to generate possible options for plays
            
            generateDroppableZonesForPlays();

            
            if (viewModel.playedTiles().length > 0)
            {
                var canMakeMove = false;
                //check if I can actually make a move
                var firstOpenValue = getOpenValue("first");
                var lastOpenValue = getOpenValue("last");

                for(i = 0; i < viewModel.myRoundTiles().length; i++)
                {
                    if(
                        viewModel.myRoundTiles()[i].value1 == firstOpenValue ||
                        viewModel.myRoundTiles()[i].value1 == lastOpenValue ||
                        viewModel.myRoundTiles()[i].value2 == firstOpenValue ||
                        viewModel.myRoundTiles()[i].value2 == lastOpenValue 
                    )
                    {
                        canMakeMove = true;
                        break;
                    }
                }

                if(!canMakeMove)
                {
                    if (viewModel.availableTiles().length > 0)
                    {
                        $("#takeTileContainer").addClass("takeEnabled");
                    }
                    else
                    {
                        noMorePlaysAction("pass");
                        $(".btnPassTurn").addClass("passEnabled");
                    }
                }

            }

        }
    }

    //check if it's users turn
    function isUsersTurn()
    {
        //TODO 7: need better way of doing this
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

        WriteConsole("Generate Droppable Zones");
        if (!viewModel.isUserSmallScreen())
        {        
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

                WriteConsole("Open values= first: " + firstOpenValue + ", last: " + lastOpenValue);

                var firstDropTarget = createDroppableTarget([firstOpenValue], "first");
                var lastDropTarget = createDroppableTarget([lastOpenValue], "last");

                $(".roundTileBoard").append(firstDropTarget).append(lastDropTarget);

                //set positioning
                positionTileOnBoard(firstDropTarget, ".roundTileBoard > .tile[data-tileid='" + firstTile.id + "']", "first");

                positionTileOnBoard(lastDropTarget, ".roundTileBoard > .tile[data-tileid='" + lastTile.id + "']", "last");
            }
        }

        //small screen interaction
        else
        {
            //if the board is empty
            if(viewModel.playedTiles().length == 0)
            {
                var firstDropTarget = createDroppableTarget([0, 1, 2, 3, 4, 5, 6], "first");
                var lastDropTarget = createDroppableTarget([0, 1, 2, 3, 4, 5, 6], "last");

                //add left and add right, they both work
                $(".mobile_playFirst").append(firstDropTarget);
                $(".mobile_playLast").append(lastDropTarget);

                $(firstDropTarget).position({
                    of: $(".mobile_playFirst"),
                    my: "center",
                    at: "center"
                });

                $(lastDropTarget).position({
                    of: $(".mobile_playLast"),
                    my: "center",
                    at: "center"
                });
            }
            else
            {
                if ($(".mobile_playabbleArea > .droppableTileZone").length == 0)
                {
                    //if something is being animated, just wait
                    if ($(".mobile_playabbleArea > .tile").add("#mobile_miniTile").filter(":animated").length > 0)
                    {
                        WriteConsole("Generating droppable is paused, animation was happening");

                        //TODO: could make the callback another function so when it calls back, it saves the first comparison again
                        droppableTargetAnimationTimer = window.setTimeout(generateDroppableZonesForPlays, 1000);
                    }
                    else
                    {
                        droppableTargetAnimationTimer = null;
                        WriteConsole("Generating droppable is happening");
                        var firstTile = viewModel.playedTiles()[0];
                        var lastTile = viewModel.playedTiles()[viewModel.playedTiles().length - 1];

                        var firstDropTarget = createDroppableTarget(firstTile.value1, "first");
                        var lastDropTarget = createDroppableTarget(lastTile.value2, "last");

                        WriteConsole("Open values= first: " + firstTile.value1 + ", last: " + lastTile.value2);

                        //add left and add right, they both work
                        $(".mobile_playFirst").append(firstDropTarget);
                        $(".mobile_playLast").append(lastDropTarget);

                        positionTileOnBoard(firstDropTarget, ".mobile_playFirst > .tile[data-tileid='" + firstTile.id + "']", "first");
                        positionTileOnBoard(lastDropTarget, ".mobile_playLast > .tile[data-tileid='" + lastTile.id + "']", "last");
                    }
                }
            }
        }
    }

    function finishAllAnimations()
    {
        if(!viewModel.isUserSmallScreen())
        {

        }
        else
        {
            $(".mobile_playFirst > .playTile").add(".mobile_playLast > .playTile").filter(":animated").stop(true, true);
        }
    }

    function animateTinyTileToPlay(listPosition) {

        var positionToGoTo = null;
        switch(listPosition)
        {
            case "first": positionToGoTo = $(".mobile_playFirst").position(); break;
            case "last": positionToGoTo = $(".mobile_playLast").position(); break;
        }

        positionToGoTo.left = positionToGoTo.left + $(".mobile_playFirst").width() / 2;
        positionToGoTo.top = positionToGoTo.top + $(".mobile_playFirst").height() / 2 - $("#mobile_miniTile").height()/2;

        $("#mobile_miniTile").animate({
            left: positionToGoTo.left,
            top: positionToGoTo.top
        }, {
            duration: 1000,
            queue: true,
            start: function(){
                $("#mobile_miniTile").css("visibility", "visible");
            },
            done: function () {

                $("#mobile_miniTile").css("visibility", "hidden");

                $("#mobile_miniTile").position({
                    of: $("#mobile_playerInTurn"),
                    my: "center",
                    at: "center"
                });
            }
        })
    }

    function animateTilePlayedOnScreen(selectorForTile, listPosition)
    {
        if(!viewModel.isUserSmallScreen())
        {
        }
        else
        {
            switch(listPosition)
            {
                case "first":
                    {
                        var movementForFirst = "-=" + $(selectorForTile).width() + "px";
                        $(".mobile_playFirst > .playTile").animate(
                            {
                               left: movementForFirst
                            },
                            {
                                duration: 1000,
                                queue: true
                            })
                    } break;

                case "last":
                    {
                        var movementForLast = "+=" + $(selectorForTile).width() + "px";

                        $(".mobile_playLast > .playTile").animate(
                            {
                                left: movementForLast
                            },
                            {
                                duration: 1000,
                                queue: true
                            })
                    } break;

                case "initial":
                    {
                        var movementForFirst = "-=" + $(selectorForTile).width() + "px";
                        var movementForLast = "+=" + $(selectorForTile).width() + "px";
                        $(".mobile_playFirst > .playTile").animate(
                            {
                                left: movementForFirst
                            },
                            {
                                duration: 1000,
                                queue: false
                            })

                        $(".mobile_playLast > .playTile").animate(
                            {
                                left: movementForLast
                            },
                            {
                                duration: 1000,
                                queue: false
                            })
                    } break;
            }
        }
    }

    function getOpenValue(listPosition)
    {
        if (listPosition == "first")
            return viewModel.playedTiles()[0].value1;
        else
            return viewModel.playedTiles()[viewModel.playedTiles().length -1 ].value2;
    }

    //accepts values is an array of numbers
    //listPosition is a string with 2 possibel values "first" or "last"
    function createDroppableTarget(acceptsValues, listPosition) {

        //TODO 8: validate that listPosition is a valid place

        //generate tile droppable zone 
        var drop = document.createElement("div");
        $(drop).addClass("tile").addClass("droppableTileZone").attr("data-listPosition", listPosition);


        if (Array.isArray(acceptsValues))
        {
            for (i = 0; i < acceptsValues.length; i++) {
                var dropClass = getDropClassForValue(acceptsValues[i]);

                $(drop).addClass(dropClass);
            }
        }
        else
        {
            var dropClass = getDropClassForValue(acceptsValues);
            $(drop).addClass(dropClass);
        }
        

        //attach events to the drop target
        $(drop).click(function () {

            var listPosition = $(this).attr("data-listposition"); 
            //TODO 27: find a better way to find if the drop target should be actionable
            //maybe check if the selectedTile's class matches the drop targets class
            if ($(this).css("visibility") == "visible")
            {
                //TODO 28: validate that the domino is droppable in the dropzone

                //remove the selectedTile from myTiles and add it to played game
                viewModel.myRoundTiles.remove(viewModel.mySelectedTile());

                var tilePlayed = playTileOnBoard(viewModel.mySelectedTile(), listPosition);


                gameHub.server.userPlayedTile(gameCode, tilePlayed, listPosition)
                userInTurn = false;
                

                //TODO 29: enforce turn bool during moves
                //empty selection, destroy droppables, & endturn
                viewModel.mySelectedTile({});                
                $(".droppableTileZone").remove();
                
            }
        });

        return drop;
    }

    
    function positionTileOnBoard(selector, anchorSelector, listPosition)
    {
        //if on big computer
        if (!viewModel.isUserSmallScreen())
        {
            var allTileContainer = ".roundTileBoard";

            //start loop until tile is correct
            var isTilePlacedCorrectly = false;

            //TODO 30: work out that this doesn't cycle forever 
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
                                    my: "right bottom",
                                    at: "right top",
                                    collision: "none"
                                });
                            } break;
                        case 1:
                            {
                                $(selector).addClass("horizontal");

                                $(selector).position({
                                    of: $(anchorSelector),
                                    my: "left top",
                                    at: "right top",
                                    collision: "none"
                                });
                            } break;
                        case 2:
                            {
                                //just in this case, I need to invert both divs with the values
                                $(selector).removeClass("horizontal");

                                $(selector).position({
                                    of: $(anchorSelector),
                                    my: "right top",
                                    at: "right bottom",
                                    collision: "none"
                                });

                            
                                //invert tile value div positions
                                invertTileValueDivs(selector);
                            } break;

                        case 3:
                            {
                                $(selector).addClass("horizontal");

                                $(selector).position({
                                    of: $(anchorSelector),
                                    my: "left bottom",
                                    at: "right bottom",
                                    collision: "none"
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
                                    my: "left top",
                                    at: "left bottom",
                                    collision: "none"
                                });
                            } break;
                        case 1:
                            {
                                $(selector).addClass("horizontal");

                                $(selector).position({
                                    of: $(anchorSelector),
                                    my: "right bottom",
                                    at: "left bottom",
                                    collision: "none"
                                });
                            } break;
                        case 2:
                            {
                                $(selector).removeClass("horizontal");

                                $(selector).position({
                                    of: $(anchorSelector),
                                    my: "left bottom",
                                    at: "left top",
                                    collision: "none"
                                });

                                invertTileValueDivs(selector);
                            } break;

                        case 3:
                            {
                                $(selector).addClass("horizontal");

                                $(selector).position({
                                    of: $(anchorSelector),
                                    my: "right top",
                                    at: "left top",
                                    collision: "none"
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
                    tilePosition.left < containerPosition.left ||
                    tilePosition.left + $(selector).outerWidth(false) > containerPosition.left + $(allTileContainer).innerWidth() ||
                    tilePosition.top < containerPosition.top ||
                    tilePosition.top + $(selector).outerHeight(false) > containerPosition.top + $(allTileContainer).innerHeight()
                    )
                {
                    //turning logic

                    //turns up, right, down, right
                    if(listPosition == "first")
                    {
                        if (list_firstDirectionIndex == 2)
                            invertTileValueDivs(selector)
                        list_firstDirectionIndex = ++list_firstDirectionIndex%4;
                    }

                        //turns down left up left
                    else
                    {
                        if (list_lastDirectionIndex == 2)
                            invertTileValueDivs(selector)

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
        else
        {
            switch(listPosition)
            {
                case "first": {
                    $(selector).position({
                        of: $(anchorSelector),
                        my: "left",
                        at: "right",
                        collision: "none"
                    });
                } break;
                case "last": {
                    $(selector).position({
                        of: $(anchorSelector),
                        my: "right",
                        at: "left",
                        collision: "none"
                    });
                } break;
            }
        }
    }

    function invertTileValueDivs(selector)
    {
        $(selector).children("div:first").clone().appendTo(selector);
        $(selector).children("div:first").remove();
    }
    

    //tile is a tile object, listPosition is either "first" or "last"
    function playTileOnBoard(tile, listPosition)
    {
        //TODO 31: need to figure out what happens when joining mid game
        //when it's the first, need to anchor it to the middle
        if (viewModel.playedTiles().length == 0) {

            //add straight to the view model
            viewModel.playedTiles.push(tile);

            //set as the first, then anchor it
            viewModel.firstPlayedTile(viewModel.mySelectedTile());

            if(!viewModel.isUserSmallScreen())
            {
                $(".roundTileBoard > .tile[data-tileid='" + tile.id + "']").addClass("firstTilePlayed");

                //put it in the middle
                $(".firstTilePlayed").position({
                    of: $(".roundTileBoard"),
                    my: "center",
                    at: "center"
                });
            }
            else
            {
                viewModel.mobile_addTile(tile, "first");
                viewModel.mobile_addTile(tile, "last");


                var selector_firstTile = ".mobile_playFirst > .tile[data-tileid='" + tile.id + "']";
                var selector_lastTile = ".mobile_playLast > .tile[data-tileid='" + tile.id + "']";

                animateTinyTileToPlay("first");

                $(selector_firstTile).position({
                    of: $(".mobile_playFirst"),
                    my: "center",
                    at: "center"
                });

                

                $(selector_lastTile).position({
                    of: $(".mobile_playLast"),
                    my: "center",
                    at: "center"
                });

                animateTilePlayedOnScreen(selector_firstTile, "initial");
            }
        }

        else
        {
            //finish all animations
            finishAllAnimations();

            //if first in the list, then shift. if end of list, then push
            switch (listPosition) {
                case "first": {

                    //check if the values that connect go together, if not, make sure they are
                    var firstOpenValue = getOpenValue(listPosition);

                    //shift viewModel when appropriate
                    if (
                        (firstOpenValue != tile.value2)
                      ) {

                        WriteConsole("Need to invert tile values");

                        var tempValue = tile.value1;
                        tile.value1 = tile.value2;
                        tile.value2 = tempValue;
                    }

                    //added new tile
                    viewModel.playedTiles.unshift(tile);

                    if (!viewModel.isUserSmallScreen()) {

                        positionTileOnBoard(".roundTileBoard > .tile[data-tileid='" + tile.id + "']", ".roundTileBoard > .tile[data-tileid='" + viewModel.playedTiles()[1].id + "']", listPosition);

                        //TODO 33: find a better place for this logic
                        //when the tile is done, I need to make sure the tiles only curve once to fit more
                        if (list_firstDirectionIndex % 2 == 1)
                            list_firstDirectionIndex = (list_firstDirectionIndex + 1) % 4;
                    }
                    else
                    {
                        var positionToGoTo = null;
                        switch (listPosition) {
                            case "first": positionToGoTo = $(".mobile_playFirst").position(); break;
                            case "last": positionToGoTo = $(".mobile_playLast").position(); break;
                        }

                        positionToGoTo.left = positionToGoTo.left + $(".mobile_playFirst").width() / 2;
                        positionToGoTo.top = positionToGoTo.top + $(".mobile_playFirst").height() / 2 - $("#mobile_miniTile").height() / 2;

                        $("#mobile_miniTile").animate({
                            left: positionToGoTo.left,
                            top: positionToGoTo.top
                        }, {
                            duration: 1000,
                            queue: true,
                            start: function () {
                                $("#mobile_miniTile").css("visibility", "visible");
                            },
                            done: function () {

                                $("#mobile_miniTile").css("visibility", "hidden");

                                $("#mobile_miniTile").position({
                                    of: $("#mobile_playerInTurn"),
                                    my: "center",
                                    at: "center"
                                });

                                viewModel.mobile_addTile(tile, "first");

                                var selector_firstTile = ".mobile_playFirst > .tile[data-tileid='" + tile.id + "']";


                                $(selector_firstTile).position({
                                    of: $(".mobile_playFirst"),
                                    my: "center",
                                    at: "center"
                                });

                                animateTilePlayedOnScreen(selector_firstTile, "first");
                            }
                        })
                    }

                } break;

                case "last": {
                    //check if the values that connect go together, if not, make sure they are
                    var lastOpenValue = getOpenValue(listPosition);

                    //shift viewModel when appropriate
                    if (
                        (lastOpenValue != tile.value1)
                      ) {
                        WriteConsole("Need to invert tile values");
                    
                        var tempValue = tile.value1;
                        tile.value1 = tile.value2;
                        tile.value2 = tempValue; 
                    }

                    //added new tile
                    viewModel.playedTiles.push(tile);

                    if (!viewModel.isUserSmallScreen()) {

                        positionTileOnBoard(".roundTileBoard > .tile[data-tileid='" + tile.id + "']", ".roundTileBoard > .tile[data-tileid='" + viewModel.playedTiles()[viewModel.playedTiles().length - 2].id + "']", listPosition);

                        if (list_lastDirectionIndex % 2 == 1)
                            list_lastDirectionIndex = (list_lastDirectionIndex + 1) % 4;
                    }
                    else
                    {
                        viewModel.mobile_addTile(tile, "last");

                        var selector_lastTile = ".mobile_playLast > .tile[data-tileid='" + tile.id + "']";

                        animateTinyTileToPlay("last");

                        $(selector_lastTile).position({
                            of: $(".mobile_playLast"),
                            my: "center",
                            at: "center"
                        });

                        animateTilePlayedOnScreen(selector_lastTile, "last");
                    }
                } break;
            }
        }

        return tile;
    }

    //section "round for gameplay, select for shuffle"
    function getTileForId(section, tileId)
    {
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
        /// <summary>Generates class that represents the droppable number it can receive</summary>
        /// <param name="value" type="int">the tile value</param>
        /// <returns type="string">Droppable class</returns>
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
    

    function isUserPlayer()
    {
        /// <summary>Determines if the current user is a player or not</summary>
        /// <returns type="bool">Bool reflecting if user is player or not</returns>
        switch (userType) {
            case "Player": return true;
            case "Viewer": return false;
        }
    }

    ///selecting a tile when shuffle
    ///need to check with server if no one else has selected that one
    function setSelectableTiles() {

        //TODO 34: make tiles draggable
        //$("#selectTiles > .tile").draggable();

        $("#selectTiles > .tile").click(function () {
            if (!$(this).hasClass("selected") && !$(this).hasClass("otherSelected"))
            {
                if (viewModel.myRoundTiles().length < 7 && tilesInRoundClient < 7) {
                    var selectedTileId = $(this).attr("data-tileid");
                    gameHub.server.selectedTile(gameCode, selectedTileId);
                }

                tilesInRoundClient++;
            }            
        });
    }

   
    /***** gameplay *****/
    function initializeRound()
    {
        //remove selectes state from selectTiles
        $("#selectTiles > .tile").removeClass("selected").removeClass("otherSelected");
        tilesInRoundClient = 0;
        list_firstDirectionIndex = 0;
        list_lastDirectionIndex = 0;
        

        noMorePlaysAction("take");

    }


    function noMorePlaysAction(value)
    {
        /// <summary>Depending on how many available tiles are left, it shows the take tile or pass button. "pass" or "take" are the only options</summary>
        /// <param name="value" type="string">the action to show</param>
        switch (value) {
            case "take":
                {
                    $("#takeTileContainer").show();
                    $("#passTurnContainer").hide();
                } break;
            case "pass":
                {
                    $("#takeTileContainer").hide();
                    $("#passTurnContainer").show();
                } break;
        }
    }

    //this is sending the tile object {id, value1, value2}
    function showPossibleTilePlays(tile)
    {
        if (!viewModel.isUserSmallScreen())
        {

        }
        else
        {
            if ($(".mobile_playabbleArea > .droppableTileZone").length == 0)
            {
                droppableTargetAnimationTimer = null;
                generateDroppableZonesForPlays();                
            }
        }

        //hide all current options
        $(".droppableTileZone").css("visibility", "hidden");


        $(".droppableTileZone[class*='" + getDropClassForValue(tile.value1) + "']").css("visibility", "visible");
        $(".droppableTileZone[class*='" + getDropClassForValue(tile.value2) + "']").css("visibility", "visible");
    }

    function cleanUpBoard()
    {
        viewModel.mobile_firstPlayedTile.removeAll();
        viewModel.mobile_lastPlayedTile.removeAll();
    }


    ///general functions
    function changeGameState(state)
    {
        switch(state)
        {
            case "WaitingUsersReady":
            case "RoundFinished":
                {
                    $(".selectReadyContainer").show();
                } break;

            case "SelectingTiles":
                {
                    $(".readyInfoShown").addClass("readyInfoHidden").removeClass("readyInfoShown")
                    $(".state").hide();
                    $(".selectTileContainer").show();
                    cleanUpBoard();
                } break;
            case "InProgress":
                {
                    $(".state").hide();
                    $(".gameInProgress").show();
                } break;
        }

        viewModel.updateGameState(state);
    }



    $(".btnPassTurn").click(function () {

        //TODO 38: only do this when in turn
        gameHub.server.userPlayedTile(gameCode, null, null)
        $(this).removeClass("passEnabled");
    });

    $("#takeTileContainer").click(function () {
        if ($(this).hasClass("takeEnabled")) {
            gameHub.server.takeTile(gameCode);
            $(this).removeClass("takeEnabled");
        }
    });

    //bind window change events
    $(window).bind("resize", function (e) {
        updateUserScreenData();

        //TODO 55: redraw tiles as needed when passing from one layout to the other
    })

    function updateUserScreenData()
    {
        //determine screensize & orientation
        var screenWidth = $(document).width();
        var screenHeight = $(document).height();

        //landscape
        if (screenWidth > screenHeight) {
            viewModel.screenOrientation("landscape");
        }
        else {
            viewModel.screenOrientation("portrait");
        }

        if (screenWidth <= 768) {
            viewModel.isUserSmallScreen(true);
        }
        else {
            viewModel.isUserSmallScreen(false);
        }
    }

    function setupPlayer()
    {
        /// <summary>Sets up actions & metadata for the user based on it being a player or not</summary>
        if(!isUserPlayer())
        {
            $("#btnRoundReady").hide();
        }
        else
        {
            $("#btnRoundReady").show();
        }

        updateUserScreenData();
    }


    /****** LAYOUT  & Game Setup ******/
    $(".selectReadyContainer > .overlay > .overlayContent").position({
        of: $(".selectReadyContainer > .overlay"),
        my: "center",
        at: "center",
        collision: "none"        
    });

    $("#mobile_miniTile").position({
        of: $("#mobile_playerInTurn"),
        my: "center",
        at: "center",
        collision: "none"
    })

    viewModel.setMessage("Game will begin when all players are ready...");

    setupPlayer();
    


    /***** DEBUG FUNCTIONS *****/
    function Debug_OutputArray(array, firstMessage)
    {
        WriteConsole(firstMessage)
        for(i = 0; i < array.length; i++)
        {
            WriteConsole(JSON.stringify(array[i]));
        }
    }

    if(debugConsoleEnabled)
    {
        $("#debugConsole").tabs();

        $("#toggleDebugConsoleContainer > a").click(function () {
            $("#debugConsole").toggle();
        })
    }

    function WriteConsole(message) {
        if (debug == true) {
            console.log(message)
            if(debugConsoleEnabled)
            {
                if ($("#debug_consoleViewer").children("div").length > 60)
                    $("#debug_consoleViewer > div").first().remove();
                $("#debug_consoleViewer").append("<div>" + message + "</div>");
            }
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