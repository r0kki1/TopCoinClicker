$ErrorActionPreference = "Stop"

$outputPath = "C:\Users\r0kki\TopCoinClicker\TopCoinClicker_Presentation.pptx"

$slides = @(
    @{
        Title = "TopCoinClicker"
        Bullets = @(
            "Клиент-серверная игра-кликер на Unity и ASP.NET Core",
            "Авторизация, сохранение прогресса и таблица лидеров",
            "Игровые действия выполняются через Web API"
        )
    },
    @{
        Title = "Идея Проекта"
        Bullets = @(
            "Сделать не просто локальный кликер, а полноценное приложение",
            "Хранить прогресс и игровую логику на сервере",
            "Подготовить архитектуру для дальнейшего расширения"
        )
    },
    @{
        Title = "Основной Функционал"
        Bullets = @(
            "Вход в аккаунт",
            "Загрузка профиля игрока",
            "Клик по монете и начисление награды",
            "Улучшение силы тапа",
            "Перевод монет другим игрокам",
            "Таблица лидеров"
        )
    },
    @{
        Title = "Архитектура"
        Bullets = @(
            "Клиент: Unity, UI на TextMeshPro",
            "Сервер: ASP.NET Core Web API",
            "База данных: SQLite",
            "Обмен данными: HTTP + JSON",
            "Безопасность: JWT access token и refresh token"
        )
    },
    @{
        Title = "Как Работает Система"
        Bullets = @(
            "Пользователь входит в аккаунт",
            "Unity получает токены и загружает профиль",
            "Игрок отправляет игровые действия на сервер",
            "Сервер обновляет баланс, улучшения и рейтинг",
            "Клиент получает ответ и обновляет интерфейс"
        )
    },
    @{
        Title = "Игровая Логика"
        Bullets = @(
            "Награда за тап зависит от уровня улучшения",
            "Стоимость улучшения растет по мере прогресса",
            "Перевод монет проверяет баланс и получателя",
            "Топ игроков строится по балансу"
        )
    },
    @{
        Title = "Авторизация И Безопасность"
        Bullets = @(
            "Вход реализован через backend",
            "Сервер выдает access token и refresh token",
            "Access token используется для API-запросов",
            "При истечении access token клиент делает refresh",
            "Если refresh token истек, пользователь входит заново"
        )
    },
    @{
        Title = "Что Было Важно В Разработке"
        Bullets = @(
            "Синхронизация Unity-клиента и backend",
            "Понятная обработка ошибок для пользователя",
            "Защита игровых данных от подмены на клиенте",
            "Корректная реализация refresh token flow"
        )
    },
    @{
        Title = "Почему Проект Полноценный"
        Bullets = @(
            "Есть клиент, сервер и база данных",
            "Есть реальная авторизация и хранение прогресса",
            "Есть взаимодействие между игроками",
            "Архитектура уже готова к масштабированию"
        )
    },
    @{
        Title = "Что Можно Улучшить Дальше"
        Bullets = @(
            "Пассивный доход и бусты",
            "Ежедневные награды",
            "История переводов",
            "Достижения и магазин",
            "Более богатая визуальная часть"
        )
    },
    @{
        Title = "Итог"
        Bullets = @(
            "TopCoinClicker - это рабочий клиент-серверный clicker",
            "Проект объединяет Unity, Web API и JWT-авторизацию",
            "Реализованы базовые игровые механики и безопасная сессия",
            "Проект можно дальше развивать как полноценный продукт"
        )
    }
)

$powerPoint = $null
$presentation = $null

try {
    $powerPoint = New-Object -ComObject PowerPoint.Application
    $powerPoint.Visible = -1
    $presentation = $powerPoint.Presentations.Add()

    foreach ($slideData in $slides) {
        $slide = $presentation.Slides.Add($presentation.Slides.Count + 1, 2)
        $slide.Shapes.Title.TextFrame.TextRange.Text = $slideData.Title

        $textShape = $slide.Shapes.Item(2)
        $textRange = $textShape.TextFrame.TextRange
        $textRange.Text = ""

        for ($i = 0; $i -lt $slideData.Bullets.Count; $i++) {
            $bullet = $slideData.Bullets[$i]

            if ($i -eq 0) {
                $textRange.Text = $bullet
            }
            else {
                $textRange.InsertAfter("`r`n$bullet") | Out-Null
            }

            $paragraph = $textRange.Paragraphs($i + 1)
            $paragraph.ParagraphFormat.Bullet.Visible = -1
        }

        $slide.Shapes.Title.TextFrame.TextRange.Font.Name = "Aptos Display"
        $slide.Shapes.Title.TextFrame.TextRange.Font.Size = 28
        $textShape.TextFrame.TextRange.Font.Name = "Aptos"
        $textShape.TextFrame.TextRange.Font.Size = 20
    }

    if (Test-Path $outputPath) {
        Remove-Item $outputPath -Force
    }

    $presentation.SaveAs($outputPath)
    Write-Output "Saved: $outputPath"
}
finally {
    if ($presentation) {
        $presentation.Close()
    }

    if ($powerPoint) {
        $powerPoint.Quit()
    }

    if ($presentation) {
        [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($presentation)
    }

    if ($powerPoint) {
        [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($powerPoint)
    }

    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
}

