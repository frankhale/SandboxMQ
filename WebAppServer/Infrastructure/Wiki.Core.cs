using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Wiki.Infrastructure.Core
{
	public static class ExtensionMethods
	{
		public static string NewLinesToBR(this string value)
		{
			return value.Trim().Replace("\n", "<br />");
		}

		public static string ToURLEncodedString(this string value)
		{
			return HttpUtility.UrlEncode(value);
		}

		public static string ToURLDecodedString(this string value)
		{
			return HttpUtility.UrlDecode(value);
		}

		public static string ToHtmlEncodedString(this string value)
		{
			return HttpUtility.HtmlEncode(value);
		}

		public static string ToHtmlDecodedString(this string value)
		{
			return HttpUtility.HtmlDecode(value);
		}
	}
}