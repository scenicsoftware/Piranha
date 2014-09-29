﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Security;

using Piranha.Data;

namespace Piranha.Models
{
	/// <summary>
	/// Active record for the sys user.
	/// 
	/// Changes made to records of this type are logged.
	/// </summary>
	[PrimaryKey(Column="sysuser_id")]
	[Join(TableName="SysGroup", ForeignKey="sysuser_group_id", PrimaryKey="sysgroup_id")]
	[Serializable]
	public class SysUser : GuidRecord<SysUser>
	{
		#region Fields
		/// <summary>
		/// Gets/sets the id.
		/// </summary>
		[Column(Name="sysuser_id")]
		public override Guid Id { get ; set ; }

		/// <summary>
		/// Gets/sets the API-key.
		/// </summary>
		[Column(Name="sysuser_apikey")]
		public Guid APIKey { get ; set ; }

		/// <summary>
		/// Gets/sets the login name.
		/// </summary>
		[Column(Name="sysuser_login")]
		[Display(ResourceType=typeof(Piranha.Resources.Settings), Name="Login")]
		[Required(ErrorMessageResourceType=typeof(Piranha.Resources.Settings), ErrorMessageResourceName="LoginRequired")]
		[StringLength(64, ErrorMessageResourceType=typeof(Piranha.Resources.Settings), ErrorMessageResourceName="LoginLength")]
		public string Login { get ; set ; }

		/// <summary>
		/// Gets/sets the firstname.
		/// </summary>
		[Column(Name="sysuser_firstname")]
		[Display(ResourceType=typeof(Piranha.Resources.Settings), Name="Firstname")]
		[Required(ErrorMessageResourceType=typeof(Piranha.Resources.Settings), ErrorMessageResourceName="FirstnameRequired")]
		[StringLength(128, ErrorMessageResourceType=typeof(Piranha.Resources.Settings), ErrorMessageResourceName="FirstnameLength")]
		public string Firstname { get ; set ; }

		/// <summary>
		/// Gets/sets the surname.
		/// </summary>
		[Column(Name="sysuser_surname")]
		[Display(ResourceType=typeof(Piranha.Resources.Settings), Name="Surname")]
		[Required(ErrorMessageResourceType=typeof(Piranha.Resources.Settings), ErrorMessageResourceName="SurnameRequired")]
		[StringLength(128, ErrorMessageResourceType=typeof(Piranha.Resources.Settings), ErrorMessageResourceName="SurnameLength")]
		public string Surname { get ; set ; }

		/// <summary>
		/// Gets/sets the email address.
		/// </summary>
		[Column(Name="sysuser_email")]
		[Display(ResourceType=typeof(Piranha.Resources.Settings), Name="Email")]
		[Required(ErrorMessageResourceType=typeof(Piranha.Resources.Settings), ErrorMessageResourceName="EmailRequired")]
		[StringLength(128, ErrorMessageResourceType=typeof(Piranha.Resources.Settings), ErrorMessageResourceName="EmailLength")]
		public string Email { get ; set ; }

		/// <summary>
		/// Gets/sets the users current group.
		/// </summary>
		[Column(Name="sysuser_group_id")]
		[Display(ResourceType=typeof(Piranha.Resources.Global), Name="Group")]
		public Guid GroupId { get ; set ; }

		/// <summary>
		/// Gets/sets the users group name.
		/// </summary>
		[Column(Name="sysgroup_name", ReadOnly=true, Table="sysgroup")]
		public string GroupName { get ; set ; }

		/// <summary>
		/// Gets/sets the last time the user logged in to the system.
		/// </summary>
		[Column(Name="sysuser_last_login")]
		public DateTime LastLogin { get ; set ; }

		/// <summary>
		/// Gets/sets the previous login for the user.
		/// </summary>
		[Column(Name="sysuser_prev_login")]
		public DateTime PreviousLogin { get ; set ; }

		/// <summary>
		/// Gets/sets whether the user account is locked or not.
		/// </summary>
		[Column(Name="sysuser_locked")]
		[Display(ResourceType=typeof(Piranha.Resources.Settings), Name="Locked")]
		public bool IsLocked { get ; set ; }

		/// <summary>
		/// Gets/sets the optional end date of user lock.
		/// </summary>
		[Column(Name="sysuser_locked_until")]
		[Display(ResourceType=typeof(Piranha.Resources.Settings), Name="LockedUntil")]
		[DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
		public DateTime LockedUntil { get ; set ; }

		/// <summary>
		/// Gets/sets the created date.
		/// </summary>
		[Column(Name="sysuser_created")]
		public DateTime Created { get ; set ; }

		/// <summary>
		/// Gets/sets the updated date.
		/// </summary>
		[Column(Name="sysuser_updated")]
		public DateTime Updated { get ; set ; }
		#endregion

		#region Properties
		/// <summary>
		/// Gets the name for the user.
		/// </summary>
		public string Name {
			get { return (Firstname + " " + Surname).Trim() ; }
		}
		#endregion

		/// <summary>
		/// Default constructor.
		/// </summary>
		public SysUser() : base() {
			ExtensionType = Extend.ExtensionType.User ;
			LogChanges = true ;
		}

		/// <summary>
		/// Authenticates the user and returns if it was successfull.
		/// </summary>
		/// <param name="login">The login</param>
		/// <param name="password">The password</param>
		/// <returns>An authenticated user</returns>
		public static SysUser Authenticate(string login, string password) {
			var siteversion = Convert.ToInt32(SysParam.GetByName("SITE_VERSION").Value) ;

			var users = GetFields(" * ", "sysuser_login = @0 AND sysuser_password = @1" + 
				(siteversion > 20 ? " AND (sysuser_locked = 0 OR (sysuser_locked_until IS NOT NULL AND sysuser_locked_until <= @2))" : ""),
				login, SysUser.Encrypt(password), DateTime.Now) ;
			if (users.Count == 1) {
				// Update last & prev login date.
				if (siteversion > 22) {
					// Don't log automatic login updates
					users[0].LogChanges = false;
					users[0].PreviousLogin = users[0].LastLogin ;
					users[0].LastLogin = DateTime.Now ;
					users[0].IsLocked = false ;
					users[0].LockedUntil = DateTime.MinValue ;
					users[0].Save() ;
				}
				return users[0] ;
			}
			return null ;
		}

		/// <summary>
		/// Authenticates and logs in the user.
		/// </summary>
		/// <param name="login">The login</param>
		/// <param name="password">The password</param>
		/// <param name="persistent">Whether the cookie should be persistent</param>
		/// <returns>If the user was successfully logged in</returns>
		public static bool LoginUser(string login, string password, bool persistent = false) {
			SysUser user = Authenticate(login, password) ;

			if (user != null) {
				FormsAuthentication.SetAuthCookie(user.Id.ToString(), persistent) ;
				return true ;
			}
			return false ;
		}

		/// <summary>
		/// Logs out the current user.
		/// </summary>
		public static void LogoutUser() {
			FormsAuthentication.SignOut() ;
			HttpContext.Current.Session.Clear() ;
			HttpContext.Current.Session.Abandon() ;
		}

		/// <summary>
		/// Saves the current record to the database.
		/// </summary>
		/// <param name="tx">Optional transaction</param>
		/// <returns>Wether the operation was successful</returns>
		public override bool Save(System.Data.IDbTransaction tx = null) {
			// Check for login name duplicates 
			if (SysUser.GetSingle("sysuser_login = @0" + (!IsNew ? " AND sysuser_id != @1" : ""), Login, Id) != null)
 				throw new DuplicateLoginException() ;

			if (IsNew) {
				if (Id == Guid.Empty)
					Id = Guid.NewGuid() ;
				Updated = Created = DateTime.Now ;
			} else {
				Updated = DateTime.Now ;
			}
			return base.Save(tx);
		}
	}
}