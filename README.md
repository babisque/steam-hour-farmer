# SteamHourFarmer

A modern, lightweight .NET application for idling Steam games to boost your hour count across multiple accounts.

This application runs as a resilient background worker, managing connections for any number of Steam accounts you configure. It's built with .NET, making it cross-platform (Windows, Linux, macOS) and efficient.

## Features

  * **Multi-Account:** Run multiple Steam accounts simultaneously.
  * **Multi-Game:** Idle any number of games per account.
  * **Status Control:** Set your online status to "Online" or "Offline" on a per-account basis.
  * **Resilient:** Automatically retries connection and login on disconnects, with an exponential backoff strategy.
  * **Cross-Platform:** Runs on Windows, Linux, and macOS.
  * **Lightweight:** Runs as a .NET Worker Service, consuming minimal resources.
  * **Secure:** After the first login, authentication tokens are saved in a local `tokens` folder. Your password is not stored or needed for subsequent logins.

## Getting Started

### 1\. Download

Go to the **[Releases page](https://github.com/babisque/steam-hour-farmer/releases)** and download the latest `.zip` file for your operating system (e.g., `win-x64`, `linux-x64`).

### 2\. Configure

1.  Unzip the downloaded file.
2.  Inside the unzipped folder, create a file named `appsettings.json`.
3.  Copy and paste the template below into your new `appsettings.json` file.
4.  Edit the file to add your own account(s).

**`appsettings.json` Template:**

```json
{
  "SteamAccounts": [
    {
      "Username": "your_first_account",
      "Password": "your_first_password",
      "games": [ 730, 440 ],
      "online": true
    },
    {
      "Username": "your_second_account",
      "Password": "your_second_password",
      "games": [ 570 ],
      "online": false
    },
    {
      "Username": "offline_no_games",
      "Password": "another_password",
      "games": [],
      "online": false
    }
  ]
}
```

#### Configuration Fields

  * `"Username"`: Your Steam login name.
  * `"Password"`: Your Steam password. **This is only used for the very first login.**
  * `"games"`: An array of Steam AppIDs you want to idle. You can find AppIDs on sites like [SteamDB](https://www.google.com/search?q=httpss://steamdb.info/). Use an empty list `[]` to idle no games.
  * `"online"`: Set to `true` to appear "Online" or `false` to appear "Offline" while idling.

### 3\. Run

Open a terminal or command prompt in the folder where you unzipped the files.

  * **On Windows:**
    ```bash
    .\SteamHourFarmer.Worker.exe
    ```
  * **On Linux / macOS:**
    ```bash
    # Make the file executable (only need to do this once)
    chmod +x SteamHourFarmer.Worker

    # Run the application
    ./SteamHourFarmer.Worker
    ```

### First-Time Login (Steam Guard / 2FA)

The **first time** you log in with each account, the application will pause and ask for your **Steam Guard (2FA) code** in the console.

Type the code and press Enter. After a successful login, an authentication token is saved in the `tokens` folder. You will not need to enter your password or 2FA code for that account ever again.

## Advanced Configuration

You can override default paths using environment variables or command-line arguments.

  * `--config` (argument) or `CONFIG_PATH` (environment variable):
    Specifies a custom path for your configuration JSON file.
    *Default: `./config.json`*
    *(Note: `appsettings.json` is always loaded first).*

  * `TOKEN_STORAGE_DIRECTORY` (environment variable):
    Specfies a custom directory to store the authentication tokens.
    *Default: `./tokens`*.

## Building from Source

If you want to build the project yourself:

1.  Install the [.NET 9 SDK](https://www.google.com/search?q=httpss://dotnet.microsoft.com/download/dotnet/9.0) (or newer).
2.  Clone this repository: `git clone httpss://github.com/babisque/steam-hour-farmer.git`
3.  `cd steam-hour-farmer`
4.  Run the `publish` command for your platform (e.g., `win-x64`, `linux-x64`):
    ```bash
    dotnet publish -c Release -r win-x64 --self-contained true
    ```
5.  The runnable application will be in the `SteamHourFarmer.Worker/bin/Release/net9.0/win-x64/publish/` folder.

## License

This project is licensed under the MIT License. See the `LICENSE` file for details.