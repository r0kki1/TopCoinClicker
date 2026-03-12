using Assets.Scripts.Models;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginService : MonoBehaviour
{
    private static readonly HttpClient HttpClient = new HttpClient();

    [Serializable]
    private class ApiErrorResponse
    {
        public string message;
    }

    public TMP_InputField loginInput;
    public TMP_InputField passwordInput;
    public TMP_Text statusText;

    public string loginUrl = "http://localhost:5279/api/Auth";
    public string nextSceneName;

    public string loadingMessage = "Входим...";
    public string successMessage = "Вход выполнен";
    public string networkErrorMessage = "Ошибка сети";
    public string serverErrorMessage = "Ошибка входа";

    private bool _isLoading;

    private void Start()
    {
        loginUrl = ServerConfig.GetAuthUrl(loginUrl);
        AuthApiClient.Configure(loginUrl);
        AuthSession.ApplyBearerToken(HttpClient);
        HideStatus();
    }

    public async void  OnLoginClick()
    {
        if (_isLoading)
        {
            return;
        }

        var login = loginInput.text.Trim();
        var password = passwordInput.text.Trim();

        if (string.IsNullOrEmpty(login) && string.IsNullOrEmpty(password))
        {
            ShowStatus("Введи логин и пароль");
            return;
        }

        if (string.IsNullOrEmpty(login))
        {
            ShowStatus("Введи логин");
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowStatus("Введи пароль");
            return;
        }

        if (string.IsNullOrWhiteSpace(loginUrl))
        {
            ShowStatus("Укажи Login Url в инспекторе");
            return;
        }

        await SendLoginRequestAsync(login, password);
    }

    private async Task SendLoginRequestAsync(string login, string password)
    {
        _isLoading = true;
        SetInputsInteractable(false);
        ShowStatus(loadingMessage);

        try
        {
            using var request = BuildRequest(login, password);
            using var response = await HttpClient.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();
            

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = GetReadableErrorMessage(response, responseText);
                ShowStatus(string.IsNullOrWhiteSpace(errorMessage) ? serverErrorMessage : errorMessage);
                return;
            }

            AuthResponse authResponse = JsonUtility.FromJson<AuthResponse>(responseText);
            if (authResponse == null || string.IsNullOrWhiteSpace(authResponse.accessToken))
            {
                ShowStatus("Сервер не вернул accessToken");
                return;
            }

            AuthSession.Save(authResponse);
            AuthSession.ApplyBearerToken(HttpClient);
            ShowStatus(successMessage);
            LoadNextScene();
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            ShowStatus(networkErrorMessage + ": " + exception.Message);
        }
        finally
        {
            _isLoading = false;
            SetInputsInteractable(true);
        }
    }

    private HttpRequestMessage BuildRequest(string login, string password)
    {
        var requestBody = new LoginRequest
        {
            username = login,
            password = password
        };

        var json = JsonUtility.ToJson(requestBody);

        var request = new HttpRequestMessage(HttpMethod.Post, loginUrl);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return request;
    }

    private string GetReadableErrorMessage(HttpResponseMessage response, string responseText)
    {
        var messageFromJson = TryGetMessageFromJson(responseText);
        if (!string.IsNullOrWhiteSpace(messageFromJson))
        {
            return NormalizeMessage(messageFromJson);
        }

        var cleanedText = CleanupRawError(responseText);
        return !string.IsNullOrWhiteSpace(cleanedText) ? NormalizeMessage(cleanedText) : NormalizeMessage(response.ReasonPhrase ?? serverErrorMessage);
    }

    private static string TryGetMessageFromJson(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return string.Empty;
        }

        try
        {
            var error = JsonUtility.FromJson<ApiErrorResponse>(responseText);
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
    private static string CleanupRawError(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return string.Empty;
        }

        string[] lines = responseText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
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

    private string NormalizeMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return serverErrorMessage;
        }

        string lower = message.ToLowerInvariant();

        if (lower.Contains("unauthorized") || lower.Contains("401") || lower.Contains("неверный логин") || lower.Contains("неверный пароль"))
        {
            return "Неверный логин или пароль";
        }

        if (lower.Contains("top academy"))
        {
            return "Не удалось войти через Top Academy";
        }

        if (lower.Contains("internal server error"))
        {
            return "Внутренняя ошибка сервера";
        }

        return message;
    }

    private void LoadNextScene()
    {
        string sceneToLoad = string.IsNullOrWhiteSpace(nextSceneName) ? "MainScene" : nextSceneName;
        SceneManager.LoadScene(sceneToLoad);
    }

    private void SetInputsInteractable(bool value)
    {
        loginInput.interactable = value;
        passwordInput.interactable = value;
    }

    private void HideStatus()
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = string.Empty;
        statusText.gameObject.SetActive(false);
    }

    private void ShowStatus(string message)
    {
        statusText.gameObject.SetActive(true);
        statusText.text = message;
    }
}
