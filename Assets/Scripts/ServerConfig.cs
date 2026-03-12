using System;
using System.IO;
using UnityEngine;

public static class ServerConfig
{
    private const string ConfigFileName = "server-config.txt";
    private const string DefaultBaseUrl = "http://localhost:5279";
    
    public static string GetAuthUrl(string fallbackUrl)
    {
        var baseUrl = GetBaseUrl(fallbackUrl);
        return baseUrl.TrimEnd('/') + "/api/Auth";
    }
    
    public static string GetGameUrl(string fallbackUrl)
    {
        var baseUrl = GetBaseUrl(fallbackUrl);
        return baseUrl.TrimEnd('/') + "/api/Game";
    }

    private static string GetBaseUrl(string fallbackUrl)
    {
        var filePath = GetConfigPath();
        var defaultValue = ExtractBaseUrl(fallbackUrl);

        if (!File.Exists(filePath))
        {
            TryWriteDefaultFile(filePath, defaultValue);
            return defaultValue;
        }

        var value = File.ReadAllText(filePath).Trim();
        try
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.TrimEnd('/');
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning(exception.Message);
        }

        return defaultValue;
    }

    private static void TryWriteDefaultFile(string filePath, string defaultValue)
    {
        try
        {
            File.WriteAllText(filePath, defaultValue);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(exception.Message);
        }
    }

    private static string GetConfigPath()
    {
        var rootDirectory = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
        return Path.Combine(rootDirectory, ConfigFileName);
    }

    private static string ExtractBaseUrl(string fallbackUrl)
    {
        if (string.IsNullOrWhiteSpace(fallbackUrl))
        {
            return DefaultBaseUrl;
        }

        int apiIndex = fallbackUrl.IndexOf("/api/", StringComparison.OrdinalIgnoreCase);
        if (apiIndex >= 0)
        {
            return fallbackUrl.Substring(0, apiIndex).TrimEnd('/');
        }

        return fallbackUrl.TrimEnd('/');
    }
}
