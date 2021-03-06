﻿using Microsoft.AspNet.Identity.Owin;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Battleships.Core.ViewModels;
using Battleships.Core.Models;
using Battleships.Core.Contracts;
using System.IO;

namespace Battleships.Web.Controllers
{
	[Authorize]
	public class UserPanelController : Controller
    {


		private ApplicationUserManager _userManager;

		private IRepository<PersonalOptions> OptionsContext;
		private IRepository<LeaderBoard> LeaderBoardContext;
		private IRepository<GameHistory> GameHistoryContext;
		public Func<string> getUserId;

		public UserPanelController(IRepository<PersonalOptions> OptionsContext, IRepository<LeaderBoard> LeaderBoardContext, IRepository<GameHistory> GameHistoryContext)
		{
			getUserId = () => User.Identity.GetUserId();
			this.OptionsContext = OptionsContext;
			this.LeaderBoardContext = LeaderBoardContext;
			this.GameHistoryContext = GameHistoryContext;
		}

		public ApplicationUserManager UserManager
		{
			get
			{
				return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
			}
			private set
			{
				_userManager = value;
			}
		}

		// GET: UserPanel
		public ActionResult Index()
        {
			UserPanelIndexModel model = new UserPanelIndexModel();
			var userId = getUserId();
			LeaderBoard leaderBoard = LeaderBoardContext.Collection().FirstOrDefault(i => i.UserId == userId);
			PersonalOptions options = OptionsContext.Collection().FirstOrDefault(i => i.UserId == userId);
			if(options != null && leaderBoard != null)
			{
				model.UserName = User.Identity.GetUserName();
				model.Image = options.Image;
				model.BoardSize = options.BoardSize;
				model.Frigate = options.Frigate;
				model.Destroyer = options.Destroyer;
				model.Cruiser = options.Cruiser;
				model.Battleship = options.Battleship;
				model.Carrier = options.Carrier;
				model.Wins = leaderBoard.Wins;
				model.Loses = leaderBoard.Loses;
				
				model.WinRatio = (leaderBoard.Loses + leaderBoard.Wins) > 0 ? (decimal)leaderBoard.Wins /(leaderBoard.Loses+leaderBoard.Wins):0;
			}
			return View(model);
        }

		[HttpPost]
		public ActionResult Index(HttpPostedFileBase image)
		{
			var userId = getUserId();
			PersonalOptions options = OptionsContext.Collection().FirstOrDefault(i => i.UserId == userId);
			if (image != null && image.ContentType.Contains("image"))
			{				
				options.Image = options.Id + Path.GetExtension(image.FileName);
				image.SaveAs(Server.MapPath("//Content//Pictures//") + options.Image);
				OptionsContext.Update(options);
				OptionsContext.Commit();
			}
			return RedirectToAction("index", "UserPanel", new { area = "" });
		}

		//get
		public ActionResult Options()
		{
			var userId = getUserId();
			PersonalOptions model = OptionsContext.Collection().FirstOrDefault(i => i.UserId == userId);

			if (model != null)
			{
				return View(model);
			}
			else
			{
				return HttpNotFound();
			}
		}

		[HttpPost]
		public ActionResult Options(PersonalOptions options, string id)
		{
			PersonalOptions optionsToEdit = OptionsContext.Find(id);
			if(optionsToEdit != null)
			{
				if (!ModelState.IsValid) return View(options);
				optionsToEdit.BoardSize = options.BoardSize;
				optionsToEdit.Frigate = options.Frigate;
				optionsToEdit.Destroyer = options.Destroyer;
				optionsToEdit.Carrier = options.Carrier;
				optionsToEdit.Battleship = options.Battleship;
				optionsToEdit.Cruiser = options.Cruiser;
				OptionsContext.Commit();
				return RedirectToAction("index", "UserPanel", new { area = "" });
			}
			else
			{
				return HttpNotFound();
			}
		}



		public ActionResult Leaderboard()
		{
			UserPanelLeaderboardModel model = new UserPanelLeaderboardModel();
			List<LeaderBoard> leaderBoards = LeaderBoardContext.Collection().OrderByDescending(o => o.Wins).ThenByDescending(o => o.Wins > 0 ? o.MatchesPlayed/o.Wins : 0).ToList();
			if(leaderBoards != null)
			{
				model.LeaderBoards = leaderBoards;
				int length = leaderBoards.Count;
				for (int i =0;i < length; i++)
				{
					var userId = leaderBoards[i].UserId;
					var image = OptionsContext.Collection().FirstOrDefault(k => k.UserId == userId).Image;
					var user = UserManager.FindById(userId);
					model.UserName.Add(user.UserName);
					model.Image.Add(image);
				}
			}
			return View(model);
		}

		public ActionResult PlayerDetails(string userId)
		{
			UserDetailsModel model = new UserDetailsModel();
			var user = UserManager.FindById(userId);
			if(user != null)
			{
				var leaderBoard = LeaderBoardContext.Collection().FirstOrDefault(i => i.UserId == userId);
				var image = OptionsContext.Collection().FirstOrDefault(i => i.UserId == userId).Image;
				if (user != null && leaderBoard != null)
				{
					model.UserId = userId;
					model.UserName = user.UserName;
					model.Image = image;
					model.Wins = leaderBoard.Wins;
					model.Loses = leaderBoard.Loses;
					model.WinRatio = (leaderBoard.Wins + leaderBoard.Loses) > 0 ? (decimal)leaderBoard.Wins / (leaderBoard.Wins + leaderBoard.Loses) : 0;
				}
				return View(model);
			}
			else
			{
				return HttpNotFound();
			}

		}

		public ActionResult GameHistory(string id)
		{
			UserPanelHistoryModel model = new UserPanelHistoryModel();
			string userId = getUserId();
			model.Matches = string.IsNullOrEmpty(id) ?	GameHistoryContext.Collection().Where(i => i.PlayerOneId == userId || i.PlayerTwoId == userId).OrderByDescending(o => o.PlayedAt).ToList() : 
														GameHistoryContext.Collection().Where(i => i.PlayerOneId == id || i.PlayerTwoId == id).OrderByDescending(o => o.PlayedAt).ToList();
			int length = model.Matches.Count;
			if(model.Matches != null)
			{
				for (int i = 0; i < length; i++)
				{
					var playerOneId = model.Matches[i].PlayerOneId;
					var playerTwoId = model.Matches[i].PlayerTwoId;

					model.PlayerOne.Add(UserManager.FindById(playerOneId).UserName);
					model.PlayerTwo.Add(UserManager.FindById(playerTwoId).UserName);
					model.ImagePlayerOne.Add(OptionsContext.Collection().FirstOrDefault(k => k.UserId == playerOneId).Image);
					model.ImagePlayerTwo.Add(OptionsContext.Collection().FirstOrDefault(k => k.UserId == playerTwoId).Image);
				}

				return View(model);
			}
			else
			{
				return HttpNotFound();
			}
				
		}


		public ActionResult FindPlayer(string searchQuery)
		{
			List<SearchUserModel> model = new List<SearchUserModel>();
			if(!string.IsNullOrEmpty(searchQuery))
			{
				var users = UserManager.Users.Where(i => i.UserName.Contains(searchQuery));
				int lp = 1;
				foreach(var user in users)
				{
					SearchUserModel foundUser = new SearchUserModel();
					foundUser.lp = lp++;
					foundUser.UserId = user.Id;
					foundUser.UserName = user.UserName;
					foundUser.Image = OptionsContext.Collection().FirstOrDefault(i => i.UserId == user.Id).Image;
					model.Add(foundUser);
				}
			}

			return View(model);
		}

	}
}