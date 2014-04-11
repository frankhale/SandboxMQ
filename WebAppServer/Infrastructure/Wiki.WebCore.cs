using System;
using System.Linq;

#region AURORA
using Aurora;
using Aurora.Extra;
using Aurora.ActiveDirectory;
#endregion

using Wiki.Models;
using Wiki.Models.Massive;

namespace Wiki.Infrastructure.WebCore
{
	public interface IAuthentication
	{
		bool Authenticated { get; }
		string Identifier { get; }
		string Name { get; }
	}

	public class Authentication : IAuthentication, IBoundToAction
	{
		public bool Authenticated { get; private set; }
		public string Identifier { get; private set; }
		public string Name { get; private set; }

		private RouteInfo routeInfo;

		public void ActiveDirectoryLookupHandler(object sender, ActiveDirectoryAuthenticationEventArgs args)
		{
#if DEBUG
			args.CACID = "9999999999";
			args.User = null;// ActiveDirectory.LookupUserByUpn(args.CACID);
			args.Authenticated = true;
#else
			args.User = ActiveDirectory.LookupUserByUpn(args.CACID);
#endif

			Authenticated = args.Authenticated;
			Identifier = args.CACID;
			Name = "frank"; //args.User.DisplayName;
		}

		public void Initialize(RouteInfo routeInfo)
		{
			if (routeInfo.Controller.CurrentUser == null)
			{
				this.routeInfo = routeInfo;

				(new ActiveDirectoryAuthentication(ActiveDirectoryLookupHandler)).Authenticate();
			}
			else
			{
				Authenticated = true;
				Identifier = routeInfo.Controller.CurrentUser.Special.ToString();
				Name = routeInfo.Controller.CurrentUser.Name;
			}
		}
	}

	public class WikiPageTransform : IActionParamTransform<WikiPage, int>
	{
		private IData db;

		public WikiPageTransform() { }

		public WikiPageTransform(Authentication auth, IData db)
		{
			this.db = db;
		}

		public WikiPage Transform(int id)
		{
			return db.GetPage(id);
		}
	}

	public class WikiFrontController : FrontController
	{
		public WikiFrontController()
		{
			OnInit += new EventHandler(WikiFrontController_OnInit);
			OnMissingRouteEvent += new EventHandler<RouteHandlerEventArgs>(WikiFrontController_MissingRouteEventHandler);
		}

		protected void WikiFrontController_OnInit(object sender, EventArgs args)
		{
			#region SET UP BUNDLES
			string[] wikiCSS = 
			{
				"/Resources/Styles/reset.css",
				"/Resources/Styles/style.css"
			};

			string[] wikiJS = 
			{ 
				"/Resources/Scripts/jquery-1.8.2.js"
			};

			string[] syntaxHighlighterCSS = 
			{
				"/Resources/Scripts/syntaxhighlighter_3.0.83/styles/shCore.css",
				"/Resources/Scripts/syntaxhighlighter_3.0.83/styles/shCoreEmacs.css"
			};

			string[] syntaxHighterJS =
			{
				"/Resources/Scripts/syntaxhighlighter_3.0.83/scripts/shCore.js",
				"/Resources/Scripts/syntaxhighlighter_3.0.83/scripts/shBrushCSharp.js"
			};

			string[] jqueryUIAndTagItCSS = 
			{
				"/Resources/Scripts/jquery-ui-1.9.0.custom/css/smoothness/jquery-ui-1.9.0.custom.css",
				"/Resources/Styles/tagit.css"
			};

			string[] jqueryUIAndTagItJS =
			{
				"/Resources/Scripts/jquery-ui-1.9.0.custom/js/jquery-ui-1.9.0.custom.js",
				"/Resources/Scripts/tagit.js"
			};

			AddBundle("wiki.css", wikiCSS);
			AddBundle("wiki.js", wikiJS);
			AddBundle("sh.css", syntaxHighlighterCSS);
			AddBundle("sh.js", syntaxHighterJS);
			AddBundle("tagit.css", jqueryUIAndTagItCSS);
			AddBundle("tagit.js", jqueryUIAndTagItJS);
			#endregion

			AddBindingsForAllActions("Wiki", new object[] { 
					new Authentication(),
					new MassiveDataConnector()
			});
		}

		private void WikiFrontController_MissingRouteEventHandler(object sender, RouteHandlerEventArgs e)
		{
			string p = e.Path.TrimStart('/');

			if (p.StartsWith("wiki-"))
				e.RouteInfo = FindRoute(string.Format("/Add/{0}", p));
		}

		//protected override bool CheckRoles()
		//{
		//  return false;
		//}
	}
}