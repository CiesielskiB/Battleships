﻿using Battleships.Core.Contracts;
using Battleships.Core.Models;
using Battleships.Core.ViewModels;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Battleships.Web.Controllers
{
	[Authorize]
    public class GameController : Controller
    {
		//TODO get players options from database optionsContext
		IRepository<PersonalOptions> OptionsContext;
		IRepository<LeaderBoard> LeaderBoardContext;
		IRepository<GameHistory> GameHistoryContext;

		public GameController(IRepository<PersonalOptions> OptionsContext, IRepository<LeaderBoard> LeaderBoardContext, IRepository<GameHistory> GameHistoryContext)
		{
			this.OptionsContext = OptionsContext;
			this.LeaderBoardContext = LeaderBoardContext;
			this.GameHistoryContext = GameHistoryContext;
		}

		// GET: Game
		[AllowAnonymous]
		public ActionResult Index()
        {
			return View();
        }

		public ActionResult GameVsBot(bool bot)
		{
			GameOptions gameOptions = new GameOptions();
			if (User.Identity.IsAuthenticated)
			{
				//var check = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>().FindByEmail("Admin@mysite.pl").PasswordHash;
				//var Try = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>().PasswordHasher.VerifyHashedPassword(check, "Test123");
				string userId = User.Identity.GetUserId();
				gameOptions.PlayerOne = User.Identity.GetUserName();
				gameOptions.PlayersOptions = OptionsContext.Collection().First(i => i.UserId.Equals(userId));
				if (gameOptions.PlayersOptions == null)
				{
					return RedirectToAction("Index", "Game");
				}
			}

			return View(gameOptions);
		}

		//get
		public ActionResult GamevsPlayerLogIn()
		{
			return View();
		}

		//post
		[HttpPost]
		public ActionResult GamevsPlayerLogIn(SecondPlayerLoginModel user)
		{
			if (!ModelState.IsValid) 
			{
				return View(user);
			}
			if(user.UserName.ToLower().Equals(User.Identity.GetUserName().ToLower())) // can't use the same user as the one logged in
			{
				ModelState.AddModelError("", "Can't play vs yourself :)");
				return View(user);
			}
			var getUser = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>().FindByName(user.UserName);
			if (getUser != null) // if user exists lets check their pw
			{
				var hash = getUser.PasswordHash;
				var isCorrect = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>().PasswordHasher.VerifyHashedPassword(hash, user.Password);
				if(isCorrect == PasswordVerificationResult.Success) // if everything is fine log him in and redirect to the game
				{
					TempData["username"] = user.UserName; //username for later use in actually game action
					return RedirectToAction("Index", "Game");
				}
			}
			ModelState.AddModelError("", "Wrong login or passowrd.");
			return View(user);
		}

		public ActionResult GameVsPlayer()
		{
			bool isLogged = TempData.TryGetValue("username", out object username);
			if (!isLogged) return RedirectToAction("Index", "Game");
			TempData.Clear();
			return View();
		}

		public void BotGameSave(int winner)
		{
			string botId = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>().FindByEmail("Bot@Battleships.com").Id;
			string userId = User.Identity.GetUserId();
			var leaderBoards = LeaderBoardContext.Collection();

			LeaderBoard playersLeaderBoard = leaderBoards.FirstOrDefault(i => i.UserId == userId);
			LeaderBoard botsLeaderBoard = leaderBoards.FirstOrDefault(i => i.UserId == botId);
			playersLeaderBoard.MatchesPlayed++;
			botsLeaderBoard.MatchesPlayed++;
			if (winner == 0)
			{
				playersLeaderBoard.Wins++;
				botsLeaderBoard.Loses++;
			}
			else
			{
				playersLeaderBoard.Loses++;
				botsLeaderBoard.Wins++;
			}
			LeaderBoardContext.Update(playersLeaderBoard);
			LeaderBoardContext.Update(botsLeaderBoard);
			LeaderBoardContext.Commit();

			GameHistory newGame = new GameHistory
			{
				PlayerOneId = userId,
				PlayerTwoId = botId,
				Winner = winner == 0 ? userId : botId
			};
			GameHistoryContext.Insert(newGame);
			GameHistoryContext.Commit();
		}
	
	}
}