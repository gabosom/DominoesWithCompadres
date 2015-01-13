using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DominoesWithCompadres.Models;
using DominoesWithCompadres.Utils;
using DominoesWithCompadres.Models.ViewModel;

namespace DominoesWithCompadres.Controllers
{
    public class GameController : Controller
    {

        //
        // GET: /Game/
        public ActionResult Index()
        {
            return View();
        }

        //
        // GET: /Game/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        //
        // GET: /Game/Create
        public ActionResult Create()
        {

            return View();
        }

        //
        // POST: /Game/Create
        [HttpPost]
        //public ActionResult Create(FormCollection collection)
        public ActionResult Start(CreateGame GameStartDetails)
        {
            if(ModelState.IsValid)
            { 
                try
                {
                    //TODO 10: if model is incomplete, then redirect with error

                    ViewBag.DisplayName = GameStartDetails.Create_UserDisplayName;
                    ViewBag.UserType = GameStartDetails.UserType.ToString();

                     
                        DominoGame newGame = GameService.CreateGame();
                        return View(newGame);
                    
                }
                catch
                {
                    return View("../Home/Index", GameStartDetails);
                }
            }
            else
            {
                ViewBag.ActionName = this.ControllerContext.RouteData.Values["action"].ToString();
                return View("../Home/Index", new JoinOrCreateGameModel()
                {
                    createGame = GameStartDetails
                });
            }
        }


        public ActionResult Join(JoinGame GameStartDetails)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    //TODO 10: if model is incomplete, then redirect with error

                    ViewBag.DisplayName = GameStartDetails.UserDisplayName;
                    ViewBag.UserType = GameStartDetails.UserType.ToString();


                    DominoGame existingGame = GameService.Get(GameStartDetails.GameCode);
                    if(existingGame != null)
                    {
                        return View("Start", existingGame);
                    }
                    else
                    {
                        ViewBag.ActionName = this.ControllerContext.RouteData.Values["action"].ToString();
                        ModelState.AddModelError("GameCode", "The game doesn't exist");
                        return View("../Home/Index", new JoinOrCreateGameModel()
                        {
                            joinGame = GameStartDetails
                        });
                    }
                    
                }
                catch
                {
                    return View("../Home/Index", GameStartDetails);
                }
            }
            else
            {
                ViewBag.ActionName = this.ControllerContext.RouteData.Values["action"].ToString();
                return View("../Home/Index", new JoinOrCreateGameModel()
                {
                    joinGame = GameStartDetails
                });
            }
        }

        //
        // GET: /Game/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        //
        // POST: /Game/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO 13: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
