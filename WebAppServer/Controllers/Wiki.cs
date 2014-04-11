using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web;
using MarkdownSharp;
using System.Text;

#region AURORA
using Aurora;
using Aurora.Extra;
#endregion

#region WIKI
using Wiki.Infrastructure.Core;
using Wiki.Infrastructure.WebCore;
//using Wiki.Models.L2S;
using Wiki.Models;
using Wiki.Models.Massive;
#endregion

namespace Wiki.Controllers
{
	public class Wiki : Controller
	{
		private static Regex matchSpecialCharacters = new Regex("(?:[^a-z0-9 ]|(?<=['\"])s)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
		private static Regex matchWhitespaces = new Regex(@"\s+", RegexOptions.Compiled);
		private static Regex csharp = new Regex(@"\[cs\](?<block>[\s\S]+?)\[/cs\]", RegexOptions.Compiled);
		private static Regex markdown = new Regex(@"\[md\](?<block>[\s\S]+?)\[/md\]", RegexOptions.Compiled);

		public Wiki()
		{
			OnPreActionEvent += new EventHandler<RouteHandlerEventArgs>(Wiki_PreActionEvent);
		}

		void Wiki_PreActionEvent(object sender, RouteHandlerEventArgs e)
		{
			if (CurrentUser != null)
			{
				ViewBag.currentUser = CurrentUser.Name;
				ViewBag.logOff = new HtmlAnchor("/Logoff", "[Logoff]").ToString();
			}
		}

		#region LOGON / LOGOFF
		[Http(ActionType.Get, "/Logon")]
		public ViewResult Logon(Authentication auth, IData dc)
		{
			if (auth.Authenticated && CurrentUser != null)
				return Redirect("/Index");

			string message = string.Empty;

			if (Request.QueryString.ContainsKey("message"))
			{
				message = Request.QueryString["message"];
				ViewBag.message = message;
			}

			ViewBag.logonForm = RenderFragment("CACForm");

			return View();
		}

		[Http(ActionType.Post, "/Go")]
		public ViewResult Go(Authentication auth, IData dc)
		{
			if (CurrentUser != null)
				return Redirect("/Index");

			if (auth.Authenticated)
			{
				WikiUser user = dc.GetUser(auth.Identifier);

				if (auth.Authenticated && user != null)
				{
					LogOn(user.UserName, new string[] { "Admin" }, user, auth.Identifier);

					return Redirect("/Index");
				}
			}

			return Redirect(string.Format("/Logon?message={0}", "You Do Not Have Access To This System".ToURLEncodedString()));
		}

		[Http(ActionType.Get, "/Logoff")]
		public ViewResult LogOff(Authentication auth, IData dc)
		{
			LogOff();

			return Redirect("/Logon");
		}
		#endregion

		[Http(ActionType.Get, "/Index", ActionSecurity.Secure, RedirectWithoutAuthorizationTo = "/Logon", Roles = "User|Admin")]
		public ViewResult Index(Authentication auth, IData dc)
		{
			ViewBag.currentUser = CurrentUser.Name;
			ViewBag.content = WikiList(dc);

			return View();
		}

		[Http(ActionType.Get, "/Add", ActionSecurity.Secure, RedirectWithoutAuthorizationTo = "/Logon", Roles = "Admin")]
		public ViewResult Add(Authentication auth, IData dc)
		{
			FragBag.tagitJS = RenderFragment("TagitJS");

			FragBag.TagitJS.availableTags = "[]";
			FragBag.TagitJS.preselectedTags = "[]";
			ViewBag.tagitJS = RenderFragment("TagitJS");
			ViewBag.wikiPageForm = RenderFragment("WikiPageForm");

			return View();
		}

		[Http(ActionType.Get, "/Add", ActionSecurity.Secure, RedirectWithoutAuthorizationTo = "/Logon", Roles = "Admin")]
		public ViewResult Add(Authentication auth, IData dc, string alias)
		{
			if (!string.IsNullOrEmpty(alias))
			{
				FragBag.WikiPageForm.title = alias.Replace("wiki-", string.Empty)
														 .Replace('-', ' ')
														 .Wordify()
														 .ToTitleCase();
			}

			FragBag.TagitJS.availableTags = "[]";
			FragBag.TagitJS.preselectedTags = "[]";
			ViewBag.tagitJS = RenderFragment("TagitJS");
			ViewBag.wikiPageForm = RenderFragment("WikiPageForm");

			return View();
		}

		[Http(ActionType.Get, "/Show", ActionSecurity.Secure, RedirectWithoutAuthorizationTo = "/Logon", Roles = "User|Admin")]
		public ViewResult Show(Authentication auth, IData dc, [ActionParameterTransform("WikiPageTransform")] WikiPage p)
		{
			if (p != null)
			{
				FragBag.WikiTitle.title = p.Title;

				ViewBag.id = p.ID;
				ViewBag.title = RenderFragment("WikiTitle");

				p.Body = p.Body.ToHtmlEncodedString();

				MatchCollection csBlocks = csharp.Matches(p.Body);
				MatchCollection mdBlocks = markdown.Matches(p.Body);

				#region C# BLOCKS
				foreach (Match m in csBlocks)
				{
					FragBag.CSharpCodeBlock.code = m.Groups["block"].Value.Trim().ToHtmlDecodedString();

					p.Body = p.Body.Replace(m.Value, RenderFragment("CSharpCodeBlock"));
				}
				#endregion

				#region MARKDOWN BLOCKS
				foreach (Match m in mdBlocks)
				{
					p.Body = p.Body.Replace(m.Value, new Markdown().Transform(m.Groups["block"].Value.Trim().ToHtmlDecodedString()));
				}
				#endregion

				ViewBag.data = p.Body;

				FragBag.EditDeleteMenuItems.edit = p.ID;
				FragBag.EditDeleteMenuItems.delete = p.ID;

				ViewBag.menu = RenderFragment("EditDeleteMenuItems");

				string tags = string.Join(", ", dc.GetPageTags(p).Select(x => x.Name));

				if (!string.IsNullOrEmpty(tags))
				{
					FragBag.FiledUnder.tags = tags;
					ViewBag.filedUnder = RenderFragment("FiledUnder");
				}
			}
			else
			{
				ViewBag.title = "Error!";
				ViewBag.data = "The requested wiki does not exist";
			}

			return View();
		}

		[Http(ActionType.Get, "/Edit", ActionSecurity.Secure, RedirectWithoutAuthorizationTo = "/Logon", Roles = "Admin")]
		public ViewResult Edit(Authentication auth, IData dc, int id)
		{
			WikiPage p = dc.GetPage(id);

			if (p != null)
			{
				FragBag.WikiPageForm.id = p.ID;
				FragBag.WikiPageForm.title = p.Title;
				FragBag.WikiPageForm.data = p.Body;
				FragBag.TagitJS.preselectedTags = GetWikiPageTagsAsJSArray(dc, p);
			}

			FragBag.TagitJS.availableTags = "[]";
			ViewBag.tagitJS = RenderFragment("TagitJS");
			ViewBag.wikiPageForm = RenderFragment("WikiPageForm");

			return View();
		}

		[Http(ActionType.Get, "/Delete", ActionSecurity.Secure, RedirectWithoutAuthorizationTo = "/Logon", Roles = "Admin")]
		public ViewResult Delete(Authentication auth, IData dc, int id)
		{
			WikiPage p = dc.GetPage(id);

			if (p != null)
			{
				string alias = "/" + p.Alias;

				if (GetAllRouteAliases().Contains(alias))
					RemoveRoute(alias);

				dc.DeletePage(id);
			}

			return Redirect("/Index");
		}

		[Http(ActionType.Post, "/Save", ActionSecurity.Secure, RedirectWithoutAuthorizationTo = "/Logon", Roles = "Admin")]
		public ViewResult Save(Authentication auth, IData dc, PageData data)
		{
			if (data.IsValid)
			{
				WikiUser u = CurrentUser.ArcheType as WikiUser;

				bool edit = false;

				WikiPage p = null;

				if (data.ID != null)
				{
					p = dc.GetPage(data.ID.Value);

					if (p == null)
						p = new WikiPage();
					else
						edit = true;
				}
				else
					p = new WikiPage();

				p.AuthorID = u.ID;
				p.Body = data.Data;
				p.ModifiedOn = DateTime.Now;
				p.Title = data.Title;

				if (!edit)
				{
					p.CreatedOn = DateTime.Now;

					Func<string, string> aliasify = delegate(string s)
					{
						s = matchSpecialCharacters.Replace(s, string.Empty).Trim();
						s = matchWhitespaces.Replace(s, "-");

						return s;
					};

					p.Alias = aliasify(data.Title);

					dc.AddPage(p);
				}
				else
					dc.UpdatePage(p);

				// DO TAG related things here
				dc.DeletePageTags(p);

				if (!string.IsNullOrEmpty(data.Tags))
				{
					List<string> allTags = dc.GetAllTags().Select(x => x.Name).ToList();
					List<string> incomingTags = data.Tags.Split(',').ToList();

					if (incomingTags.Count() > 0)
					{
						var tagDiffs = (allTags.Count() > 0) ? incomingTags.Except(allTags) : incomingTags;

						if (tagDiffs.Count() > 0)
						{
							foreach (var t in tagDiffs)
								dc.AddTag(t);
						}

						foreach (var it in incomingTags)
						{
							dc.AddPageTag(p, it);
						}
					}
				}

				string alias = "/" + p.Alias;

				if (!GetAllRouteAliases().Contains(alias))
					AddRoute(alias, "Wiki", "Show", p.ID.ToString());

				return Redirect(alias);
			}

			ViewBag.error = data.Error.NewLinesToBR() + "<hr />";

			return Redirect("/Index");
		}

		[Http(ActionType.Get, "/About", ActionSecurity.Secure, RedirectWithoutAuthorizationTo = "/Logon", Roles = "User|Admin")]
		public ViewResult About(Authentication auth, IData dc)
		{
			FragBag.AboutInfo.appName = "Wiki";
			FragBag.AboutInfo.author = "Frank Hale";
			FragBag.AboutInfo.lastUpdate = "9 November 2012";

			ViewBag.content = RenderFragment("AboutInfo");

			return View();
		}

		[Http(ActionType.Get, "/Aliases", ActionSecurity.Secure, RedirectWithoutAuthorizationTo = "/Logon", Roles = "User|Admin")]
		public ViewResult Aliases(Authentication auth, IData dc)
		{
			ViewBag.content = string.Join(", ", GetAllRouteAliases().ToArray());

			return View("Index");
		}

		#region HELPERS
		private string WikiList(IData dc)
		{
			List<WikiTitle> titles = dc.GetAllPageTitles();

			if (titles.Count() == 0)
			{
				return "There are no pages in this wiki";
			}
			else
			{
				List<string> pageList = new List<string>();
				List<string> routeAliases = GetAllRouteAliases();

				foreach (WikiTitle p in titles)
				{
					string alias = "/" + p.Alias;

					if (!routeAliases.Contains(alias))
					{
						// Create dynamic routes based on wiki aliases
						AddRoute(alias, "Wiki", "Show", p.ID.ToString());
					}

					pageList.Add(string.Format("<a href=\"/{0}\">{1}</a>", p.Alias.ToURLEncodedString(), p.Title));
				}

				return String.Join(" | ", pageList.ToArray());
			}
		}

		private string GetWikiPageTagsAsJSArray(IData dc, WikiPage p)
		{
			List<string> mungedNames = new List<string>();
			List<string> tagNames = dc.GetPageTags(p).Select(x => x.Name).ToList();

			foreach (var t in tagNames)
			{
				mungedNames.Add(string.Format("\"{0}\"", t));
			}

			return string.Format("[{0}]", string.Join(",", mungedNames));
		}
		#endregion
	}
}