# SteamHourFarmer (v2.0.0)

A modern, lightweight .NET application for idling Steam games to boost your hour count, designed to run as a self-hosted Docker service.

This application has been refactored to a **"one-container-one-bot"** model. Each Steam account you want to farm runs in its own lightweight container, allowing for easy configuration and management.

## Features

  * **Docker-First:** Designed to be run as a lightweight, self-hosted container. The official image is available on [Docker Hub](https://hub.docker.com/r/babisque/steam-hour-farmer).
  * **Multi-Account Management:** Easily run multiple accounts by simply starting multiple containers.
  * **Environment Variable Configuration:** No `appsettings.json`. Configure your user, password, and games via environment variables.
  * **Multi-Game:** Idle multiple games on a single account by providing a list of AppIDs.
  * **Status Control:** Set your status to "Online" or "Offline" on a per-account basis.
  * **Resilient:** Automatically retries connection and login on disconnects.
  * **Secure:** After the first login, authentication tokens are saved to a Docker volume, removing the need to use your password again.

## How to Run (Docker)

### Prerequisites

  * [Docker](https://www.docker.com/products/docker-desktop/) installed.

### Step 1: Pull the Image from Docker Hub

The image is publicly available on Docker Hub. Pull the 2.0.0 tag (or `:latest` if you prefer):

```bash
docker pull babisque/steam-hour-farmer:2.0.0
```

### Step 2: Create a Folder for Tokens

You need a folder on your host PC to store the authentication tokens. This ensures you don't need to log in with 2FA every time the container restarts.

**Windows Example (PowerShell):**

```powershell
mkdir C:\docker-data\steam-tokens-account1
```

**Linux/macOS Example:**

```bash
mkdir -p /home/$USER/docker-data/steam-tokens-account1
```

*(It is recommended to use a separate folder for each account.)*

### Step 3: Start the Bot Container

This is the main command. You will run it **once for each Steam account**.

```bash
docker run -d \
  --name steam-farmer-account1 \
  -v C:\docker-data\steam-tokens-account1:/app/data/tokens \
  -e "STEAM_USERNAME=my_steam_account" \
  -e "STEAM_PASSWORD=my_super_secret_password" \
  -e "STEAM_GAMES=730,440" \
  -e "STEAM_ONLINE=false" \
  babisque/steam-hour-farmer:2.0.0
```

*(On Linux/macOS, change the volume path: `-v /home/$USER/docker-data/steam-tokens-account1:/app/data/tokens`)*

### Running Multiple Accounts

To add a second account, just repeat the `docker run` command with different data:

```bash
docker run -d \
  --name steam-farmer-account2 \
  -v C:\docker-data\steam-tokens-account2:/app/data/tokens \
  -e "STEAM_USERNAME=my_other_account" \
  -e "STEAM_PASSWORD=another_password_123" \
  -e "STEAM_GAMES=570" \
  -e "STEAM_ONLINE=true" \
  babisque/steam-hour-farmer:2.0.0
```

**Important:**

1.  Use a different `--name` for each container.
2.  Use a different volume path (`-v`) for each account so the tokens don't get mixed up.

-----

## Configuration (Environment Variables)

Configure your bot by passing these `-e` flags in the `docker run` command.

| Variable | Required | Description | Example |
| :--- | :--- | :--- | :--- |
| `STEAM_USERNAME` | **Yes** | Your Steam login username. | `my_account` |
| `STEAM_PASSWORD` | **Yes** | Your Steam password. (Only used for the first login). | `password123` |
| `STEAM_GAMES` | No | Comma-separated list of AppIDs to idle. | `730,440,570` |
| `STEAM_ONLINE` | No | If `true`, you will appear "Online". If `false` (default), you appear "Offline". | `false` |

### Persistent Volume

  * `/app/data/tokens`: The container stores authentication tokens in this path. You **must** map this path to a local folder using `-v` to make your logins persistent.

-----

## First-Time Login (Steam Guard / 2FA)

On the **first login** for an account, the bot will need your 2FA (Steam Guard) code, as `SteamSession` uses the `UserConsoleAuthenticator`.

1.  Run the `docker run` command as shown above.
2.  View your container's logs:
    ```bash
    docker logs steam-farmer-account1
    ```
3.  The log will prompt you for the 2FA code.
4.  To enter the code, "attach" to the container:
    ```bash
    docker attach steam-farmer-account1
    ```
5.  Type the 2FA code and press Enter.
6.  To detach from the container without stopping it, press **`Ctrl+P`** then **`Ctrl+Q`**.

After this, the token will be saved in your volume, and you will never need to enter your password or 2FA code again.

## Building from Source

1.  Install the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (or newer) and [Docker](https://www.docker.com/products/docker-desktop/).
2.  Clone this repository: `git clone https://github.com/babisque/steam-hour-farmer.git`
3.  `cd steam-hour-farmer`
4.  Build your own Docker image:
    ```bash
    docker build -t your-name/steam-hour-farmer:latest .
    ```
5.  Run the image you just built (use the `docker run` command from the "How to Run" section).

## License

This project is licensed under the MIT License. See the `LICENSE` file for details.