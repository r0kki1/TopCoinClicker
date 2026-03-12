using Assets.Scripts.Models;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class AuthApiClient
{
    private static readonly HttpClient HttpClient = new();
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);
    private static string _authBaseUrl = "http://localhost:5279/api/Auth";

    public static void Configure(string authBaseUrl)
    {
        if (!string.IsNullOrWhiteSpace(authBaseUrl))
        {
            _authBaseUrl = authBaseUrl;
        }
    }

    public static async Task<bool> TryRefreshAccessTokenAsync()
    {
        if (!AuthSession.HasRefreshToken)
        {
            return false;
        }

        await RefreshLock.WaitAsync();
        try
        {
            if (!AuthSession.HasRefreshToken)
            {
                return false;
            }

            if (!AuthSession.IsAccessTokenExpired())
            {
                return true;
            }

            var requestBody = new RefreshTokenRequest
            {
                refreshToken = AuthSession.RefreshToken
            };

            string requestJson = JsonUtility.ToJson(requestBody);
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, BuildRefreshUrl());
            request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await HttpClient.SendAsync(request);
            string responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                AuthSession.Clear();
                return false;
            }

            AuthResponse authResponse = JsonUtility.FromJson<AuthResponse>(responseJson);
            if (authResponse == null || string.IsNullOrWhiteSpace(authResponse.accessToken))
            {
                AuthSession.Clear();
                return false;
            }

            AuthSession.Save(authResponse);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Refresh token request failed: " + exception.Message);
            return false;
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    private static string BuildRefreshUrl()
    {
        return _authBaseUrl.TrimEnd('/') + "/refresh";
    }
}
