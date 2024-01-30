﻿using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using RFIDify.Services;
using RFIDify.Spotify.Apis.Extensions;
using System.Text.Json.Serialization;

namespace RFIDify.Spotify.Apis;

public interface ISpotifyAccountsApi
{
    Task<SpotifyTokens> ExchangeAuthorizationCodeForTokens(string authorizationCode, SpotifyAuthorizationState authorizationState, CancellationToken cancellationToken);
    Uri GenerateAuthorizationUri(SpotifyCredentials credentials, SpotifyAuthorizationState authorizationState);
}

public class SpotifyAccountsApiOptions
{
    public required string Scopes { get; init; }
}

public class SpotifyAccountsApi(HttpClient httpClient, IOptionsMonitor<SpotifyAccountsApiOptions> options, IDateTimeProvider dateTimeProvider) : ISpotifyAccountsApi
{
    public Uri GenerateAuthorizationUri(SpotifyCredentials credentials, SpotifyAuthorizationState authorizationState)
    {
        var uri = httpClient.BaseAddress!.AbsoluteUri.Replace("/api", "/authorize");

        var query = new Dictionary<string, string?>
        {
            [Parameters.ClientId] = credentials.ClientId,
            [Parameters.ResponseType] = ResponseTypes.Code,
            [Parameters.RedirectUri] = authorizationState.RedirectUri,
            [Parameters.State] = authorizationState.State,
            [Parameters.Scope] = options.CurrentValue.Scopes,
            [Parameters.ShowDialog] = "true"
        };

        return new Uri(QueryHelpers.AddQueryString(uri, query));
    }

    public async Task<SpotifyTokens> ExchangeAuthorizationCodeForTokens(string authorizationCode, SpotifyAuthorizationState authorizationState, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                [Parameters.RedirectUri] = authorizationState.RedirectUri,
                [Parameters.Code] = authorizationCode,
                [Parameters.GrantType] = GrantTypes.AuthorizationCode
            })
        };

        var response = await httpClient.SendAndDeserializeJson<ExchangeAuthorizationCodeForTokensResponse>(request, cancellationToken);

        var accessToken = new SpotifyAccessToken
        {
            Token = response.AccessToken,
            ExpiresAtUtc = dateTimeProvider.UtcNow.AddSeconds(response.ExpiresInSeconds)
        };

        var refreshToken = new SpotifyRefreshToken
        {
            Token = response.RefreshToken
        };

        return new SpotifyTokens(accessToken, refreshToken);
    }

    private static class Parameters
    {
        public const string ClientId = "client_id";
        public const string ResponseType = "response_type";
        public const string RedirectUri = "redirect_uri";
        public const string State = "state";
        public const string Scope = "scope";
        public const string ShowDialog = "show_dialog";
        public const string Code = "code";
        public const string GrantType = "grant_type";
    }

    private static class ResponseTypes
    {
        public const string Code = "code";
    }

    private static class GrantTypes
    {
        public const string AuthorizationCode = "authorization_code";
    }

    private record ExchangeAuthorizationCodeForTokensResponse
    {
        [JsonPropertyName("access_token")]
        public required string AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public required int ExpiresInSeconds { get; set; }

        [JsonPropertyName("refresh_token")]
        public required string RefreshToken { get; set; }
    }
}