using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

using Aurora;
using AspNetConnector;
using Newtonsoft.Json;
using ZMQ;

namespace WebAppServer
{
	internal class WebXml
	{
		public string AppName { get; set; }
		public string AppPath { get; set; }
		public string AppIPAddress { get; set; }
		public string AppPort { get; set; }
	}

	public class AppFrameworkServer
	{
		private Application app;
		private WebXml webxml;
		private static string webxmlFileName = "web.xml";
		private Dictionary<string, object> application;
		private Dictionary<string, Dictionary<string, object>> session;
		private Request request;
		private Socket listener;

		public AppFrameworkServer()
		{
			application = new Dictionary<string, object>();
			session = new Dictionary<string, Dictionary<string, object>>();

			app = new Application()
			{
				AddApplicationCallback = AddApplication,
				AddSessionCallback = AddSession,
				GetApplicationCallback = GetApplication,
				GetSessionCallback = GetSession,
				RemoveApplicationCallback = RemoveApplication,
				RemoveSessionCallback = RemoveSession,
				ResponseCallback = RenderResponse
			};

			#region WEB.XML
			if (File.Exists(webxmlFileName))
			{
				XDocument config = XDocument.Load(webxmlFileName);

				try
				{
					webxml = (from e in config.Descendants("server").Descendants("app")
										select new WebXml
										{
											AppName = (e.Attribute("name") != null) ? e.Attribute("name").Value : null,
											AppPath = (e.Attribute("path") != null) ? e.Attribute("path").Value : null,
											AppIPAddress = (e.Attribute("ipaddress") != null) ? e.Attribute("ipaddress").Value : null,
											AppPort = (e.Attribute("port") != null) ? e.Attribute("port").Value : null
										}).Single();
				}
				catch (System.Exception ex)
				{
					Console.WriteLine(ex.Message);
				}

				if (!Directory.Exists(webxml.AppPath))
					throw new DirectoryNotFoundException("The app path in the web.xml does not exist.");
			}
			#endregion
		}

		public void Run()
		{
			using (var context = new Context(1))
			{
				using (listener = context.Socket(SocketType.REP))
				{
					string url = string.Format("{0}:{1}", webxml.AppIPAddress, webxml.AppPort);

					listener.Bind(string.Format("tcp://{0}", url));

					Console.WriteLine("[{0}] : {1} is waiting for requests", url, webxml.AppName);

					while (true)
					{
						string requestJson = listener.Recv(Encoding.Unicode);

						request = JsonConvert.DeserializeObject<Request>(requestJson);

						if (request != null)
						{
							request.PathBase = webxml.AppPath;

							User user = GetSession("CURRENT_USER") as User;

							Console.WriteLine("[{0}] : {1} -> {2}", (user != null) ? user.Name : request.SessionID, request.Method, request.Path);
							
							new Aurora.Engine().Init(app, request);
						}
						else
							SendErrorResponse();
					}
				}
			}
		}

		private void SendErrorResponse()
		{
			var rep = new Response()
			{
				ContentType = "text/html",
				Body = "Could not process this request",
				Status = 500
			};

			listener.Send(JsonConvert.SerializeObject(rep), Encoding.Unicode);
		}

		#region APPLICATION STORE
		public void AddApplication(string key, object value)
		{
			if (!string.IsNullOrEmpty(key))
			{
				lock (application)
				{
					application[key] = value;
				}
			}
		}

		public object GetApplication(string key)
		{
			if (!string.IsNullOrEmpty(key) && application.ContainsKey(key))
				return application[key];

			return null;
		}

		public void RemoveApplication(string key)
		{
			if (!string.IsNullOrEmpty(key) && application.ContainsKey(key))
			{
				lock (application)
				{
					application.Remove(key);
				}
			}
		}
		#endregion

		#region SESSION STORE
		public void AddSession(string key, object value)
		{
			if (!string.IsNullOrEmpty(request.SessionID) && !string.IsNullOrEmpty(key))
			{
				lock (session)
				{
					if (!session.ContainsKey(request.SessionID))
						session[request.SessionID] = new Dictionary<string, object>();

					session[request.SessionID][key] = value;
				}
			}
		}

		public object GetSession(string key)
		{
			if (!string.IsNullOrEmpty(request.SessionID) && !string.IsNullOrEmpty(key))
			{
				if (session.ContainsKey(request.SessionID))
				{
					if (session[request.SessionID] != null && session[request.SessionID].ContainsKey(key))
						return session[request.SessionID][key];
				}
			}

			return null;
		}

		public void RemoveSession(string key)
		{
			if (!string.IsNullOrEmpty(request.SessionID) && !string.IsNullOrEmpty(key))
			{
				lock (session)
				{
					if (session.ContainsKey(request.SessionID))
					{
						if (session[request.SessionID] != null && session[request.SessionID].ContainsKey(key))
							session[request.SessionID].Remove(key);
					}
				}
			}
		}
		#endregion

		public void RenderResponse(Response response)
		{
			if (response != null)
			{
				if (response.Headers != null && response.Headers.ContainsKey("Location"))
				{
					Console.WriteLine("Redirect: " + response.Headers["Location"]);
					response.Status = (int)HttpStatusCode.Redirect;
				}
				else
					Console.WriteLine("Served: " + request.Path);

				listener.Send(JsonConvert.SerializeObject(response).Compress(), Encoding.Unicode);
			}
		}

		static void Main(string[] args)
		{
			new AppFrameworkServer().Run();
		}
	}
}
