using Microsoft.Extensions.Logging;
using Polly;
using SteamHourFarmer.Core;
using SteamHourFarmer.Core.Interfaces;
using SteamKit2;
using SteamKit2.Authentication;
using SteamKit2.Internal;

namespace SteamHourFarmer.Infrastructure;

public class SteamSession : ISteamSession
{
    private readonly AccountConfig _config;
    private readonly ILogger<SteamSession> _logger;

    private readonly SteamClient _steamClient;
    private readonly CallbackManager _callbackManager;
    private readonly SteamUser _steamUser;
    private readonly SteamFriends _steamFriends;
    
    private bool _isRunning;
    private TaskCompletionSource<bool>? _loginTcs;
    private static readonly TimeSpan LoginTimeout = TimeSpan.FromMinutes(10);
    private CancellationTokenSource _internalCts = new();

    public SteamSession(AccountConfig config, ILogger<SteamSession> logger)
    {
        _config = config;
        _logger = logger;

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
    
    /// <summary>
    /// Starts the bot, connects to Steam, and runs the callback loop.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _isRunning = true;
        _internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(15,
                retryAttempt => TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, retryAttempt))),
                (ex, timespan) => _logger.LogWarning(ex, "[{Username}] Connection/login failed: {Message}. Retrying in {TotalSeconds}s...", _config.Username, ex.Message, timespan.TotalSeconds));
        
        try
        {
            await retryPolicy.ExecuteAsync(ConnectAndRunLoopAsync);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Username}] A fatal, non-retryable error occurred: {Message}", _config.Username, ex.Message);
        }
        finally
        {
            _isRunning = false;
            _logger.LogWarning("[{Username}] Could not re-login after multiple attempts, stopping.", _config.Username);
        }
    }
    
    /// <summary>
    /// Connects to Steam and runs the callback loop. This is the core logic.
    /// </summary>
    private async Task ConnectAndRunLoopAsync()
    {
        _ = Task.Run(() => CallbackLoop(_internalCts.Token), _internalCts.Token);

        _logger.LogInformation("[{Username}] Connecting to Steam network...", _config.Username);
        _steamClient.Connect();

        _loginTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var timeoutCts = new CancellationTokenSource(LoginTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, _internalCts.Token);

        await using (linkedCts.Token.Register(() => _loginTcs.TrySetException(new TimeoutException("Login timed out."))))
        {
            await _loginTcs.Task;
        }

        await Task.Delay(Timeout.Infinite, _internalCts.Token);
    }


    // The main loop that dispatches callbacks from Steam.
    private void CallbackLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _isRunning)
        {
            try
            {
                _callbackManager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "[{Username}] Exception in callback loop: {Message}", _config.Username, ex.Message);
            }
        }
        _logger.LogDebug("[{Username}] Callback loop exiting.", _config.Username);
    }


    private async void OnConnected(SteamClient.ConnectedCallback callback)
    {
        _logger.LogInformation("[{Username}] Connected to Steam! Logging in...", _config.Username);

        try
        {
            var authSession = await _steamClient.Authentication.BeginAuthSessionViaCredentialsAsync(new AuthSessionDetails
            {
                Username = _config.Username,
                Password = _config.Password,
                IsPersistentSession = true,
                Authenticator = new UserConsoleAuthenticator(),
            });

            var pollResponse = await authSession.PollingWaitForResultAsync();

            _logger.LogInformation("[{Username}] Authentication successful! Logging on...", _config.Username);

            _steamUser.LogOn(new SteamUser.LogOnDetails
            {
                Username = pollResponse.AccountName,
                AccessToken = pollResponse.RefreshToken,
                ShouldRememberPassword = true,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Username}] Authentication failed: {Message}", _config.Username, ex.Message);
            _loginTcs?.TrySetException(ex);
        }
    }

    private void OnDisconnected(SteamClient.DisconnectedCallback callback)
    {
        _logger.LogWarning("[{Username}] Disconnected from Steam.", _config.Username);
        
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
            _logger.LogInformation("[{Username}] Logged in successfully!", _config.Username);
            _loginTcs?.TrySetResult(true);
            return;
        }

        _logger.LogError("[{Username}] Login failed: {Result} / {ExtendedResult}", _config.Username, callback.Result, callback.ExtendedResult);
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
        _logger.LogInformation("[{Username}] Logged off: {Result}", _config.Username, callback.Result);
    }

    /// <summary>
    /// Play configured games (sends ClientGamesPlayed message).
    /// </summary>
    private void PlayGames()
    {
        try
        {
            var msg = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayed);
            if (_config.Games.Length == 0)
            {
                _logger.LogInformation("[{Username}] Not playing any games (none configured).", _config.Username);
                msg.Body.games_played.Clear();
            }
            else
            {
                foreach (var appId in _config.Games)
                {
                    msg.Body.games_played.Add(new CMsgClientGamesPlayed.GamePlayed { game_id = (ulong)appId });
                }
                _logger.LogInformation("[{Username}] Playing {GameCount} games.", _config.Username, _config.Games.Length);
            }
            _steamClient.Send(msg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Username}] Failed to send games list: {Message}", _config.Username, ex.Message);
        }
    }
}