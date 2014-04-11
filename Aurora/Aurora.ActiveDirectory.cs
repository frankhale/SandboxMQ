//
// Aurora.ActiveDirectory 
//
// Updated On: 21 November 2012
//
// Contact Info:
//
//  Frank Hale - <frankhale@gmail.com> 
//               <http://about.me/frank.hale>
//
// LICENSE: Unless otherwise stated all code is under the GNU GPLv3
// 
// GPL version 3 <http://www.gnu.org/licenses/gpl-3.0.html> (see below)
//

using System;
using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Aurora.Extra;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Aurora.ActiveDirectory
{
	#region WEB.CONFIG
	public class ActiveDirectoryWebConfig : ConfigurationSection
	{
		[ConfigurationProperty("EncryptionKey", DefaultValue = "", IsRequired = false)]
		public string EncryptionKey
		{
			get { return this["EncryptionKey"] as string; }
		}

		[ConfigurationProperty("UserName", DefaultValue = null, IsRequired = false)]
		public string ADSearchUser
		{
			get { return this["UserName"].ToString(); }
			set { this["UserName"] = value; }
		}

		[ConfigurationProperty("Password", DefaultValue = null, IsRequired = false)]
		public string ADSearchPW
		{
			get { return this["Password"].ToString(); }
			set { this["Password"] = value; }
		}

		[ConfigurationProperty("Domain", DefaultValue = null, IsRequired = false)]
		public string ADSearchDomain
		{
			get { return this["Domain"].ToString(); }
			set { this["Domain"] = value; }
		}

		[ConfigurationProperty("SearchRoot", DefaultValue = null, IsRequired = false)]
		public string ADSearchRoot
		{
			get { return this["SearchRoot"].ToString(); }
			set { this["SearchRoot"] = value; }
		}
	}
	#endregion

	public class ActiveDirectoryUser
	{
		public string FirstName { get; internal set; }
		public string LastName { get; internal set; }
		public string DisplayName { get; internal set; }
		public string UserName { get; internal set; }
		public string UserPrincipalName { get; internal set; }
		public string PrimaryEmailAddress { get; internal set; }
		public string PhoneNumber { get; internal set; }
		public string Path { get; internal set; }
		public X509Certificate2 ClientCertificate { get; internal set; }
	}

	public static class ActiveDirectory
	{
		public static ActiveDirectoryWebConfig webConfig = ConfigurationManager.GetSection("ActiveDirectory") as ActiveDirectoryWebConfig;
		public static string ADSearchUser = (webConfig == null) ? null : (!string.IsNullOrEmpty(webConfig.ADSearchUser) && !string.IsNullOrEmpty(webConfig.EncryptionKey)) ? Encryption.Decrypt(webConfig.ADSearchUser, webConfig.EncryptionKey) : null;
		public static string ADSearchPW = (webConfig == null) ? null : (!string.IsNullOrEmpty(webConfig.ADSearchPW) && !string.IsNullOrEmpty(webConfig.EncryptionKey)) ? Encryption.Decrypt(webConfig.ADSearchPW, webConfig.EncryptionKey) : null;
		public static string ADSearchDomain = (webConfig == null) ? null : webConfig.ADSearchDomain;

		public static ActiveDirectoryUser LookupUserByUpn(string upn)
		{
			PrincipalContext ctx = new PrincipalContext(ContextType.Domain, ADSearchDomain, ADSearchUser, ADSearchPW);
			UserPrincipal usr = UserPrincipal.FindByIdentity(ctx, IdentityType.UserPrincipalName, upn);
			ActiveDirectoryUser adUser = null;

			if (usr != null)
			{
				DirectoryEntry de = usr.GetUnderlyingObject() as DirectoryEntry;

				adUser = GetUser(de);
			}

			return adUser;
		}

		private static ActiveDirectoryUser GetUser(DirectoryEntry de)
		{
			return new ActiveDirectoryUser()
			{
				FirstName = de.Properties["givenName"].Value.ToString(),
				LastName = de.Properties["sn"].Value.ToString(),
				UserPrincipalName = (de.Properties["userPrincipalName"].Value != null) ?
							de.Properties["userPrincipalName"].Value.ToString() : null,
				DisplayName = de.Properties["displayName"].Value.ToString(),
				UserName = (de.Properties["samAccountName"].Value != null) ? de.Properties["samAccountName"].Value.ToString() : null,
				PrimaryEmailAddress = GetPrimarySMTP(de) ?? string.Empty,
				PhoneNumber = de.Properties["telephoneNumber"].Value.ToString(),
				Path = de.Path,
				ClientCertificate = de.Properties.Contains("userSMIMECertificate") ?
								new X509Certificate2(de.Properties["userSMIMECertificate"].Value as byte[]) ?? null :
								new X509Certificate2(de.Properties["userCertificate"].Value as byte[]) ?? null
			};
		}

		private static List<string> GetProxyAddresses(DirectoryEntry user)
		{
			List<string> addresses = new List<string>();

			if (user.Properties.Contains("proxyAddresses"))
			{
				foreach (string addr in user.Properties["proxyAddresses"])
					addresses.Add(Regex.Replace(addr, @"\s+", string.Empty, RegexOptions.IgnoreCase).Trim());
			}

			return addresses;
		}

		private static string GetPrimarySMTP(DirectoryEntry user)
		{
			foreach (string p in GetProxyAddresses(user))
			{
				if (p.StartsWith("SMTP:", StringComparison.Ordinal))
					return p.Replace("SMTP:", string.Empty).ToLowerInvariant();
			}

			return null;
		}
	}

	public class ActiveDirectoryAuthenticationEventArgs : EventArgs
	{
		public ActiveDirectoryUser User { get; set; }
		public bool Authenticated { get; set; }
		public string CACID { get; set; }
	}

	public class ActiveDirectoryAuthentication : IBoundToAction
	{
		private Controller controller;
		public ActiveDirectoryUser User { get; private set; }
		public bool Authenticated { get; private set; }
		public string CACID { get; private set; }

		private event EventHandler<ActiveDirectoryAuthenticationEventArgs> ActiveDirectoryLookupEvent = (sender, args) => { };

		public ActiveDirectoryAuthentication()
		{
		}

		public ActiveDirectoryAuthentication(EventHandler<ActiveDirectoryAuthenticationEventArgs> activeDirectoryLookupHandler)
		{
			ActiveDirectoryLookupEvent += activeDirectoryLookupHandler;
		}

		public void Initialize(RouteInfo routeInfo)
		{
			ActiveDirectoryLookupEvent.ThrowIfArgumentNull();

			controller = routeInfo.Controller;

			Authenticate();
		}

		public string GetCACIDFromCN()
		{
			if (controller.ClientCertificate == null)
				throw new Exception("The HttpContext.Request.ClientCertificate did not contain a valid certificate");

			string cn = controller.ClientCertificate.GetNameInfo(X509NameType.SimpleName, false);
			string cacid = string.Empty;
			bool valid = true;

			if (string.IsNullOrEmpty(cn))
				throw new Exception("Cannot determine the simple name from the client certificate");

			if (cn.Contains("."))
			{
				string[] fields = cn.Split('.');

				if (fields.Length > 0)
				{
					cacid = fields[fields.Length - 1];

					foreach (char c in cacid.ToCharArray())
					{
						if (!Char.IsDigit(c))
						{
							valid = false;
							break;
						}
					}
				}
			}

			if (valid)
				return cacid;
			else
				throw new Exception(string.Format("The CAC ID was not in the expected format within the common name (last.first.middle.cacid), actual CN = {0}", cn));
		}

		public void Authenticate()
		{
			ActiveDirectoryAuthenticationEventArgs args = new ActiveDirectoryAuthenticationEventArgs();

#if DEBUG
			ActiveDirectoryLookupEvent(this, args);

			User = args.User;
			Authenticated = args.Authenticated;
			CACID = args.CACID;
#else
			CACID = GetCACIDFromCN();

			User = null;
			Authenticated = false;

			if (!String.IsNullOrEmpty(CACID))
			{
				X509Chain chain = new X509Chain();
				chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
				chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
				chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 0, 30);

				if (chain.Build(controller.ClientCertificate))
				{
					try
					{
						args.CACID = CACID;

						ActiveDirectoryLookupEvent(this, args);

						if (args.User != null)
						{
							User = args.User;
							Authenticated = true;
						}
					}
					catch (DirectoryServicesCOMException)
					{
						throw new Exception("A problem occurred trying to communicate with Active Directory");
					}
				}
			}
#endif
		}
	}	
}
