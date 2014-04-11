//
// Aurora.Extra - Additional bits that may be useful in your applications      
//
// Updated On: 9 November 2012
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
// NON-GPL code = my fork of Rob Conery's Massive which is under the 
//                "New BSD License"
//

#region LICENSE - GPL version 3 <http://www.gnu.org/licenses/gpl-3.0.html>
//
// NOTE: Aurora contains some code that is not licensed under the GPLv3. 
//       that code has been labeled with it's respective license below. 
//
// NON-GPL code = Rob Conery's Massive which is under the "New BSD License" and
//                My Gravatar fork which the original author did not include
//                a license.
//
// GNU GPLv3 quick guide: http://www.gnu.org/licenses/quick-guide-gplv3.html
//
// GNU GPLv3 license <http://www.gnu.org/licenses/gpl-3.0.html>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace Aurora.Extra
{
	#region ATTRIBUTES
	[AttributeUsage(AttributeTargets.All)]
	public sealed class MetadataAttribute : Attribute
	{
		public string Metadata { get; private set; }

		public MetadataAttribute(string metadata)
		{
			Metadata = metadata;
		}
	}

	public enum DescriptiveNameOperation
	{
		SplitCamelCase,
		None
	}

	[AttributeUsage(AttributeTargets.All)]
	public sealed class DescriptiveNameAttribute : Attribute
	{
		public string Name { get; private set; }

		public DescriptiveNameOperation Op { get; private set; }

		public DescriptiveNameAttribute(string name)
		{
			Name = name;
			Op = DescriptiveNameOperation.None;
		}

		public DescriptiveNameAttribute(DescriptiveNameOperation op)
		{
			Name = string.Empty; // Name comes from property name

			Op = op; // We'll perform an operation on the property name like put spacing between camel case names, then title case the name.
		}

		public string PerformOperation(string name)
		{
			// This regex comes from this StackOverflow question answer:
			//
			// http://stackoverflow.com/questions/155303/net-how-can-you-split-a-caps-delimited-string-into-an-array
			if (Op == DescriptiveNameOperation.SplitCamelCase)
				return Regex.Replace(name, @"([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))", "$1 ");

			return null;
		}
	}

	[AttributeUsage(AttributeTargets.Property)]
	public sealed class DateFormatAttribute : Attribute
	{
		public string Format { get; set; }

		public DateFormatAttribute(string format)
		{
			Format = format;
		}
	}

	[AttributeUsage(AttributeTargets.Property)]
	public sealed class StringFormatAttribute : Attribute
	{
		public string Format { get; set; }

		public StringFormatAttribute(string format)
		{
			Format = format;
		}
	}
	#endregion

	#region EXTENSION METHODS
	public static class ExtensionMethods
	{
		/// <summary>
		/// Converts a lowercase string to title case
		/// <remarks>
		/// Adapted from: http://stackoverflow.com/questions/271398/what-are-your-favorite-extension-methods-for-c-codeplex-com-extensionoverflow
		/// </remarks>
		/// </summary>
		/// <param name="value">String to convert</param>
		/// <returns>A title cased string</returns>
		public static string ToTitleCase(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return value;
			}

			System.Globalization.CultureInfo cultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
			System.Globalization.TextInfo textInfo = cultureInfo.TextInfo;

			// TextInfo.ToTitleCase only operates on the string if is all lower case, otherwise it returns the string unchanged.
			return textInfo.ToTitleCase(value.ToLower());
		}

		/// <summary>
		/// Takes a camel cased string and returns the string with spaces between the words
		/// <remarks>
		/// from http://stackoverflow.com/questions/271398/what-are-your-favorite-extension-methods-for-c-codeplex-com-extensionoverflow
		/// </remarks>
		/// </summary>
		/// <param name="camelCaseWord">The input string</param>
		/// <returns>A string with spaces between words</returns>
		public static string Wordify(this string camelCaseWord)
		{
			// if the word is all upper, just return it
			if (!Regex.IsMatch(camelCaseWord, "[a-z]"))
				return camelCaseWord;

			return string.Join(" ", Regex.Split(camelCaseWord, @"(?<!^)(?=[A-Z])"));
		}

		public static string GetMetadata(this Enum obj)
		{
			if (obj != null)
			{
				MetadataAttribute mda = (MetadataAttribute)obj.GetType().GetField(obj.ToString()).GetCustomAttributes(false).FirstOrDefault(x => x is MetadataAttribute);

				if (mda != null)
				{
					return mda.Metadata;
				}
			}

			return null;
		}

		public static string GetDescriptiveName(this Enum obj)
		{
			if (obj != null)
			{
				DescriptiveNameAttribute dna = (DescriptiveNameAttribute)obj.GetType().GetField(obj.ToString()).GetCustomAttributes(false).FirstOrDefault(x => x is DescriptiveNameAttribute);

				if (dna != null)
					return dna.Name;
			}

			return null;
		}
	}
	#endregion

	#region ENCRYPTION
	public static class Encryption
	{
		private static byte[] GetPassphraseHash(string passphrase, int size)
		{
			byte[] phash;

			using (SHA1CryptoServiceProvider hashsha1 = new SHA1CryptoServiceProvider())
			{
				phash = hashsha1.ComputeHash(ASCIIEncoding.ASCII.GetBytes(passphrase));
				Array.Resize(ref phash, size);
			}

			return phash;
		}

		public static string Encrypt(string original, string key)
		{
			string encrypted = string.Empty;

			using (TripleDESCryptoServiceProvider des = new TripleDESCryptoServiceProvider())
			{
				des.Key = GetPassphraseHash(key, des.KeySize / 8);
				des.IV = GetPassphraseHash(key, des.BlockSize / 8);
				des.Padding = PaddingMode.PKCS7;
				des.Mode = CipherMode.ECB;

				byte[] buff = ASCIIEncoding.ASCII.GetBytes(original);
				encrypted = Convert.ToBase64String(des.CreateEncryptor().TransformFinalBlock(buff, 0, buff.Length));
			}

			return encrypted;
		}

		public static string Decrypt(string encrypted, string key)
		{
			string decrypted = string.Empty;

			using (TripleDESCryptoServiceProvider des = new TripleDESCryptoServiceProvider())
			{
				des.Key = GetPassphraseHash(key, des.KeySize / 8);
				des.IV = GetPassphraseHash(key, des.BlockSize / 8);
				des.Padding = PaddingMode.PKCS7;
				des.Mode = CipherMode.ECB;

				byte[] buff = Convert.FromBase64String(encrypted);
				decrypted = ASCIIEncoding.ASCII.GetString(des.CreateDecryptor().TransformFinalBlock(buff, 0, buff.Length));
			}

			return decrypted;
		}
	}
	#endregion

	#region HTML HELPERS

	public enum HtmlInputType
	{
		[Metadata("<input type=\"button\" {0} />")]
		Button,

		[Metadata("<input type=\"checkbox\" {0} />")]
		CheckBox,

		[Metadata("<input type=\"file\" {0} />")]
		File,

		[Metadata("<input type=\"hidden\" {0} />")]
		Hidden,

		[Metadata("<input type=\"image\" {0} />")]
		Image,

		[Metadata("<input type=\"password\" {0} />")]
		Password,

		[Metadata("<input type=\"radio\" {0} />")]
		Radio,

		[Metadata("<input type=\"reset\" {0} />")]
		Reset,

		[Metadata("<input type=\"submit\" {0} />")]
		Submit,

		[Metadata("<input type=\"text\" {0} />")]
		Text,

		[Metadata("<textarea {0}>{1}</textarea>")]
		TextArea
	}

	public enum HtmlFormPostMethod
	{
		Get,
		Post
	}

	#region ABSTRACT BASE HELPER
	public abstract class HtmlBase
	{
		protected Dictionary<string, string> AttribsDict;
		protected Func<string, string>[] AttribsFunc;

		public string CondenseAttribs()
		{
			return (AttribsFunc != null) ? GetParams() : string.Empty;
		}

		private string GetParams()
		{
			StringBuilder sb = new StringBuilder();

			Dictionary<string, string> attribs = new Dictionary<string, string>();

			if (AttribsFunc != null)
			{
				foreach (Func<string, string> f in AttribsFunc)
				{
					attribs.Add(f.Method.GetParameters()[0].Name == "@class" ? "class" : f.Method.GetParameters()[0].Name, f(null));
				}
			}
			else if (AttribsDict != null)
			{
				attribs = AttribsDict;
			}

			foreach (KeyValuePair<string, string> kvp in attribs)
			{
				sb.AppendFormat("{0}=\"{1}\" ", kvp.Key, kvp.Value);
			}

			if (sb.Length > 0)
			{
				return sb.ToString().Trim();
			}

			return null;
		}
	}
	#endregion

	#region HTMLTABLE HELPER
	internal enum ColumnTransformType
	{
		New,
		Existing
	}

	public class RowTransform<T> where T : Model
	{
		private List<T> Models;
		private Func<T, string> Func;

		public RowTransform(List<T> models, Func<T, string> func)
		{
			Models = models;
			Func = func;
		}

		public string Result(int index)
		{
			return Func(Models[index]);
		}

		public IEnumerable<string> Results()
		{
			foreach (T t in Models)
			{
				yield return Func(t);
			}
		}
	}

	public class ColumnTransform<T> where T : Model
	{
		private List<T> Models;
		private Func<T, string> TransformFunc;
		private PropertyInfo ColumnInfo;
		internal ColumnTransformType TransformType { get; private set; }

		public string ColumnName { get; private set; }

		public ColumnTransform(List<T> models, string columnName, Func<T, string> transformFunc)
		{
			Models = models;
			TransformFunc = transformFunc;
			ColumnName = columnName;
			ColumnInfo = typeof(T).GetProperties().FirstOrDefault(x => x.Name == ColumnName);

			if (ColumnInfo != null)
			{
				TransformType = ColumnTransformType.Existing;
			}
			else
			{
				TransformType = ColumnTransformType.New;
			}
		}

		public string Result(int index)
		{
			return TransformFunc(Models[index]);
		}

		public IEnumerable<string> Results()
		{
			foreach (T t in Models)
			{
				yield return TransformFunc(t);
			}
		}
	}

	public class HtmlTable<T> : HtmlBase where T : Model
	{
		private List<T> Models;
		private List<string> PropertyNames;
		private List<PropertyInfo> PropertyInfos;
		private List<string> IgnoreColumns;
		private List<ColumnTransform<T>> ColumnTransforms;
		private List<RowTransform<T>> RowTransforms;
		public string AlternateRowColor { get; set; }
		public bool AlternateRowColorEnabled { get; set; }

		public HtmlTable(List<T> models, bool alternateRowColorEnabled, params Func<string, string>[] attribs)
		{
			AlternateRowColorEnabled = alternateRowColorEnabled;
			Init(models, null, null, null, attribs);
		}

		public HtmlTable(List<T> models,
										 List<string> ignoreColumns,
										 List<ColumnTransform<T>> columnTransforms,
										 params Func<string, string>[] attribs)
		{
			AlternateRowColorEnabled = true;
			Init(models, ignoreColumns, columnTransforms, null, attribs);
		}

		public HtmlTable(List<T> models,
										 List<string> ignoreColumns,
										 List<ColumnTransform<T>> columnTransforms,
										 List<RowTransform<T>> rowTransforms,
										 params Func<string, string>[] attribs)
		{
			AlternateRowColorEnabled = true;
			Init(models, ignoreColumns, columnTransforms, rowTransforms, attribs);
		}

		private void Init(List<T> models,
											List<string> ignoreColumns,
											List<ColumnTransform<T>> columnTransforms,
											List<RowTransform<T>> rowTransforms,
											params Func<string, string>[] attribs)
		{
			Models = models;

			IgnoreColumns = ignoreColumns;
			AttribsFunc = attribs;
			ColumnTransforms = columnTransforms;
			RowTransforms = rowTransforms;
			AlternateRowColor = "#dddddd";

			PropertyNames = ObtainPropertyNames();
		}

		private List<string> ObtainPropertyNames()
		{
			PropertyNames = new List<string>();
			List<string> hasDescriptiveNames = new List<string>();

			if (Models.Count() > 0)
			{
				PropertyInfos = Model.GetPropertiesWithExclusions(Models[0].GetType(), false);

				foreach (PropertyInfo p in PropertyInfos)
				{
					DescriptiveNameAttribute pn = (DescriptiveNameAttribute)p.GetCustomAttributes(typeof(DescriptiveNameAttribute), false).FirstOrDefault();

					if ((IgnoreColumns != null) && IgnoreColumns.Contains(p.Name))
						continue;

					if (pn != null)
					{
						if (pn.Op == DescriptiveNameOperation.SplitCamelCase)
							PropertyNames.Add(CultureInfo.InvariantCulture.TextInfo.ToTitleCase(pn.PerformOperation(p.Name)));
						else
							PropertyNames.Add(pn.Name);

						hasDescriptiveNames.Add(p.Name);
					}
					else
						PropertyNames.Add(p.Name);
				}

				if (ColumnTransforms != null)
				{
					foreach (ColumnTransform<T> addColumn in ColumnTransforms)
						if ((!PropertyNames.Contains(addColumn.ColumnName)) && (!hasDescriptiveNames.Contains(addColumn.ColumnName)))
							PropertyNames.Add(addColumn.ColumnName);
				}

				if (PropertyNames.Count > 0)
					return PropertyNames;
			}

			return null;
		}

		public string ToString(int start, int length, bool displayNull)
		{
			if (start > Models.Count() ||
					start < 0 ||
					(length - start) > Models.Count() ||
					(length - start) < 0)
			{
				throw new ArgumentOutOfRangeException("The start or length is out of bounds with the model");
			}

			StringBuilder html = new StringBuilder();

			html.AppendFormat("<table {0}><thead>", CondenseAttribs());

			foreach (string pn in PropertyNames)
				html.AppendFormat("<th>{0}</th>", pn);

			html.Append("</thead><tbody>");

			for (int i = start; i < length; i++)
			{
				string rowClass = string.Empty;
				string alternatingColor = string.Empty;

				if (RowTransforms != null)
					foreach (RowTransform<T> rt in RowTransforms)
						rowClass = rt.Result(i);

				if (AlternateRowColorEnabled && !string.IsNullOrEmpty(AlternateRowColor) && (i & 1) != 0)
					alternatingColor = string.Format("bgcolor=\"{0}\"", AlternateRowColor);

				html.AppendFormat("<tr {0} {1}>", rowClass, alternatingColor);

				foreach (PropertyInfo pn in PropertyInfos)
				{
					if ((IgnoreColumns != null) && IgnoreColumns.Contains(pn.Name))
						continue;

					if (pn.CanRead)
					{
						string value = string.Empty;
						object o = pn.GetValue(Models[i], null);

						StringFormatAttribute sfa = (StringFormatAttribute)Attribute.GetCustomAttribute(pn, typeof(StringFormatAttribute));

						if (sfa != null)
							value = string.Format(sfa.Format, o);
						else
							value = (o == null) ? ((displayNull) ? "NULL" : string.Empty) : o.ToString();

						if (o is DateTime)
						{
							DateFormatAttribute dfa = (DateFormatAttribute)Attribute.GetCustomAttribute(pn, typeof(DateFormatAttribute));

							if (dfa != null)
								value = ((DateTime)o).ToString(dfa.Format);
						}

						if (ColumnTransforms != null)
						{
							ColumnTransform<T> transform = (ColumnTransform<T>)ColumnTransforms.FirstOrDefault(x => x.ColumnName == pn.Name && x.TransformType == ColumnTransformType.Existing);

							if (transform != null)
								value = transform.Result(i);
						}

						html.AppendFormat("<td>{0}</td>", value);
					}
				}

				if (ColumnTransforms != null)
				{
					foreach (ColumnTransform<T> ct in ColumnTransforms.Where(x => x.TransformType == ColumnTransformType.New))
						html.AppendFormat("<td>{0}</td>", ct.Result(i));
				}

				html.Append("</tr>");
			}

			html.Append("</tbody></table>");

			return html.ToString();
		}

		public override string ToString()
		{
			return ToString(0, Models.Count(), false);
		}
	}
	#endregion

	#region CHECKBOX AND RADIO BUTTON LIST (NOT FINISHED)
	public class HtmlCheckBoxList : HtmlBase
	{
		public List<HtmlListItem> Items { get; private set; }

		public HtmlCheckBoxList()
		{
			Items = new List<HtmlListItem>();
		}

		public void AddItem(HtmlListItem item)
		{
			Items.Add(item);
		}

		public string ToString(bool lineBreak)
		{
			StringBuilder sb = new StringBuilder();
			int counter = 0;

			string br = (lineBreak) ? "<br />" : string.Empty;

			foreach (HtmlListItem i in Items)
			{
				sb.AppendFormat("<input type=\"checkbox\" name=\"checkboxItem{0}\" value=\"{1}\">{2}</input>{3}", counter, i.Value, i.Text, br);
				counter++;
			}

			return sb.ToString();
		}

		public override string ToString()
		{
			return ToString(true);
		}
	}

	public class HtmlRadioButtonList : HtmlBase
	{
		public List<HtmlListItem> Items { get; private set; }

		public HtmlRadioButtonList()
		{
			Items = new List<HtmlListItem>();
		}

		public void AddItem(HtmlListItem item)
		{
			Items.Add(item);
		}

		public string ToString(bool lineBreak)
		{
			StringBuilder sb = new StringBuilder();
			int counter = 0;

			string br = (lineBreak) ? "<br />" : string.Empty;

			foreach (HtmlListItem i in Items)
			{
				sb.AppendFormat("<input type=\"radio\" name=\"radioItem{0}\" value=\"{1}\">{2}</input>{3}", counter, i.Value, i.Text, br);
				counter++;
			}

			return sb.ToString();
		}

		public override string ToString()
		{
			return ToString(true);
		}
	}
	#endregion

	#region MISC HELPERS
	public class HtmlAnchor : HtmlBase
	{
		private string Url;
		private string Description;

		public HtmlAnchor(string url, string description, params Func<string, string>[] attribs)
		{
			Url = url;
			Description = description;
			AttribsFunc = attribs;
		}

		public override string ToString()
		{
			return string.Format("<a {0} href=\"{1}\">{2}</a>", CondenseAttribs(), Url, Description);
		}
	}

	public class HtmlInput : HtmlBase
	{
		private HtmlInputType InputType;

		public HtmlInput(HtmlInputType type, params Func<string, string>[] attribs)
		{
			AttribsFunc = attribs;
			InputType = type;
		}

		public override string ToString()
		{
			if (InputType == HtmlInputType.TextArea)
			{
				return string.Format(InputType.GetMetadata(), CondenseAttribs(), string.Empty);
			}

			return string.Format(InputType.GetMetadata(), CondenseAttribs());
		}

		public string ToString(string text)
		{
			if (InputType == HtmlInputType.TextArea)
			{
				return string.Format(InputType.GetMetadata(), CondenseAttribs(), text);
			}

			return string.Format(InputType.GetMetadata(), CondenseAttribs());
		}
	}

	public class HtmlForm : HtmlBase
	{
		private string Action;
		private HtmlFormPostMethod Method;
		private List<string> InputTags;

		public HtmlForm(string action, HtmlFormPostMethod method, List<string> inputTags, params Func<string, string>[] attribs)
		{
			Action = action;
			Method = method;
			AttribsFunc = attribs;
			InputTags = inputTags;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendFormat("<form action=\"{0}\" method=\"{1}\" {2}>", Action, Method, CondenseAttribs());

			foreach (string i in InputTags)
			{
				sb.Append(i);
			}

			sb.Append("</form>");

			return sb.ToString();
		}
	}

	public class HtmlSpan : HtmlBase
	{
		private string Contents;

		public HtmlSpan(string contents, params Func<string, string>[] attribs)
		{
			Contents = contents;
			AttribsFunc = attribs;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendFormat("<span {0}>{1}</span>", CondenseAttribs(), Contents);

			return sb.ToString();
		}
	}

	public class HtmlSelect : HtmlBase
	{
		private List<string> Options;
		private string SelectedDefault;
		private bool EmptyOption;
		private string Enabled;

		public HtmlSelect(List<string> options, string selectedDefault, bool emptyOption, bool enabled, params Func<string, string>[] attribs)
		{
			Options = options;
			AttribsFunc = attribs;
			SelectedDefault = selectedDefault ?? string.Empty;
			EmptyOption = emptyOption;
			Enabled = (enabled) ? "disabled=\"disabled\"" : string.Empty;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendFormat("<select {0} {1}>", CondenseAttribs(), Enabled);

			if (EmptyOption)
			{
				sb.Append("<option selected=\"selected\"></option>");
			}

			int count = 0;

			foreach (string o in Options)
			{
				string selected = string.Empty;

				if (!string.IsNullOrEmpty(SelectedDefault) && o == SelectedDefault)
				{
					selected = "selected=\"selected\"";
				}

				sb.AppendFormat("<option name=\"opt{0}\" {1}>{2}</option>", count, selected, o);
				count++;
			}

			sb.Append("</select>");

			return sb.ToString();
		}
	}

	public class HtmlCheckBox : HtmlBase
	{
		private string ID;
		private string Name;
		private string CssClass;
		private string Check;
		private string Enabled;

		public HtmlCheckBox(string id, string name, string cssClass, bool enabled, bool check)
		{
			ID = id;
			Name = name;
			CssClass = cssClass;
			Check = (check) ? "checked=\"checked\"" : string.Empty;
			Enabled = (enabled) ? "disabled=\"disabled\"" : string.Empty;
		}

		public override string ToString()
		{
			return string.Format("<input type=\"checkbox\" id=\"{0}\" name=\"{1}\" class=\"{2}\" {3} {4} />", ID, Name, CssClass, Check, Enabled);
		}
	}

	public class HtmlListItem
	{
		public string Text { get; set; }
		public string Value { get; set; }
	}

	public class HtmlImage : HtmlBase
	{
		public string Src { get; set; }

		public HtmlImage(string src) : this(src, null) { }

		public HtmlImage(string src, params Func<string, string>[] attribs)
		{
			Src = src;
			AttribsFunc = attribs;
		}

		public override string ToString()
		{
			return string.Format("<img src=\"{0}\" {1}/>", Src, CondenseAttribs());
		}
	}
	#endregion

	#endregion

	#region PLUGIN MANAGEMENT

	public enum PluginDevelopmentStatus
	{
		PreAlpha,
		Alpha,
		Beta,
		RC,
		Stable
	}

	public interface IPluginHost
	{
		string HostName { get; }
		string HostVersion { get; }
	}

	public interface IPlugin<T>
	{
		void Load(T host);
		void Unload();
	}

	public abstract class Plugin<T> : IPlugin<T> where T : IPluginHost
	{
		public T Host { get; protected set; }

		public string Guid { get; protected set; }
		public string Name { get; protected set; }
		public string[] Authors { get; protected set; }
		public string Website { get; protected set; }
		public string Version { get; protected set; }
		public PluginDevelopmentStatus DevelopmentStatus { get; protected set; }
		public DateTime DevelopmentDate { get; protected set; }
		public bool Enabled { get; protected set; }
		public string ShortDescription { get; protected set; }
		public string LongDescription { get; protected set; }

		public abstract void Load(T host);
		public abstract void Unload();
	}

	public sealed class PluginManager<T>
	{
		public List<IPlugin<T>> Plugins { get; private set; }
		public T Host { get; private set; }

		public PluginManager(T host)
		{
			Host = host;
		}

		public void LoadPlugin(string path)
		{
			if (Plugins == null)
			{
				Plugins = new List<IPlugin<T>>();
			}

			if (File.Exists(path))
			{
				FileInfo fi = new FileInfo(path);

				if (fi.Extension.Equals(".dll"))
				{
					try
					{
						Assembly pluginAssembly = Assembly.LoadFrom(fi.FullName);

						string ipluginFullName = typeof(IPlugin<>).FullName;

						var pluginsInAssembly = pluginAssembly.GetTypes()
							.Where(x => x.GetInterface(ipluginFullName, false) != null);

						if (pluginsInAssembly.Count() > 0)
						{
							foreach (Type t in pluginsInAssembly)
							{
								if (t.IsPublic && !t.IsAbstract)
								{
									Type ti = t.GetInterface(ipluginFullName, false);

									if (ti != null)
									{
										IPlugin<T> p = (IPlugin<T>)Activator.CreateInstance(pluginAssembly.GetType(t.ToString()));

										p.Load(Host);

										Plugins.Add(p);
									}
									else
									{
										throw new PluginException(string.Format("{0} does not implement the IPlugin interface", t.Name));
									}
								}
							}
						}
						else
						{
							throw new PluginException(string.Format("There are no plugins in {0}", fi.Name));
						}
					}
					catch (Exception ex)
					{
						if (ex is BadImageFormatException || ex is ReflectionTypeLoadException)
						{
							throw new PluginException(string.Format("Unable to load {0}", fi.Name));
						}

						throw;
					}
				}
			}
		}

		public void LoadPlugins(string path)
		{
			if (Directory.Exists(path))
			{
				foreach (FileInfo fi in new DirectoryInfo(path).GetFiles())
				{
					LoadPlugin(fi.FullName);
				}
			}
		}

		public void UnloadPlugins()
		{
			foreach (IPlugin<T> p in Plugins)
			{
				p.Unload();
				Plugins.Remove(p);
			}
		}
	}

	public class PluginException : Exception
	{
		public PluginException(string message)
			: base(message)
		{
		}

		public PluginException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
	#endregion
}