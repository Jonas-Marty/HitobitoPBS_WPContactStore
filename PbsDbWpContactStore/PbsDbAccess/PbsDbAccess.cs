﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PbsDbAccess
{
	public class PbsDbAccess
	{
		private const string JsonMimeType = "application/json";

		private const string BaseUrl = "https://db.scout.ch/";
		private const string ReadTokenUrl = "/users/sign_in";
		private const string GenerateTokenUrl = "/users/sign_in";
		private const string DeleteTokenUrl = "/users/sign_in";

		private const string EmailFormDataString = "person[email]";
		private const string PasswortFormDataString = "person[password]";
		private const string EmailHeaderString = "X-User-Email";
		private const string AuthentificationTokenHeaderString = "X-User-Token";


		private readonly string _email;
		private readonly LoggedinUserInformation _loggedinUserInformation;

		private readonly HttpClient _client;

		public PbsDbAccess(string email, LoggedinUserInformation loggedinUserInformation)
		{
			_email = email;
			_loggedinUserInformation = loggedinUserInformation;
			_client = CreateHttpClient();
		}

		private static HttpClient CreateHttpClient()
		{
			return new HttpClient { BaseAddress = new Uri(BaseUrl) };
		}

		private HttpRequestMessage CreateRequestMessage(Uri uri)
		{
			return CreateRequestMessage(uri, HttpMethod.Get);
		}

		private HttpRequestMessage CreateRequestMessage(Uri uri, HttpMethod method)
		{
			HttpRequestMessage message = new HttpRequestMessage(method, uri);
			message.Headers.Add("X-User-Email", _email);
			message.Headers.Add("X-User-Token", _loggedinUserInformation.Token);
			message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(JsonMimeType));
			return message;
		}

		/// <summary>
		/// Reads the token for the specified user. If the token is not existing, the token will
		/// be automatically generated by the server.
		/// </summary>
		/// <param name="email"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		public static async Task<LoggedinUserInformation> RecieveUserInformationAsync(string email, string password)
		{
			var client = CreateHttpClient();

			var formData = new Dictionary<string, string> { { EmailFormDataString, email }, { PasswortFormDataString, password } };
			FormUrlEncodedContent requestContent = new FormUrlEncodedContent(formData);
			var message = new HttpRequestMessage(HttpMethod.Post, ReadTokenUrl);
			message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(JsonMimeType));
			message.Content = requestContent;

			HttpResponseMessage response = await client.SendAsync(message);

			if (!response.IsSuccessStatusCode)
			{
				HandleStatusCodeErrors(response, message);
			}

			string responseContent = await response.Content.ReadAsStringAsync();

			return JsonParser.ParseLoggedinUserInformation(responseContent);
		}

		private static void HandleStatusCodeErrors(HttpResponseMessage response, HttpRequestMessage request)
		{
			//if (response.StatusCode == HttpStatusCode.Unauthorized)
			//{
			//	throw new InvalidLoginInformationException();
			//}
			throw new Exception(FormatHttpErrorMessage(response, request));
		}

		private static string FormatHttpErrorMessage(HttpResponseMessage response, HttpRequestMessage request)
		{
			return String.Format("{0} - {1}\nRequest Content:\n{2}\n\nResponse:\n{3}",
				response.StatusCode,
				response.ReasonPhrase,
				"",
				response.Content.ReadAsStringAsync().Result);
		}

		/// <summary>
		/// Creates an Uri with the given path and parameters. <para>urlWithoutParameters</para> must not
		/// have any parameters in it!
		/// </summary>
		/// <param name="urlWithoutParameters">An url (relativ or absolut) wihtout any parameters.</param>
		/// <param name="parameterValueCollection">A dictionay containing the parameter name as the key and its value as the values.</param>
		/// <returns>The uri with the given path and parameters.</returns>
		private static Uri CreateUriWithParameters(string urlWithoutParameters, Dictionary<string, string> parameterValueCollection)
		{
			var parameterValueProjection = parameterValueCollection
				.Select(keyValuePair => String.Format("{0}={1}", keyValuePair.Key, keyValuePair.Value));
			string queryString = "?" + String.Join("&", parameterValueCollection);
			return new Uri(urlWithoutParameters + queryString);
		}
	}

}
