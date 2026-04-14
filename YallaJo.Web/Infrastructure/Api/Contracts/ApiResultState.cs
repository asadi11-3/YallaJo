namespace YallaJo.Web.Infrastructure.Api.Contracts
{
    public enum ApiResultState
    {
        /// <summary>API call succeeded.</summary>
        Success,

        /// <summary>API call was unauthorized (401).</summary>
        Unauthorized,

        /// <summary>API call was forbidden (403).</summary>
        Forbidden,

        /// <summary>Resource was not found (404).</summary>
        NotFound,

        /// <summary>Conflict occurred (409).</summary>
        Conflict,

        /// <summary>Validation error (400 or 422).</summary>
        ValidationError,

        /// <summary>Too many requests (429).</summary>
        TooManyRequests,

        /// <summary>General error (other non-success status).</summary>
        Error,
    }
}
