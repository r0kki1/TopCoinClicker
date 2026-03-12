using System;
using System.Net.Http;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainSceneController : MonoBehaviour
{
    public TMP_Text profileNameText;
    public TMP_Text coinsPerSecondText;
    public Button settingsButton;

    public TMP_Text balanceText;
    public TMP_Text incomeText;
    public Button coinButton;
    public RectTransform coinButtonRect;

    public Button clickerTabButton;
    public Button transferTabButton;
    public Button topTabButton;
    public Button boostTabButton;

    public GameObject clickerPage;
    public GameObject transferPage;
    public GameObject topPage;
    public GameObject boostPage;

    public TMP_Text clickPowerText;
    public TMP_Text autoIncomeText;
    public Button quickUpgradeButton;

    public TMP_InputField transferLoginInput;
    public TMP_InputField transferAmountInput;
    public TMP_Text transferStatusText;
    public Button transferButton;

    public Button boostItem1Button;
    public Button boostItem2Button;

    public TMP_Text[] topNameTexts;
    public TMP_Text[] topScoreTexts;

    public string gameApiBaseUrl = "http://localhost:5279/api/Game";
    public string loginSceneName = "SampleScene";

    private GameProfileResponse _profile;
    private bool _isBusy;
    private float _coinPunchTimer;

    private readonly Color _activeTabColor = new Color32(255, 176, 36, 255);
    private readonly Color _inactiveTabColor = new Color32(28, 43, 74, 255);
    private readonly Color _activeTabTextColor = new Color32(44, 27, 5, 255);
    private readonly Color _inactiveTabTextColor = new Color32(150, 171, 210, 255);

    private async void Start()
    {
        gameApiBaseUrl = ServerConfig.GetGameUrl(gameApiBaseUrl);
        AuthApiClient.Configure(ServerConfig.GetAuthUrl(gameApiBaseUrl));
        DisableBoostPurchaseButtons();
        OpenClickerPage();
        await LoadProfileAsync();
        await LoadTopAsync();
    }

    private void Update()
    {
        UpdateCoinAnimation();
    }

    private void UpdateCoinAnimation()
    {
        if (!coinButtonRect)
        {
            return;
        }

        if (_coinPunchTimer > 0f)
        {
            _coinPunchTimer -= Time.deltaTime * 6f;

            var scale = Mathf.Lerp(1.1f, 1f, 1f - Mathf.Clamp01(_coinPunchTimer));
            coinButtonRect.localScale = Vector3.one * scale;
            return;
        }

        coinButtonRect.localScale = Vector3.Lerp(coinButtonRect.localScale, Vector3.one, Time.deltaTime * 12f);
    }

    public async void OnCoinClick()
    {
        if (!BeginRequest())
        {
            return;
        }

        try
        {
            _profile = await MainSceneApiClient.PostAsync<GameProfileResponse>(gameApiBaseUrl, "click", null);
            _coinPunchTimer = 1f;
            RefreshProfileUI();
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            ShowMainMessage("Ошибка клика");
        }
        finally
        {
            EndRequest();
        }
    }

    public async void BuyOneUpgrade()
    {
        if (!BeginRequest())
        {
            return;
        }

        var request = new UpgradeTapRequest
        {
            levels = 1
        };

        try
        {
            _profile = await MainSceneApiClient.PostAsync<GameProfileResponse>(
                gameApiBaseUrl,
                "upgrade-tap",
                request);

            ShowMainMessage("Улучшение куплено");
            RefreshProfileUI();
        }
        catch (HttpRequestException exception)
        {
            ShowMainMessage(exception.Message);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            ShowMainMessage("Ошибка улучшения");
        }
        finally
        {
            EndRequest();
        }
    }

    public async void TransferCoins()
    {
        if (transferLoginInput == null || transferAmountInput == null || transferStatusText == null)
        {
            return;
        }

        var username = transferLoginInput.text.Trim();
        var amountText = transferAmountInput.text.Trim();

        if (string.IsNullOrWhiteSpace(username))
        {
            transferStatusText.text = "Введи логин получателя";
            return;
        }

        if (!long.TryParse(amountText, out long amount) || amount <= 0)
        {
            transferStatusText.text = "Введи сумму больше 0";
            return;
        }

        if (!BeginRequest())
        {
            return;
        }

        var request = new TransferRequestDto();
        try
        {
            request.targetUsername = username;
            request.amount = amount;

            _profile = await MainSceneApiClient.PostAsync<GameProfileResponse>(
                gameApiBaseUrl,
                "transfer",
                request);

            transferStatusText.text = "Перевод выполнен";
            transferAmountInput.text = string.Empty;

            RefreshProfileUI();
            await LoadTopAsync();
        }
        catch (HttpRequestException exception)
        {
            transferStatusText.text = exception.Message;
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            transferStatusText.text = "Ошибка перевода";
        }
        finally
        {
            EndRequest();
        }
    }

    private async Task LoadProfileAsync()
    {
        if (!AuthSession.HasToken)
        {
            ShowMainMessage("Нет токена авторизации");
            return;
        }

        try
        {
            _profile = await MainSceneApiClient.GetAsync<GameProfileResponse>(gameApiBaseUrl, "profile");
            RefreshProfileUI();
        }
        catch (HttpRequestException exception) when (exception.Message.Contains("повторный вход"))
        {
            Debug.LogWarning(exception.Message);
            ShowMainMessage(exception.Message);
            Logout();
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            ShowMainMessage("Не удалось загрузить профиль");
        }
    }

    private async Task LoadTopAsync()
    {
        if (topNameTexts == null || topScoreTexts == null)
        {
            return;
        }

        try
        {
            var response = await MainSceneApiClient.GetAsync<TopResponseDto>(gameApiBaseUrl, "top?limit=3");

            for (var i = 0; i < topNameTexts.Length; i++)
            {
                var hasEntry = response.entries != null && i < response.entries.Length;

                if (hasEntry)
                {
                    topNameTexts[i].text = response.entries[i].username;
                    topScoreTexts[i].text = response.entries[i].balance.ToString("N0");
                }
                else
                {
                    topNameTexts[i].text = "Нет данных";
                    topScoreTexts[i].text = "-";
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
        }
    }
    
    public void OpenClickerPage()
    {
        SetPages(clickerPage);
        SetTabColors(clickerTabButton, transferTabButton, topTabButton, boostTabButton);
    }

    public void OpenTransferPage()
    {
        SetPages(transferPage);
        SetTabColors(transferTabButton, clickerTabButton, topTabButton, boostTabButton);
    }

    public async void OpenTopPage()
    {
        SetPages(topPage);
        SetTabColors(topTabButton, clickerTabButton, transferTabButton, boostTabButton);
        await LoadTopAsync();
    }

    private void SetPages(GameObject activePage)
    {
        if (clickerPage != null) clickerPage.SetActive(activePage == clickerPage);
        if (transferPage != null) transferPage.SetActive(activePage == transferPage);
        if (topPage != null) topPage.SetActive(activePage == topPage);
        if (boostPage != null) boostPage.SetActive(activePage == boostPage);
    }

    private void SetTabColors(Button activeButton, Button inactiveButton1, Button inactiveButton2, Button inactiveButton3)
    {
        PaintTab(activeButton, true);
        PaintTab(inactiveButton1, false);
        PaintTab(inactiveButton2, false);
        PaintTab(inactiveButton3, false);
    }

    private void PaintTab(Button button, bool isActive)
    {
        if (button == null)
        {
            return;
        }

        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = isActive ? _activeTabColor : _inactiveTabColor;
        }

        var label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.color = isActive ? _activeTabTextColor : _inactiveTabTextColor;
        }
    }

    private void RefreshProfileUI()
    {
        if (_profile == null)
        {
            return;
        }

        if (profileNameText != null)
        {
            profileNameText.text = _profile.username;
        }

        if (coinsPerSecondText != null)
        {
            coinsPerSecondText.text = $"Бонус: +{_profile.tapBonusPercent}%";
        }

        if (balanceText != null)
        {
            balanceText.text = _profile.balance.ToString("N0");
        }

        if (incomeText != null)
        {
            incomeText.text = $"+{_profile.tapReward} за тап";
        }

        if (clickPowerText != null)
        {
            clickPowerText.text = $"Уровень тапа: {_profile.tapUpgradeLevel}";
        }

        if (autoIncomeText != null)
        {
            autoIncomeText.text = $"Следующий апгрейд: {_profile.nextTapUpgradeCost}";
        }

        SetButtonLabel(quickUpgradeButton, $"Улучшить {_profile.nextTapUpgradeCost}");
        SetButtonLabel(boostItem1Button, "-");
        SetButtonLabel(boostItem2Button, "OFF");
    }

    private static void SetButtonLabel(Button button, string text)
    {
        if (button == null)
        {
            return;
        }

        var label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.text = text;
        }
    }

    public void ShowMainMessage(string message)
    {
        if (incomeText != null)
        {
            incomeText.text = message;
        }
    }

    private bool BeginRequest()
    {
        if (_isBusy)
        {
            return false;
        }

        _isBusy = true;
        SetButtonsEnabled(false);
        return true;
    }

    private void EndRequest()
    {
        _isBusy = false;
        SetButtonsEnabled(true);
    }

    private void SetButtonsEnabled(bool value)
    {
        if (coinButton != null) coinButton.interactable = value;
        if (quickUpgradeButton != null) quickUpgradeButton.interactable = value;
        if (transferButton != null) transferButton.interactable = value;
        if (settingsButton != null) settingsButton.interactable = value;
    }

    private void DisableBoostPurchaseButtons()
    {
        if (boostItem1Button != null)
        {
            boostItem1Button.interactable = false;
        }

        if (boostItem2Button != null)
        {
            boostItem2Button.interactable = false;
        }
    }

    public void Logout()
    {
        AuthSession.Clear();
        MainSceneApiClient.ClearAuthorization();

        if (!string.IsNullOrWhiteSpace(loginSceneName))
        {
            SceneManager.LoadScene(loginSceneName);
        }
    }
}
