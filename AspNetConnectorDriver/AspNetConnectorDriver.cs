using System;
using System.Web;
using System.Web.SessionState;

namespace AspNetConnector
{
	public sealed class AspNetConnectorHandler : IHttpHandler, IRequiresSessionState
	{
		public bool IsReusable
		{
			get
			{
				return false;
			}
		}

		public void ProcessRequest(HttpContext context)
		{
			if (context.Session["__AspNetConnector__"] == null)
				context.Session["__AspNetConnector__"] = true;

			new HttpContextConnector(context);
		}
	}

	public sealed class AspNetConnectorModule : IHttpModule
	{
		public void Dispose() { }

		public void Init(HttpApplication context)
		{
			context.Error += new EventHandler(app_Error);
		}

		private void app_Error(object sender, EventArgs e)
		{
			HttpContext context = HttpContext.Current;
			new HttpContextConnector(context);
			context.Server.ClearError();
		}
	}
}