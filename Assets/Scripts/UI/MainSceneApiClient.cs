using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class MainSceneApiClient
{
    private static readonly HttpClient HttpClient = new();

    [Serializable]
    private class ApiErrorResponse
    {
        public string message;
    }

    public static async Task<T> GetAsync<T>(string baseUrl, string relativeUrl)
    {
        using HttpResponseMessage response = await SendWithRefreshAsync(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, BuildUrl(baseUrl, relativeUrl));
            AuthSession.ApplyBearerToken(request);
            return request;
        });

        string json = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, json);
        return JsonUtility.FromJson<T>(json);
    }

    public static async Task<T> PostAsync<T>(string baseUrl, string relativeUrl, object body)
    {
        string requestJson = body != null ? JsonUtility.ToJson(body) : null;

        using HttpResponseMessage response = await SendWithRefreshAsync(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl(baseUrl, relativeUrl));
            AuthSession.ApplyBearerToken(request);

            if (!string.IsNullOrWhiteSpace(requestJson))
            {
                request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            }

            return request;
        });

        string responseJson = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, responseJson);
        return JsonUtility.FromJson<T>(responseJson);
    }

    public static void ClearAuthorization()
    {
        AuthSession.ApplyBearerToken(HttpClient);
    }

    private static string BuildUrl(string baseUrl, string relativeUrl)
    {
        return baseUrl.TrimEnd('/') + "/" + relativeUrl.TrimStart('/');
    }

    private static async Task<HttpResponseMessage> SendWithRefreshAsync(Func<HttpRequestMessage> requestFactory)
    {
        if (requestFactory == null)
        {
            throw new ArgumentNullException(nameof(requestFactory));
        }

        if (AuthSession.HasRefreshToken && AuthSession.IsAccessTokenExpired() && !AuthSession.IsRefreshTokenExpired())
        {
            bool refreshedByExpiry = await AuthApiClient.TryRefreshAccessTokenAsync();
            if (!refreshedByExpiry)
            {
                throw new HttpRequestException("Нужен повторный вход в аккаунт");
            }
        }

        HttpResponseMessage response = await SendAsync(requestFactory);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        response.Dispose();

        if (!AuthSession.HasRefreshToken || AuthSession.IsRefreshTokenExpired())
        {
            AuthSession.Clear();
            throw new HttpRequestException("Нужен повторный вход в аккаунт");
        }

        bool refreshedAfter401 = await AuthApiClient.TryRefreshAccessTokenAsync();
        if (!refreshedAfter401)
        {
            throw new HttpRequestException("Нужен повторный вход в аккаунт");
        }

        return await SendAsync(requestFactory);
    }

    private static async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> requestFactory)
    {
        using HttpRequestMessage request = requestFactory();
        return await HttpClient.SendAsync(request);
    }

    private static void EnsureSuccess(HttpResponseMessage response, string responseJson)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw new HttpRequestException(GetErrorMessage(response, responseJson));
    }

    private static string GetErrorMessage(HttpResponseMessage response, string responseJson)
    {
        string messageFromJson = TryGetMessageFromJson(responseJson);
        if (!string.IsNullOrWhiteSpace(messageFromJson))
        {
            return NormalizeMessage(messageFromJson);
        }

        string cleanedText = CleanupRawError(responseJson);
        if (!string.IsNullOrWhiteSpace(cleanedText))
        {
            return NormalizeMessage(cleanedText);
        }

        return NormalizeMessage(response.ReasonPhrase ?? "Ошибка запроса");
    }

    private static string TryGetMessageFromJson(string responseJson)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            return string.Empty;
        }

        try
        {
            ApiErrorResponse error = JsonUtility.FromJson<ApiErrorResponse>(responseJson);
            if (error != null && !string.IsNullOrWhiteSpace(error.message))
            {
                return error.message;
            }
        }
        catch
        {
        }

        return string.Empty;
    }

    // Берем первую полезную строку из сырого ответа сервера,
    // чтобы не показывать в UI stack trace и технические заголовки.
    private static string CleanupRawError(string responseJson)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            return string.Empty;
        }

        string[] lines = responseJson.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            string candidate = line.Trim();
            if (ShouldSkipErrorLine(candidate))
            {
                continue;
            }

            return TrimExceptionPrefix(candidate);
        }

        return string.Empty;
    }

    private static bool ShouldSkipErrorLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return true;
        }

        return line.StartsWith("at ", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("HEADERS", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("Host:", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("Content-", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("Authorization:", StringComparison.OrdinalIgnoreCase);
    }

    private static string TrimExceptionPrefix(string line)
    {
        if (!line.StartsWith("System.", StringComparison.OrdinalIgnoreCase))
        {
            return line;
        }

        int colonIndex = line.IndexOf(':');
        if (colonIndex < 0 || colonIndex == line.Length - 1)
        {
            return line;
        }

        return line[(colonIndex + 1)..].Trim();
    }

    private static string NormalizeMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Ошибка запроса";
        }

        string lower = message.ToLowerInvariant();

        if (lower.Contains("user") && lower.Contains("not found"))
        {
            return "Пользователь не найден";
        }

        if (lower.Contains("entity not found"))
        {
            return "Объект не найден";
        }

        if (lower.Contains("refresh token") && lower.Contains("not found"))
        {
            return "Сессия истекла. Войди заново";
        }

        if (lower.Contains("unauthorized") || lower.Contains("forbidden") || lower.Contains("нужен повторный вход"))
        {
            return "Нужен повторный вход в аккаунт";
        }

        if (lower.Contains("topacademy login failed"))
        {
            return "Не удалось войти через Top Academy";
        }

        if (lower.Contains("internal server error"))
        {
            return "Внутренняя ошибка сервера";
        }

        return message;
    }
}
