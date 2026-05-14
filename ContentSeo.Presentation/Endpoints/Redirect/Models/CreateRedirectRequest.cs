// <copyright file="CreateRedirectRequest.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Presentation.Endpoints.Redirect.Models;

public sealed record CreateRedirectRequest(string OldUrl, string NewUrl, int StatusCode);
