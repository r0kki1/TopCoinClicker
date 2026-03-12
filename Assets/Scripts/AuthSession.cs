using Assets.Scripts.Models;
using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using UnityEngine;

public static class AuthSession
{
    private const string AccessTokenKey = "auth.accessToken";
    private const string AccessTokenExpiresAtKey = "auth.accessTokenExpiresAt";
    private const string RefreshTokenKey = "auth.refreshToken";
    private const string RefreshTokenExpiresAtKey = "auth.refreshTokenExpiresAt";

    public static string AccessToken => PlayerPrefs.GetString(AccessTokenKey, string.Empty);
    public static string AccessTokenExpiresAt => PlayerPrefs.GetString(AccessTokenExpiresAtKey, string.Empty);
    public static string RefreshToken => PlayerPrefs.GetString(RefreshTokenKey, string.Empty);
    public static string RefreshTokenExpiresAt => PlayerPrefs.GetString(RefreshTokenExpiresAtKey, string.Empty);

    public static bool HasToken => !string.IsNullOrWhiteSpace(AccessToken);
    public static bool HasRefreshToken => !string.IsNullOrWhiteSpace(RefreshToken);

    public static void Save(AuthResponse authResponse)
    {
        PlayerPrefs.SetString(AccessTokenKey, authResponse.accessToken ?? string.Empty);
        PlayerPrefs.SetString(AccessTokenExpiresAtKey, authResponse.accessTokenExpiresAt ?? string.Empty);
        PlayerPrefs.SetString(RefreshTokenKey, authResponse.refreshToken ?? string.Empty);
        PlayerPrefs.SetString(RefreshTokenExpiresAtKey, authResponse.refreshTokenExpiresAt ?? string.Empty);
        PlayerPrefs.Save();
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(AccessTokenKey);
        PlayerPrefs.DeleteKey(AccessTokenExpiresAtKey);
        PlayerPrefs.DeleteKey(RefreshTokenKey);
        PlayerPrefs.DeleteKey(RefreshTokenExpiresAtKey);
        PlayerPrefs.Save();
    }

    public static void ApplyBearerToken(HttpClient httpClient)
    {
        if (HasToken)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            return;
        }

        httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public static void ApplyBearerToken(HttpRequestMessage request)
    {
        if (request == null)
        {
            return;
        }

        if (HasToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            return;
        }

        request.Headers.Authorization = null;
    }

    public static bool IsAccessTokenExpired()
    {
        return IsExpired(AccessTokenExpiresAt);
    }

    public static bool IsRefreshTokenExpired()
    {
        return IsExpired(RefreshTokenExpiresAt);
    }

    private static bool IsExpired(string expiresAt)
    {
        if (string.IsNullOrWhiteSpace(expiresAt))
        {
            return false;
        }

        if (!DateTime.TryParse(expiresAt, null, DateTimeStyles.RoundtripKind, out DateTime parsedUtc)
            && !DateTime.TryParse(expiresAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsedUtc))
        {
            return false;
        }

        return parsedUtc <= DateTime.UtcNow.AddSeconds(30);
    }
}
