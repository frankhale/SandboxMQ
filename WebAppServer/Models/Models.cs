using System;
using System.Collections.Generic;
using System.Linq;

using Aurora;

namespace Wiki.Models
{
	public class PageData : Model
	{
		[NotRequired]
		public int? ID { get; set; }
		[Required("The wiki title is a required field.")]
		public string Title { get; set; }

		[Unsafe]
		[Required("The wiki data is a required field.")]
		public string Data { get; set; }

		[NotRequired]
		public string Tags { get; set; }
	}

	public class WikiTitle
	{
		public int ID { get; set; }
		public string Alias { get; set; }
		public string Title { get; set; }
	}

	public class WikiPage
	{
		public int ID { get; set; }
		public DateTime CreatedOn { get; set; }
		public DateTime ModifiedOn { get; set; }
		public string Alias { get; set; }
		public int AuthorID { get; set; }
		public string Title { get; set; }
		public string Body { get; set; }
		public bool Published { get; set; }
	}

	public class WikiPageTag
	{
		public int ID { get; set; }
		public int PageID { get; set; }
		public int TagID { get; set; }
	}

	public class WikiComment
	{
		public int ID { get; set; }
		public int PageID { get; set; }
		public DateTime CreatedOn { get; set; }
		public DateTime ModifiedOn { get; set; }
		public int AuthorID { get; set; }
		public string Body { get; set; }
	}

	public class WikiTag
	{
		public int ID { get; set; }
		public DateTime CreatedOn { get; set; }
		public string Name { get; set; }
	}

	public class WikiUser
	{
		public int ID { get; set; }
		public string UserName { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public byte[] Avatar { get; set; }
		public string Identifier { get; set; }
	}

	public interface IData
	{
		#region PAGE METHODS
		List<WikiTitle> GetAllPageTitles();
		List<WikiPage> GetAllPages();
		WikiPage GetPage(int id);
		List<WikiPage> GetPages(int count);
		void AddPage(WikiPage p);
		void DeletePage(int id);
		void DeletePage(WikiPage p);
		void UpdatePage(WikiPage p);
		#endregion

		#region PAGE TAG METHODS
		List<WikiTag> GetPageTags(WikiPage p);
		void AddPageTag(WikiPage p, string name);
		void DeletePageTags(WikiPage p);
		#endregion

		#region COMMENT METHODS
		List<WikiComment> GetAllComments();
		List<WikiComment> GetComments(int pageID);
		void AddComment(WikiComment c);
		void DeleteComment(WikiComment c);
		void UpdateComment(WikiComment c);
		#endregion

		#region TAG METHODS
		List<WikiTag> GetAllTags();
		void AddTag(string name);
		void DeleteTag(WikiTag t);
		void UpdateTag(WikiTag t);
		#endregion

		#region USER METHODS
		List<WikiUser> GetAllUsers();
		WikiUser GetUserByOpenIDIdentifier(string openIDIdentifier);
		void AddUser(WikiUser u);
		void DeleteUser(WikiUser u);
		void UpdateUser(WikiUser u);
		WikiUser GetUser(string identifier);
		#endregion
	}

	#region MASSIVE
	namespace Massive
	{
		using Aurora.Massive;

		internal class Comments : DynamicModel
		{
			public Comments() : base(connectionString: @"Data Source=.\SQLEXPRESS;Initial Catalog=Wiki;Integrated Security=True") { }
		}
		internal class Pages : DynamicModel 
		{
			public Pages()
				: base(connectionString: @"Data Source=.\SQLEXPRESS;Initial Catalog=Wiki;Integrated Security=True")
			{

			}
		}
		internal class PageTags : DynamicModel
		{
			public PageTags()
				: base(connectionString: @"Data Source=.\SQLEXPRESS;Initial Catalog=Wiki;Integrated Security=True")
			{

			}
		}
		internal class Roles : DynamicModel
		{
			public Roles()
				: base(connectionString: @"Data Source=.\SQLEXPRESS;Initial Catalog=Wiki;Integrated Security=True")
			{

			}
		}
		internal class Tags : DynamicModel
		{
			public Tags()
				: base(connectionString: @"Data Source=.\SQLEXPRESS;Initial Catalog=Wiki;Integrated Security=True")
			{

			}
		}
		internal class UserRoles : DynamicModel
		{
			public UserRoles()
				: base(connectionString: @"Data Source=.\SQLEXPRESS;Initial Catalog=Wiki;Integrated Security=True")
			{

			}
		}
		internal class Users : DynamicModel
		{
			public Users()
				: base(connectionString: @"Data Source=.\SQLEXPRESS;Initial Catalog=Wiki;Integrated Security=True")
			{

			}
		}

		public class MassiveDataConnector : IData
		{
			private dynamic pages = new Pages();
			private dynamic comments = new Comments();
			private dynamic tags = new Tags();
			private dynamic roles = new Roles();
			private dynamic pageTags = new PageTags();
			private dynamic userRoles = new UserRoles();
			private dynamic users = new Users();

			#region PAGE METHODS
			public List<WikiTitle> GetAllPageTitles()
			{
				List<WikiTitle> allTitles = new List<WikiTitle>();

				foreach (var x in pages.Query("SELECT ID, Alias, Title FROM PAGES"))
				{
					allTitles.Add(new WikiTitle()
					{
						ID = x.ID,
						Alias = x.Alias,
						Title = x.Title
					});
				}

				return allTitles;
			}

			public List<WikiPage> GetAllPages()
			{
				List<WikiPage> allPages = new List<WikiPage>();

				foreach (var x in pages.All())
				{
					allPages.Add(new WikiPage()
					{
						ID = x.ID,
						CreatedOn = x.CreatedOn,
						ModifiedOn = x.ModifiedOn,
						Alias = x.Alias,
						AuthorID = x.AuthorID,
						Title = x.Title,
						Body = x.Body,
						Published = x.Published
					});
				}

				return allPages;
			}

			public WikiPage GetPage(int id)
			{
				WikiPage result = null;

				//__Page _p = db.__Pages.FirstOrDefault(x => x.ID == id);
				var _p = pages.First(ID: id);

				if (_p != null)
				{
					result = new WikiPage()
					{
						ID = _p.ID,
						CreatedOn = _p.CreatedOn,
						ModifiedOn = _p.ModifiedOn,
						Alias = _p.Alias,
						AuthorID = _p.AuthorID,
						Title = _p.Title,
						Body = _p.Body,
						Published = _p.Published
					};
				}

				return result;
			}

			public List<WikiPage> GetPages(int count)
			{
				throw new NotImplementedException();
			}

			public void AddPage(WikiPage p)
			{
				if (p == null)
				{
					throw new ArgumentNullException("p");
				}

				var x = pages.Insert(new
				{
					CreatedOn = p.CreatedOn,
					ModifiedOn = p.ModifiedOn,
					Alias = p.Alias,
					AuthorID = p.AuthorID,
					Title = p.Title,
					Body = p.Body,
					Published = p.Published
				});

				p.ID = (int)x.ID;
			}

			public void DeletePage(int id)
			{
				WikiPage p = GetPage(id);

				if (p != null)
				{
					DeletePage(p);
				}
			}

			public void DeletePage(WikiPage p)
			{
				if (p == null)
				{
					throw new ArgumentNullException("p");
				}

				pages.Delete(p.ID);
			}

			public void UpdatePage(WikiPage p)
			{
				if (p == null)
				{
					throw new ArgumentNullException("p");
				}

				var _p = new
				{
					CreatedOn = p.CreatedOn,
					ModifiedOn = p.ModifiedOn,
					Alias = p.Alias,
					AuthorID = p.AuthorID,
					Title = p.Title,
					Body = p.Body,
					Published = p.Published
				};

				pages.Update(_p, p.ID);
			}
			#endregion

			#region COMMENT METHODS
			public List<WikiComment> GetAllComments()
			{
				throw new NotImplementedException();
			}

			public List<WikiComment> GetComments(int pageID)
			{
				throw new NotImplementedException();
			}

			public void AddComment(WikiComment c)
			{
				throw new NotImplementedException();
			}

			public void DeleteComment(WikiComment c)
			{
				throw new NotImplementedException();
			}

			public void UpdateComment(WikiComment c)
			{
				throw new NotImplementedException();
			}
			#endregion

			#region PAGE TAG METHODS
			public List<WikiTag> GetPageTags(WikiPage p)
			{
				List<WikiTag> result = new List<WikiTag>();

				foreach (var pt in pageTags.Find(PageID: p.ID))
				{
					var t = tags.First(id: pt.TagID);

					if (t != null)
					{
						result.Add(new WikiTag()
						{
							ID = t.ID,
							CreatedOn = t.CreatedOn,
							Name = t.Name
						});
					}
				}

				return result;
			}

			public void AddPageTag(WikiPage p, string name)
			{
				var _p = pages.First(id: p.ID);

				if (_p != null)
				{
					var _t = tags.First(Name: name);

					if (_t != null)
					{
						pageTags.Insert(new
						{
							PageID = _p.ID,
							TagID = _t.ID
						});
					}
				}
			}

			public void DeletePageTags(WikiPage p)
			{
				pageTags.Delete(where: "PageID = @0", args: p.ID.ToString());
			}
			#endregion

			#region TAG METHODS
			public List<WikiTag> GetAllTags()
			{
				List<WikiTag> allTags = new List<WikiTag>();

				foreach (var t in tags.All())
				{
					allTags.Add(new WikiTag()
					{
						ID = t.ID,
						CreatedOn = t.CreatedOn,
						Name = t.Name
					});
				}

				return allTags;
			}

			public void AddTag(string name)
			{
				var _t = tags.First(Name: name);

				if (_t == null)
				{
					var tag = new
					{
						CreatedOn = DateTime.Now,
						Name = name
					};

					tags.Insert(tag);
				}
			}

			public void DeleteTag(WikiTag t)
			{
				throw new NotImplementedException();
			}

			public void UpdateTag(WikiTag t)
			{
				throw new NotImplementedException();
			}


			#endregion

			#region USER METHODS
			public List<WikiUser> GetAllUsers()
			{
				throw new NotImplementedException();

				//return db.__Users.Select(x => new WikiUser()
				//{
				//  ID = x.ID,
				//  Avatar = x.Avatar.ToArray(),
				//  FirstName = x.FirstName,
				//  LastName = x.LastName,
				//  UserName = x.UserName,
				//  Identifier = x.Identifier
				//}).ToList();
			}

			public WikiUser GetUserByOpenIDIdentifier(string openIDIdentifier)
			{
				throw new NotImplementedException();

				//return db.__Users.Select(x => new WikiUser()
				//{
				//  ID = x.ID,
				//  Avatar = (x.Avatar != null) ? x.Avatar.ToArray() : null,
				//  FirstName = x.FirstName,
				//  LastName = x.LastName,
				//  UserName = x.UserName,
				//  Identifier = x.Identifier
				//}).FirstOrDefault(u => u.Identifier == openIDIdentifier);
			}

			public void AddUser(WikiUser u)
			{
				//__User _u = db.__Users.FirstOrDefault(x => x.Identifier == u.Identifier);

				//if (_u == null)
				//{
				//  _u = new __User()
				//  {
				//    Avatar = u.Avatar,
				//    FirstName = u.FirstName,
				//    LastName = u.LastName,
				//    UserName = u.UserName,
				//    Identifier = u.Identifier
				//  };

				//  db.__Users.InsertOnSubmit(_u);
				//  db.SubmitChanges();
				//}
			}

			public void DeleteUser(WikiUser u)
			{
				//__User deleteUser = db.__Users.FirstOrDefault(x => x.ID == u.ID);

				//if (deleteUser != null)
				//{
				//  db.__Users.DeleteOnSubmit(deleteUser);
				//  db.SubmitChanges();
				//}
			}

			public void UpdateUser(WikiUser u)
			{
				//__User updateUser = db.__Users.FirstOrDefault(x => x.ID == u.ID);

				//if (updateUser != null)
				//{
				//  updateUser.FirstName = u.FirstName;
				//  updateUser.LastName = u.LastName;
				//  updateUser.Avatar = u.Avatar;
				//  updateUser.UserName = u.UserName;

				//  db.SubmitChanges();
				//}
			}

			public WikiUser GetUser(string identifier)
			{
				WikiUser u = null;

				var _u = users.First(Identifier: identifier);

				if (_u != null)
				{
					u = new WikiUser()
					{
						ID = _u.ID,
						Identifier = _u.Identifier,
						FirstName = _u.FirstName,
						LastName = _u.LastName,
						UserName = _u.UserName
					};
				}

				return u;
			}
			#endregion
		}
	}
	#endregion

	#region L2S
#if L2S
	namespace L2S
	{
		public class L2SDataConnector : IData, IBoundToAction
		{
			private WikiDataClassesDataContext db;

			public void Initialize(Controller c)
			{
				db = new WikiDataClassesDataContext();
			}

	#region PAGE METHODS
			public List<WikiPage> GetAllPages()
			{
				return db.__Pages
					.OrderBy(x => x.Title)
					.Select(x => new WikiPage()
				{
					ID = x.ID,
					CreatedOn = x.CreatedOn,
					ModifiedOn = x.ModifiedOn,
					Alias = x.Alias,
					AuthorID = x.AuthorID,
					Title = x.Title,
					Body = x.Body,
					Published = x.Published
				}).ToList();
			}

			public WikiPage GetPage(int id)
			{
				WikiPage result = null;

				__Page _p = db.__Pages.FirstOrDefault(x => x.ID == id);

				if (_p != null)
				{
					result = new WikiPage()
					{
						ID = _p.ID,
						CreatedOn = _p.CreatedOn,
						ModifiedOn = _p.ModifiedOn,
						Alias = _p.Alias,
						AuthorID = _p.AuthorID,
						Title = _p.Title,
						Body = _p.Body,
						Published = _p.Published
					};
				}

				return result;
			}

			public List<WikiPage> GetPages(int count)
			{
				throw new NotImplementedException();
			}

			public void AddPage(WikiPage p)
			{
				if (p == null)
					throw new ArgumentNullException("p");

				__Page _p = new __Page()
				{
					CreatedOn = p.CreatedOn,
					ModifiedOn = p.ModifiedOn,
					Alias = p.Alias,
					AuthorID = p.AuthorID,
					Title = p.Title,
					Body = p.Body,
					Published = p.Published
				};

				db.__Pages.InsertOnSubmit(_p);
				db.SubmitChanges();

				p.ID = _p.ID;
			}

			public void DeletePage(int id)
			{
				WikiPage p = GetPage(id);

				if (p != null)
					DeletePage(p);
			}

			public void DeletePage(WikiPage p)
			{
				if (p == null)
					throw new ArgumentNullException("p");

				__Page _p = db.__Pages.FirstOrDefault(x => x.ID == p.ID);

				if (_p != null)
				{
					db.__Pages.DeleteOnSubmit(_p);
					db.SubmitChanges();
				}
			}

			public void UpdatePage(WikiPage p)
			{
				if (p == null)
					throw new ArgumentNullException("p");

				__Page _p = db.__Pages.FirstOrDefault(x => x.ID == p.ID);

				if (_p != null)
				{
					_p.CreatedOn = p.CreatedOn;
					_p.ModifiedOn = p.ModifiedOn;
					_p.Alias = p.Alias;
					_p.AuthorID = p.AuthorID;
					_p.Title = p.Title;
					_p.Body = p.Body;
					_p.Published = p.Published;

					db.SubmitChanges();
				}
			}
	#endregion

	#region COMMENT METHODS
			public List<WikiComment> GetAllComments()
			{
				throw new NotImplementedException();
			}

			public List<WikiComment> GetComments(int pageID)
			{
				throw new NotImplementedException();
			}

			public void AddComment(WikiComment c)
			{
				throw new NotImplementedException();
			}

			public void DeleteComment(WikiComment c)
			{
				throw new NotImplementedException();
			}

			public void UpdateComment(WikiComment c)
			{
				throw new NotImplementedException();
			}
	#endregion

	#region TAG METHODS
			public List<WikiTag> GetAllTags()
			{
				return db.__Tags.Select(x => new WikiTag()
				{
					ID = x.ID,
					CreatedOn = x.CreatedOn,
					Name = x.Name
				}).ToList();
			}

			public void AddTag(WikiTag t)
			{
				__Tag _t = db.__Tags.FirstOrDefault(x => x.Name == t.Name);

				if (_t == null)
				{
					_t = new __Tag()
					{
						CreatedOn = DateTime.Now,
						Name = t.Name
					};
				}
			}

			public void DeleteTag(WikiTag t)
			{
				throw new NotImplementedException();
			}

			public void UpdateTag(WikiTag t)
			{
				throw new NotImplementedException();
			}
	#endregion

	#region USER METHODS
			public List<WikiUser> GetAllUsers()
			{
				return db.__Users.Select(x => new WikiUser()
				{
					ID = x.ID,
					Avatar = x.Avatar.ToArray(),
					FirstName = x.FirstName,
					LastName = x.LastName,
					UserName = x.UserName,
					Identifier = x.Identifier
				}).ToList();
			}

			public WikiUser GetUserByOpenIDIdentifier(string openIDIdentifier)
			{
				return db.__Users.Select(x => new WikiUser()
					{
						ID = x.ID,
						Avatar = (x.Avatar != null) ? x.Avatar.ToArray() : null,
						FirstName = x.FirstName,
						LastName = x.LastName,
						UserName = x.UserName,
						Identifier = x.Identifier
					})
					.FirstOrDefault(u => u.Identifier == openIDIdentifier);
			}

			public void AddUser(WikiUser u)
			{
				__User _u = db.__Users.FirstOrDefault(x => x.Identifier == u.Identifier);

				if (_u == null)
				{
					_u = new __User()
					{
						Avatar = u.Avatar,
						FirstName = u.FirstName,
						LastName = u.LastName,
						UserName = u.UserName,
						Identifier = u.Identifier
					};

					db.__Users.InsertOnSubmit(_u);
					db.SubmitChanges();
				}
			}

			public void DeleteUser(WikiUser u)
			{
				__User deleteUser = db.__Users.FirstOrDefault(x => x.ID == u.ID);

				if (deleteUser != null)
				{
					db.__Users.DeleteOnSubmit(deleteUser);
					db.SubmitChanges();
				}
			}

			public void UpdateUser(WikiUser u)
			{
				__User updateUser = db.__Users.FirstOrDefault(x => x.ID == u.ID);

				if (updateUser != null)
				{
					updateUser.FirstName = u.FirstName;
					updateUser.LastName = u.LastName;
					updateUser.Avatar = u.Avatar;
					updateUser.UserName = u.UserName;

					db.SubmitChanges();
				}
			}
	#endregion
		}
	}
#endif
	#endregion
}