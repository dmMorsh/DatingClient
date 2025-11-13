# DatingClient — Расширенная справка разработчика

> Этот документ содержит детальные примеры кода и рекомендации по разработке

## 📝 Справка разработчика

### Добавление новой страницы

1. **Создать XAML страницу** в `Views/NewPage.xaml`

```xaml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="DatingClient.Views.NewPage"
             Title="New Page">
    <VerticalStackLayout Padding="20" Spacing="10">
        <Label Text="Welcome to New Page" FontSize="24" FontAttributes="Bold" />
        <Entry Placeholder="Введите текст" Text="{Binding InputText, Mode=TwoWay}" />
        <Button Text="Submit" Command="{Binding SubmitCommand}" />
        <ActivityIndicator IsRunning="{Binding IsLoading}" />
    </VerticalStackLayout>
</ContentPage>
```

2. **Создать ViewModel** в `ViewModels/NewViewModel.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DatingClient.Services;

namespace DatingClient.ViewModels;

public partial class NewViewModel : ObservableObject
{
    private readonly ApiService _api;

    [ObservableProperty]
    private string inputText = "";

    [ObservableProperty]
    private bool isLoading = false;

    public NewViewModel(ApiService api)
    {
        _api = api;
    }

    [RelayCommand]
    public async Task SubmitAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText)) return;

        IsLoading = true;
        try
        {
            // Отправить данные на сервер
            var result = await _api.PostAsync<ResponseDto>("/endpoint", new { text = InputText });

            // Обработать результат
            await Shell.Current.DisplayAlert("Success", "Данные отправлены", "OK");
            InputText = "";  // Очистить поле
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

3. **Регистрировать в MauiProgram.cs**

```csharp
builder.Services.AddTransient<NewViewModel>();
builder.Services.AddTransient<NewPage>();
```

4. **Добавить маршрут в AppShell.xaml**

```xaml
<ShellContent Title="New" Route="new" ContentTemplate="{DataTemplate views:NewPage}" />
```

5. **Навигация из другой страницы**

```csharp
await Shell.Current.GoToAsync("new");
```

---

### Работа с API

**Базовый GET запрос:**

```csharp
[RelayCommand]
public async Task LoadUserAsync(string userId)
{
    IsLoading = true;
    try
    {
        var user = await _apiService.GetAsync<User>($"/users/{userId}");
        UserName = user?.Name ?? "Unknown";
    }
    catch (Exception ex)
    {
        await Shell.Current.DisplayAlert("Ошибка", ex.Message, "OK");
    }
    finally
    {
        IsLoading = false;
    }
}
```

**POST запрос с данными:**

```csharp
[RelayCommand]
public async Task CreateProfileAsync()
{
    var profileData = new
    {
        Name = UserName,
        Age = Age,
        Bio = Biography
    };

    var result = await _apiService.PostAsync<CreateProfileResponse>("/users/profile", profileData);

    if (result != null)
    {
        UserId = result.Id;
        await Shell.Current.DisplayAlert("Success", "Профиль создан", "OK");
    }
}
```

**PUT запрос (обновление):**

```csharp
[RelayCommand]
public async Task UpdateProfileAsync()
{
    var updateData = new { Name = UserName, Bio = Biography };

    var updated = await _apiService.PutAsync<User>($"/users/{UserId}", updateData);

    if (updated != null)
    {
        CurrentUser = updated;
    }
}
```

**DELETE запрос:**

```csharp
[RelayCommand]
public async Task DeleteProfileAsync()
{
    var confirmed = await Shell.Current.DisplayAlert(
        "Удалить профиль?",
        "Это действие необратимо",
        "Да", "Нет"
    );

    if (confirmed)
    {
        await _apiService.DeleteAsync<object>($"/users/{UserId}");
        await Shell.Current.GoToAsync("login");
    }
}
```

---

### Работа с WebSocket и чатами

**Полный пример чата:**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DatingClient.Models;
using DatingClient.Services;

namespace DatingClient.ViewModels;

public partial class MessagesViewModel : ObservableObject
{
    private readonly SocketService _socketService;
    private long _currentChatId;

    [ObservableProperty]
    private ObservableCollection<Message> messages = new();

    [ObservableProperty]
    private string currentMessage = "";

    [ObservableProperty]
    private bool isConnected = false;

    public MessagesViewModel(SocketService socketService)
    {
        _socketService = socketService;
        _socketService.OnMessageReceived += OnMessageReceived;
    }

    // Вызывается когда страница становится видимой
    public async Task OnAppearingAsync(long chatId)
    {
        _currentChatId = chatId;
        await _socketService.ConnectAsync();
        IsConnected = SocketService.IsConnected;
    }

    // Вызывается когда страница уходит со сцены
    public async Task OnDisappearingAsync()
    {
        // Можно отключиться или оставить соединение
        // await _socketService.DisconnectAsync();
    }

    // Обработчик входящих сообщений
    private void OnMessageReceived(WSMessage msg)
    {
        // Убедиться, что это для нашего чата
        if (msg.ChatId != _currentChatId) return;

        // Добавить в основной поток UI
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Messages.Add(new Message
            {
                Content = msg.Content,
                Timestamp = DateTime.Now,
                SenderId = msg.SenderId,
                IsFromMe = msg.SenderId == _apiService.UserId
            });
        });
    }

    [RelayCommand]
    public async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentMessage)) return;

        var messageContent = CurrentMessage;

        // Оптимистичное обновление UI (добавить сразу, не дожидаясь ответа)
        Messages.Add(new Message
        {
            Content = messageContent,
            Timestamp = DateTime.Now,
            IsFromMe = true
        });

        CurrentMessage = "";  // Очистить поле ввода

        // Отправить на сервер асинхронно
        try
        {
            var wsMessage = new WSMessage
            {
                Type = Constants.Message,
                Content = messageContent,
                ChatId = _currentChatId
            };

            await _socketService.SendMessageAsync(wsMessage);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Send message error: {ex.Message}");
            // Можно показать алерт об ошибке
        }
    }
}
```

**XAML для чата:**

```xaml
<Grid RowDefinitions="*,Auto" Padding="10" Spacing="10">
    <!-- Список сообщений -->
    <CollectionView Grid.Row="0"
                    ItemsSource="{Binding Messages}"
                    SelectionMode="None"
                    VerticalScrollBarVisibility="Always">
        <CollectionView.ItemTemplate>
            <DataTemplate>
                <StackLayout Padding="10" Spacing="3"
                             Margin="10,0,10,5">
                    <!-- Сообщение -->
                    <Frame CornerRadius="12"
                           Padding="12,8"
                           HasShadow="False"
                           BackgroundColor="{Binding IsFromMe, Converter={StaticResource BoolToColorConverter}}">
                        <Label Text="{Binding Content}"
                               FontSize="14"
                               TextColor="{Binding IsFromMe, Converter={StaticResource InverseBoolConverter}, StringFormat='Color'}"/>
                    </Frame>

                    <!-- Время -->
                    <Label Text="{Binding Timestamp, StringFormat='{0:HH:mm}'}"
                           FontSize="11"
                           TextColor="Gray"
                           Margin="10,0,10,0"/>
                </StackLayout>
            </DataTemplate>
        </CollectionView.ItemTemplate>
    </CollectionView>

    <!-- Поле ввода + кнопка отправки -->
    <Grid Grid.Row="1" ColumnDefinitions="*,Auto" Spacing="5" Padding="5">
        <Entry Grid.Column="0"
               Placeholder="Написать сообщение..."
               Text="{Binding CurrentMessage, Mode=TwoWay}"
               FontSize="14"/>

        <Button Grid.Column="1"
                Text="➤"
                Command="{Binding SendMessageCommand}"
                Padding="15,10"
                CornerRadius="5"
                BackgroundColor="{StaticResource Primary}"/>
    </Grid>
</Grid>
```

---

### Использование кэширования

**Загрузить с автоматическим кэшем:**

```csharp
private readonly CacheService _cacheService;

[RelayCommand]
public async Task LoadProfileAsync(long userId)
{
    IsLoading = true;
    try
    {
        // Автоматически проверит RAM → SQLite → API
        CurrentProfile = await _cacheService.GetOrFetchUserProfileAsync(userId);
    }
    catch (Exception ex)
    {
        await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
    }
    finally
    {
        IsLoading = false;
    }
}
```

**Очистить кэш:**

```csharp
[RelayCommand]
public async Task ClearCacheAsync()
{
    var confirmed = await Shell.Current.DisplayAlert(
        "Очистить кэш?",
        "Все сохранённые данные будут удалены",
        "Да", "Нет"
    );

    if (confirmed)
    {
        await _cacheService.ClearCacheAsync();
        await Shell.Current.DisplayAlert("Success", "Кэш очищен", "OK");
    }
}
```

---

### Работа с геолокацией

**Получить текущее местоположение:**

```csharp
private readonly LocationService _locationService;

[RelayCommand]
public async Task DetectLocationAsync()
{
    try
    {
        IsLoading = true;
        var location = await _locationService.GetCurrentLocationAsync();

        Latitude = location.Latitude;
        Longitude = location.Longitude;

        // Опционально: получить название города
        Location = await _locationService.GetCityNameAsync(location);

        await Shell.Current.DisplayAlert("Success",
            $"Location: {Location}", "OK");
    }
    catch (Exception ex)
    {
        await Shell.Current.DisplayAlert("Error",
            "Не удалось определить локацию: " + ex.Message, "OK");
    }
    finally
    {
        IsLoading = false;
    }
}
```

**Поиск и автодополнение локаций:**

```csharp
[ObservableProperty]
private string locationQuery = "";

[ObservableProperty]
private ObservableCollection<LocationSuggestion> locationSuggestions = new();

partial void OnLocationQueryChanged(string value)
{
    if (value.Length < 2)
    {
        LocationSuggestions.Clear();
        return;
    }

    SearchLocationsAsync(value);
}

[RelayCommand]
public async Task SearchLocationsAsync(string query)
{
    try
    {
        var suggestions = await _locationService.SearchLocationsAsync(query);

        LocationSuggestions.Clear();
        foreach (var suggestion in suggestions)
        {
            LocationSuggestions.Add(suggestion);
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Location search error: {ex.Message}");
    }
}

[RelayCommand]
public void SelectLocation(LocationSuggestion suggestion)
{
    Location = suggestion.Name;
    Latitude = suggestion.Latitude;
    Longitude = suggestion.Longitude;

    // Скрыть список предложений
    LocationSuggestions.Clear();
    LocationQuery = "";
}
```

---

### Валидация и обработка ошибок

**Валидация в реальном времени:**

```csharp
[ObservableProperty]
private string email = "";

[ObservableProperty]
private string emailError = "";

[ObservableProperty]
private bool isEmailValid = false;

partial void OnEmailChanged(string value)
{
    // Валидация при каждом изменении
    if (string.IsNullOrWhiteSpace(value))
    {
        EmailError = "Email обязателен";
        IsEmailValid = false;
    }
    else if (!value.Contains("@") || !value.Contains("."))
    {
        EmailError = "Некорректный формат email";
        IsEmailValid = false;
    }
    else
    {
        EmailError = "";
        IsEmailValid = true;
    }
}
```

**Комплексная обработка ошибок:**

```csharp
[RelayCommand]
public async Task LoginAsync()
{
    // Валидация на клиенте
    if (!IsEmailValid || string.IsNullOrWhiteSpace(Password))
    {
        await Shell.Current.DisplayAlert("Error", "Заполните все поля корректно", "OK");
        return;
    }

    IsLoading = true;
    try
    {
        var result = await _apiService.PostAsync<LoginResponse>(
            "/auth/login",
            new { Email, Password }
        );

        if (result != null)
        {
            _apiService.FillFromLoginResponse(result);
            await Shell.Current.GoToAsync("search");
        }
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
    {
        await Shell.Current.DisplayAlert("Error", "Email или пароль неверный", "OK");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
    {
        await Shell.Current.DisplayAlert("Error", "Слишком много попыток. Попробуйте позже", "OK");
    }
    catch (HttpRequestException ex) when (ex.InnerException is TimeoutException)
    {
        await Shell.Current.DisplayAlert("Error", "Timeout: проверьте интернет", "OK");
    }
    catch (Exception ex)
    {
        await Shell.Current.DisplayAlert("Error", $"Ошибка: {ex.Message}", "OK");
    }
    finally
    {
        IsLoading = false;
    }
}
```

---

### Отладка и логирование

**Добавить логирование:**

```csharp
using System.Diagnostics;

public partial class DebugViewModel : ObservableObject
{
    [RelayCommand]
    public void LogDebugInfo()
    {
        Debug.WriteLine("=== DEBUG INFO ===");
        Debug.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        Debug.WriteLine($"CurrentUser: {CurrentUser?.Name ?? "null"}");
        Debug.WriteLine($"IsLoading: {IsLoading}");
        Debug.WriteLine($"CacheSize: {_cache.GetSize()}");
        Debug.WriteLine("==================");
    }
}
```

**Просмотр логов в Visual Studio:**

- Debug → Windows → Output
- Выбрать "Debug" в выпадающем списке
- Всё что выведено через `Debug.WriteLine()` появится здесь

---

## 🚀 Build и развертывание

### Для Android

```bash
# Debug сборка (для эмулятора/телефона)
dotnet publish -f net8.0-android -c Debug

# Release сборка (для Play Store)
dotnet publish -f net8.0-android -c Release
```

**Выходной файл:** `bin/Release/net8.0-android/com.companyname.datingclient.apk`

### Для iOS

```bash
# Требуется macOS и Xcode установлены
dotnet publish -f net8.0-ios -c Release
```

**Выходной файл:** `.ipa` пакет

---

## 🐛 Решение проблем

### "Unable to connect to API"

**Диагностика:**

```csharp
// В LoginViewModel
[RelayCommand]
public async Task TestConnectionAsync()
{
    try
    {
        var response = await _apiService.GetAsync<object>("/health");
        Debug.WriteLine("✅ API доступен");
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"❌ API недоступен: {ex.Message}");
        Debug.WriteLine($"API URL: {_apiService.BaseUrl}");
    }
}
```

**Решение:**

- Проверить IP и порт в Constants.cs
- Убедиться, что сервер запущен
- Проверить, что устройство в той же сети
- Для Android: добавить `android:usesCleartextTraffic="true"` в AndroidManifest.xml

### "WebSocket connection failed"

**Диагностика:**

```csharp
// В SocketService
private async Task ConnectAsync()
{
    try
    {
        Debug.WriteLine("🔌 Connecting to WebSocket...");
        Debug.WriteLine($"URL: {wsUrl}");
        await _ws.ConnectAsync(uri, CancellationToken.None);
        Debug.WriteLine("✅ WebSocket connected");
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"❌ WebSocket connection failed: {ex.Message}");
        Debug.WriteLine($"Exception type: {ex.GetType().Name}");
    }
}
```

**Решение:**

- Убедиться, что URL преобразуется правильно (http → ws)
- Проверить, что `/ws/start` возвращает sessionToken
- Проверить файерволл
- Убедиться, что WebSocket порт открыт

### "UI зависает"

**Проблема:** Долгая операция на главном потоке

**Решение:**

```csharp
// ❌ НЕПРАВИЛЬНО (замораживает UI)
public NewViewModel()
{
    var data = _cache.GetAllData().GetAwaiter().GetResult();  // ПЛОХО!
}

// ✅ ПРАВИЛЬНО (не блокирует UI)
public NewViewModel()
{
    LoadDataAsync();
}

[RelayCommand]
private async Task LoadDataAsync()
{
    var data = await _cache.GetAllDataAsync();  // Асинхронно
}
```

### "SQLite database locked"

**Решение:**

- Убедиться, что нет множественных одновременных операций
- Использовать `async` методы вместо синхронных
- Проверить, что БД файл не заблокирован другим процессом

---

## 💡 Лучшие практики

### ✅ DO (Делай)

- Используй `async/await` для всех I/O операций
- Отправляй запросы в фон, не блокируя UI
- Кэшируй часто запрашиваемые данные
- Показывай индикаторы загрузки при ожидании
- Обрабатывай ошибки с информативными сообщениями
- Используй `ObservableProperty` для двусторонней привязки
- Валидируй данные перед отправкой на сервер

### ❌ DON'T (Не делай)

- Не блокируй основной поток синхронными операциями
- Не игнорируй исключения (обрабатывай их)
- Не отправляй пароли или токены в логи
- Не создавай View без ViewModel
- Не забывай отписываться от событий (утечки памяти)
- Не используй строковые константы для API endpoints (используй Constants.cs)
- Не показывай технические ошибки пользователю (преобразуй в понятные сообщения)

---

**Последнее обновление:** November 13, 2025
