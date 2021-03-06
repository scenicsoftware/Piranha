﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Web;

namespace Piranha.Web
{
	/// <summary>
	/// Simple authorization manager that handles API-keys.
	/// </summary>
	public class APIKeyAuthorization : ServiceAuthorizationManager
	{
		#region Members
		private const string APIKEY = "apikey" ;
		#endregion

		/// <summary>
		/// Checks that the given API-key is valid.
		/// </summary>
		/// <param name="context">The current service context</param>
		/// <returns>Whether the api key is valid or not.</returns>
		protected override bool CheckAccessCore(OperationContext context) {
			return APIKeys.IsValidKey(GetAPIKey(context)) ;
		}

		#region Private members
		/// <summary>
		/// Gets the API-key from the current request.
		/// </summary>
		/// <param name="context">The current context</param>
		/// <returns>The API-key</returns>
		private string GetAPIKey(OperationContext context) {
			// Get the request message
			var request = context.RequestContext.RequestMessage ;

			// Get the HTTP Request
			var props = (HttpRequestMessageProperty)request.Properties[HttpRequestMessageProperty.Name] ;

			// Get the api key
			return HttpUtility.ParseQueryString(props.QueryString)[APIKEY] ;
			
			// Return as Guid
			/*if (!String.IsNullOrEmpty(key))
				try {
					return new Guid(key) ;
				} catch {}
			return Guid.Empty ;*/
		}
		#endregion
	}
}