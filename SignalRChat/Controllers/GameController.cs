using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DominoesWithCompadres.Models;
using DominoesWithCompadres.Utils;

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
        public ActionResult Start(DominoesWithCompadres.Models.ViewModel.GameStart GameStartDetails)
        {
            try
            {
                //TODO 10: if model is incomplete, then redirect with error

                //TODO 11: make sure the gamecode is always caps

                ViewBag.DisplayName = GameStartDetails.UserDisplayName;
                ViewBag.UserType = GameStartDetails.UserType.ToString();

                if(GameStartDetails.UserAction.Equals("newGame"))
                { 
                    DominoGame newGame = GameService.CreateGame();
                    return View(newGame);
                }
                else
                {
                    DominoGame existingGame = GameService.Get(GameStartDetails.GameCode);
                    if(existingGame != null)
                    {
                        return View(existingGame);
                    }
                    else
                    {
                        //TODO 12
                        throw new Exception("Game doesn't exist");
                    }
                }
            }
            catch
            {
                return View();
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
