using Polly;
using SteamKit2;
using SteamKit2.Authentication;
using SteamKit2.Internal;

namespace SteamHourFarmer.Bot;

public class Bot
{
    private readonly AccountConfig _config;

    private readonly SteamClient _steamClient;
    private readonly CallbackManager _callbackManager;
    private readonly SteamUser _steamUser;
    private readonly SteamFriends _steamFriends;
    
    private bool _isRunning;
    private TaskCompletionSource<bool>? _loginTcs;
    private static readonly TimeSpan LoginTimeout = TimeSpan.FromMinutes(10);

    public Bot(AccountConfig config, string dataDirectory, ISentryStorage sentryStorage)
    {
        _config = config;

        _steamClient = new SteamClient();
        _steamUser = _steamClient.GetHandler<SteamUser>()!;
        _steamFriends = _steamClient.GetHandler<SteamFriends>()!;
        
        _callbackManager = new CallbackManager(_steamClient);
        
        _callbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
        _callbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
        _callbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
        _callbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
        _callbackManager.Subscribe<SteamUser.AccountInfoCallback>(OnAccountInfo);
    }

    private void Log(string message)
    {
        Console.WriteLine($"[{_config.Username}] {message}");
    }

    /// <summary>
    /// Starts the bot, connects to Steam, and runs the callback loop.
    /// </summary>
    public async Task StartAsync()
    {
        _isRunning = true;
        
        // Retry policy with exponential backoff
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(15,
                retryAttempt => TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, retryAttempt))),
                (ex, timespan) => Log($"Connection/login failed: {ex.Message}. Retrying in {timespan.TotalSeconds}s..."));

        await retryPolicy.ExecuteAsync(ConnectAndRunLoopAsync);
        
        Log("Could not re-login after multiple attempts, stopping.");
    }
    
    /// <summary>
    /// Connects to Steam and runs the callback loop. This is the core logic.
    /// </summary>
    private async Task ConnectAndRunLoopAsync()
    {
        // Start the callback loop in a background thread
        var cts = new CancellationTokenSource();
        _ = Task.Run(() => CallbackLoop(cts.Token));

        Log("Connecting to Steam network...");
        _steamClient.Connect();

        // Prepare TCS for login
        _loginTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var timeoutCts = new CancellationTokenSource(LoginTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cts.Token);
        
        // Register cancellation for timeout
        using (linkedCts.Token.Register(() => _loginTcs.TrySetException(new TimeoutException("Login timed out."))))
        {
            await _loginTcs.Task; // wait until login success/failure
        }

        // After successful login keep process alive indefinitely.
        await Task.Delay(Timeout.Infinite, cts.Token);
    }


    // The main loop that dispatches callbacks from Steam.
    private void CallbackLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            _callbackManager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
        }
    }


    private async void OnConnected(SteamClient.ConnectedCallback callback)
    {
        Log("Connected to Steam! Logging in...");

        try
        {
            // Use modern authentication flow
            var authSession = await _steamClient.Authentication.BeginAuthSessionViaCredentialsAsync(new AuthSessionDetails
            {
                Username = _config.Username,
                Password = _config.Password,
                IsPersistentSession = true,
                Authenticator = new UserConsoleAuthenticator(),
            });

            // Poll Steam for authentication response
            var pollResponse = await authSession.PollingWaitForResultAsync();

            Log("Authentication successful! Logging on...");

            // Log on to Steam with the access token
            _steamUser.LogOn(new SteamUser.LogOnDetails
            {
                Username = pollResponse.AccountName,
                AccessToken = pollResponse.RefreshToken,
                ShouldRememberPassword = true,
            });
        }
        catch (Exception ex)
        {
            Log($"Authentication failed: {ex.Message}");
            _loginTcs?.TrySetException(ex);
        }
    }

    private void OnDisconnected(SteamClient.DisconnectedCallback callback)
    {
        Log("Disconnected from Steam.");
        
        if (_isRunning && _loginTcs is not null && !_loginTcs.Task.IsCompleted)
        {
            _loginTcs.TrySetException(new Exception("Disconnected during login"));
        }
        
        if (_isRunning)
        {
            throw new Exception("Disconnected, triggering retry...");
        }
    }

    private void OnLoggedOn(SteamUser.LoggedOnCallback callback)
    {
        if (callback.Result == EResult.OK)
        {
            Log("Logged in successfully!");
            _loginTcs?.TrySetResult(true);
            return;
        }

        Log($"Login failed: {callback.Result} / {callback.ExtendedResult}");
        _loginTcs?.TrySetException(new Exception($"Login failed: {callback.Result}"));
    }
    
    private void OnAccountInfo(SteamUser.AccountInfoCallback callback)
    {
        if (_config.Online == true)
        {
            _steamFriends.SetPersonaState(EPersonaState.Online);
        }
        else
        {
            _steamFriends.SetPersonaState(EPersonaState.Offline);
        }
        PlayGames();
    }

    private void OnLoggedOff(SteamUser.LoggedOffCallback callback)
    {
        Log($"Logged off: {callback.Result}");
    }

    /// <summary>
    /// Play configured games (sends ClientGamesPlayed message). If blocked, send empty list.
    /// </summary>
    private void PlayGames()
    {
        try
        {
            var msg = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayed);
            if (_config.Games.Length == 0)
            {
                Log("Not playing any games (none configured).");
                msg.Body.games_played.Clear();
            }
            else
            {
                foreach (var appId in _config.Games)
                {
                    msg.Body.games_played.Add(new CMsgClientGamesPlayed.GamePlayed { game_id = (ulong)appId });
                }
                Log($"Playing {_config.Games.Length} games.");
            }
            _steamClient.Send(msg);
        }
        catch (Exception ex)
        {
            Log($"Failed to send games list: {ex.Message}");
        }
    }
}