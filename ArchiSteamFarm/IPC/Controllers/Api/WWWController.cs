﻿//     _                _      _  ____   _                           _____
//    / \    _ __  ___ | |__  (_)/ ___| | |_  ___   __ _  _ __ ___  |  ___|__ _  _ __  _ __ ___
//   / _ \  | '__|/ __|| '_ \ | |\___ \ | __|/ _ \ / _` || '_ ` _ \ | |_  / _` || '__|| '_ ` _ \
//  / ___ \ | |  | (__ | | | || | ___) || |_|  __/| (_| || | | | | ||  _|| (_| || |   | | | | | |
// /_/   \_\|_|   \___||_| |_||_||____/  \__|\___| \__,_||_| |_| |_||_|   \__,_||_|   |_| |_| |_|
// 
// Copyright 2015-2018 Łukasz "JustArchi" Domeradzki
// Contact: JustArchi@JustArchi.net
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArchiSteamFarm.IPC.Requests;
using ArchiSteamFarm.IPC.Responses;
using ArchiSteamFarm.Localization;
using Microsoft.AspNetCore.Mvc;

namespace ArchiSteamFarm.IPC.Controllers.Api {
	[ApiController]
	[Route("Api/WWW")]
	public sealed class WWWController : ControllerBase {
		[HttpGet("Directory/{directory:required}")]
		public ActionResult<GenericResponse<HashSet<string>>> DirectoryGet(string directory) {
			if (string.IsNullOrEmpty(directory)) {
				ASF.ArchiLogger.LogNullError(nameof(directory));
				return BadRequest(new GenericResponse<HashSet<string>>(false, string.Format(Strings.ErrorIsEmpty, nameof(directory))));
			}

			string directoryPath = Path.Combine(SharedInfo.HomeDirectory, SharedInfo.WebsiteDirectory, directory);
			if (!Directory.Exists(directoryPath)) {
				return BadRequest(new GenericResponse<HashSet<string>>(false, string.Format(Strings.ErrorIsInvalid, directory)));
			}

			string[] files;

			try {
				files = Directory.GetFiles(directoryPath);
			} catch (Exception e) {
				return BadRequest(new GenericResponse<HashSet<string>>(false, string.Format(Strings.ErrorParsingObject, nameof(files)) + Environment.NewLine + e));
			}

			HashSet<string> result = files.Select(Path.GetFileName).ToHashSet();
			return Ok(new GenericResponse<HashSet<string>>(result));
		}

		[HttpPost("Send")]
		public async Task<ActionResult<GenericResponse<string>>> SendPost([FromBody] WWWSendRequest request) {
			if (request == null) {
				ASF.ArchiLogger.LogNullError(nameof(request));
				return BadRequest(new GenericResponse<string>(false, string.Format(Strings.ErrorIsEmpty, nameof(request))));
			}

			if (string.IsNullOrEmpty(request.URL) || !Uri.TryCreate(request.URL, UriKind.Absolute, out Uri uri) || !uri.Scheme.Equals(Uri.UriSchemeHttps)) {
				return BadRequest(new GenericResponse<string>(false, string.Format(Strings.ErrorIsInvalid, nameof(request.URL))));
			}

			WebBrowser.StringResponse urlResponse = await Program.WebBrowser.UrlGetToString(request.URL).ConfigureAwait(false);
			if (urlResponse?.Content == null) {
				return BadRequest(new GenericResponse<string>(false, string.Format(Strings.ErrorRequestFailedTooManyTimes, WebBrowser.MaxTries)));
			}

			return Ok(new GenericResponse<string>(urlResponse.Content));
		}
	}
}
