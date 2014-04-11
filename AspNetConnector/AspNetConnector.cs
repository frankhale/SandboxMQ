//
// This is a thin wrapper around the ASP.NET request and response cycle. It's
// purpose is to facilitate the exploration of web framework design off of the
// ASP.NET stack while still relying on ASP.NET stack to manage HTTP traffic.
// 
// Once a request comes in it is transformed to a simple JSON string and sent
// to a client listening on port 9000 (hardcoded for now). The distant client
// is responsible for consuming the request and producing a JSON response which 
// is sent back here and then delegated to ASP.NET for sending over the wire to 
// the client.
// 
// The "web framework/web app" is any application that can consume the request
// and produce a response.
//
// Frank Hale <frankhale@gmail.com>
// http://github.com/frankhale
// 9 November 2012
//

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using Microsoft.Web.Infrastructure.DynamicValidationHelper;
using Newtonsoft.Json;
using ZMQ;

namespace AspNetConnector
{
	public sealed class PostedFile
	{
		public string ContentType { get; set; }
		public string FileName { get; set; }
		public byte[] FileBytes { get; set; }
	}

	public class Application
	{
		public Action<string, object> AddSessionCallback { get; set; }
		public Func<string, object> GetSessionCallback { get; set; }
		public Action<string> RemoveSessionCallback { get; set; }
		public Action<string, object> AddApplicationCallback { get; set; }
		public Func<string, object> GetApplicationCallback { get; set; }
		public Action<string> RemoveApplicationCallback { get; set; }
		public Action<Response> ResponseCallback { get; set; }
	}

	public class Request
	{
		public Dictionary<string, string> ServerVariables { get; set; }
		public Dictionary<string, string> Headers { get; set; }
		public Dictionary<string, string> QueryString { get; set; }
		public Dictionary<string, string> Cookies { get; set; }
		public Dictionary<string, string> Body { get; set; }
		public Dictionary<string, string> Form { get; set; }

		public List<PostedFile> Files { get; set; }

		// SessionID is only used as a unique key to identify series of requests.
		// The ASP.NET Connector Driver simply puts a value in a session so that
		// an ASP.NET session cookie will be placed on the client. All real session
		// management is done in the application on the other end of the zmq socket.
		public string SessionID { get; set; }
		public string UrlScheme { get; set; }
		public string Url { get; set; }
		public string Method { get; set; }
		public string PathBase { get; set; }
		public string Path { get; set; }
		public string IpAddress { get; set; }
		public string Error { get; set; }
		public string ErrorStackTrace { get; set; }
		public string Identity { get; set; }

		public byte[] ClientCertificate { get; set; }

		public bool IsSecure { get; set; }
	}

	public class Response
	{
		public Dictionary<string, string> Headers { get; set; }
		public int Status { get; set; }
		public string ContentType { get; set; }
		public string Body { get; set; }
		public byte[] Bytes { get; set; }
		// Again, ASP.NET is not managing sessions for us per se but we do delegate
		// for the session cookie and session id. This property gives us a clue 
		// (scroll down) when we are generating the response to abandon the given 
		// session which removes the session cookie from the client.
		public bool AbandonSession { get; set; }
	}

	public class HttpContextConnector
	{
		private HttpContext context;
		private NameValueCollection unvalidatedForm, unvalidatedQueryString;

		public HttpContextConnector(HttpContext ctx)
		{
			context = ctx;

			SetupUnvalidatedFormAndQueryStringCollections();

			using (var zcontext = new Context(1))
			using (Socket requester = zcontext.Socket(SocketType.REQ))
			{
				var request = BuildRequest();
				var requestJson = JsonConvert.SerializeObject(request);

				// this url is a great thing to put in the web.config
				requester.Connect("tcp://localhost:9000");
				requester.Send(requestJson, Encoding.Unicode);
				
				var responseJson = requester.Recv(Encoding.Unicode);

				var response = JsonConvert.DeserializeObject<Response>(responseJson.Decompress());

				RenderResponse(response);
			}
		}

		private Request BuildRequest()
		{
			var request = new Request();

			request.SessionID = context.Session != null ? context.Session.SessionID : Guid.NewGuid().ToString().Replace("-", "");
			request.ServerVariables = NameValueCollectionToDictionary(context.Request.ServerVariables);
			request.UrlScheme = context.Request.Url.Scheme;
			request.IsSecure = context.Request.IsSecureConnection;
			request.Headers = StringToDictionary(context.Request.ServerVariables["ALL_HTTP"], '\n', ':');
			request.Method = context.Request.HttpMethod;
			request.Path = context.Request.Path;
			request.QueryString = NameValueCollectionToDictionary(unvalidatedQueryString);
			request.Cookies = StringToDictionary(context.Request.ServerVariables["HTTP_COOKIE"], ';', '=');
			request.Body = StringToDictionary(new StreamReader(context.Request.InputStream).ReadToEnd(), '&', '=');
			request.Form = NameValueCollectionToDictionary(unvalidatedForm);
			request.IpAddress = GetIPAddress();
			request.Files = GetRequestFiles();
			request.Url = context.Request.Url.ToString();
			request.Error = (context.Server.GetLastError() != null) ? context.Server.GetLastError().Message : null;
			request.ErrorStackTrace = (context.Server.GetLastError() != null) ? context.Server.GetLastError().GetStackTrace() : null;
			request.Identity = (context.User != null) ? context.User.Identity.Name : null;
			request.ClientCertificate = (context.Request.ClientCertificate != null) ? context.Request.ClientCertificate.Certificate : null;

			return request;
		}

		private Dictionary<string, string> NameValueCollectionToDictionary(NameValueCollection nvc)
		{
			Dictionary<string, string> result = new Dictionary<string, string>();

			foreach (string key in nvc.AllKeys)
				if (!result.ContainsKey(key))
					result[key] = nvc.Get(key);

			return result;
		}

		private Dictionary<string, string> StringToDictionary(string value, char splitOn, char delimiter)
		{
			Dictionary<string, string> result = new Dictionary<string, string>();

			if (!string.IsNullOrEmpty(value))
			{
				foreach (string[] arr in value.Split(new char[] { splitOn }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split(delimiter)))
					if (!result.ContainsKey(arr[0]))
						result.Add(arr[0].Trim(), arr[1].Trim());
			}

			return result;
		}

		private string GetIPAddress()
		{
			// This method is based on the following example at StackOverflow:
			// http://stackoverflow.com/questions/735350/how-to-get-a-users-client-ip-address-in-asp-net
			string ip = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

			return (string.IsNullOrEmpty(ip)) ? context.Request.ServerVariables["REMOTE_ADDR"] : ip.Split(',')[0];
		}

		private List<PostedFile> GetRequestFiles()
		{
			List<PostedFile> postedFiles = new List<PostedFile>();

			foreach (HttpPostedFileBase pf in context.Request.Files)
			{
				postedFiles.Add(new PostedFile()
				{
					ContentType = pf.ContentType,
					FileName = pf.FileName,
					FileBytes = ReadStream(pf.InputStream)
				});
			}

			return (postedFiles.Count > 0) ? postedFiles : null;
		}

		// It's likely I grabbed this from Stackoverflow but I cannot remember
		private byte[] ReadStream(Stream stream)
		{
			int length = (int)stream.Length;
			byte[] buffer = new byte[length];
			int count, sum = 0;

			while ((count = stream.Read(buffer, sum, length - sum)) > 0)
				sum += count;

			return buffer;
		}

		private void SetupUnvalidatedFormAndQueryStringCollections()
		{
			ValidationUtility.EnableDynamicValidation(context.ApplicationInstance.Context);

			Func<NameValueCollection> _unvalidatedForm, _unvalidatedQueryString;

			ValidationUtility.GetUnvalidatedCollections(context.ApplicationInstance.Context, out _unvalidatedForm, out _unvalidatedQueryString);

			unvalidatedForm = _unvalidatedForm();
			unvalidatedQueryString = _unvalidatedQueryString();
		}

		private void RenderResponse(Response response)
		{
			response.ThrowIfArgumentNull();

			context.Response.AddHeader("X_FRAME_OPTIONS", "SAMEORIGIN");
			context.Response.StatusCode = response.Status;
			context.Response.ContentType = response.ContentType;

			if (response.AbandonSession)
			{
				context.Session.Abandon();
				context.Response.Cookies.Add(new HttpCookie("ASP.NET_SessionId", ""));
			}

			if (response.Headers != null)
			{
				foreach (KeyValuePair<string, string> kvp in response.Headers)
					context.Response.AddHeader(kvp.Key, kvp.Value);
			}

			try
			{
				if (!string.IsNullOrEmpty(response.Body))
					context.Response.Write(response.Body);
				else if (response.Bytes != null)
					context.Response.BinaryWrite(response.Bytes);
			}
			catch (System.Exception ex)
			{
				if (!(ex is ThreadAbortException))
				{
					// Send request with error information
					// Get response and render that
					context.Response.Write("Oops... An error has occurred!<br/><br/>" + ex.GetStackTrace());
				}
			}
		}
	}

	#region EXTENSION METHODS
	public static class ExtensionMethods
	{
		public static void ThrowIfArgumentNull<T>(this T t, string message = null)
		{
			string argName = t.GetType().Name;

			if (t == null)
				throw new ArgumentNullException(argName, message);
			else if ((t is string) && (t as string) == string.Empty)
				throw new ArgumentException(argName, message);
		}

		public static string GetStackTrace(this System.Exception exception)
		{
			string result = null;

			if (exception != null)
			{
				StringBuilder stackTraceBuilder = new StringBuilder();

				var trace = new System.Diagnostics.StackTrace((exception.InnerException != null) ? exception.InnerException : exception, true);

				foreach (StackFrame sf in trace.GetFrames())
					if (!string.IsNullOrEmpty(sf.GetFileName()))
						stackTraceBuilder.AppendFormat("<b>method:</b> {0} <b>file:</b> {1}<br />", sf.GetMethod().Name, System.IO.Path.GetFileName(sf.GetFileName()));

				result = stackTraceBuilder.ToString();
			}

			return result;
		}

		#region string compression using gzipstream
		// Borrowed from:
		// http://madskristensen.net/post/Compress-and-decompress-strings-in-C.aspx
		public static string Compress(this string text)
		{
			byte[] buffer = Encoding.UTF8.GetBytes(text);
			MemoryStream ms = new MemoryStream();
			using (GZipStream zip = new GZipStream(ms, CompressionMode.Compress, true))
			{
				zip.Write(buffer, 0, buffer.Length);
			}

			ms.Position = 0;
			MemoryStream outStream = new MemoryStream();

			byte[] compressed = new byte[ms.Length];
			ms.Read(compressed, 0, compressed.Length);

			byte[] gzBuffer = new byte[compressed.Length + 4];
			System.Buffer.BlockCopy(compressed, 0, gzBuffer, 4, compressed.Length);
			System.Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gzBuffer, 0, 4);
			return Convert.ToBase64String(gzBuffer);
		}

		public static string Decompress(this string compressedText)
		{
			byte[] gzBuffer = Convert.FromBase64String(compressedText);
			using (MemoryStream ms = new MemoryStream())
			{
				int msgLength = BitConverter.ToInt32(gzBuffer, 0);
				ms.Write(gzBuffer, 4, gzBuffer.Length - 4);

				byte[] buffer = new byte[msgLength];

				ms.Position = 0;
				using (GZipStream zip = new GZipStream(ms, CompressionMode.Decompress))
				{
					zip.Read(buffer, 0, buffer.Length);
				}

				return Encoding.UTF8.GetString(buffer);
			}
		}
		#endregion
	}
	#endregion
}