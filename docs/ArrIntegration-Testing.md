# Testing the Radarr/Sonarr (Arr) Integration

## 1. Prerequisites

- **Jellyfin server** – built and runnable (see below).
- **Web client (optional)** – needed if you want the normal Jellyfin UI; API testing works without it.
- **Radarr** (e.g. http://localhost:7878) and **Sonarr** (e.g. http://localhost:8989) running, with API keys from each app’s Settings → General.

## 2. Run the Jellyfin server

From the repo root:

```powershell
# With web client (replace with your jellyfin-web path if different)
dotnet run --project Jellyfin.Server --webdir "C:\path\to\jellyfin-web\dist"

# Without web client (API only – good for testing endpoints)
dotnet run --project Jellyfin.Server --nowebclient
```

Default URL: **http://localhost:8096**

- First run: complete the setup wizard in the browser (or use API).
- Note: Without a web client you can’t use the setup wizard; use an existing data directory or create an admin user via API.

### If the website doesn’t start

1. **Don’t use `--nowebclient`**  
   That flag disables the web UI. Omit it if you want the normal Jellyfin site.

2. **You must pass `--webdir`**  
   When running from source, the server does **not** ship with the web client. It looks for a `jellyfin-web` folder next to the executable (which doesn’t exist), so it will exit with an error like:
   > The server is expected to host the web client, but the provided content directory is either invalid or empty.

   Fix: point to the folder that **contains** the built web app (e.g. an `index.html` at the top level):
   ```powershell
   dotnet run --project Jellyfin.Server --webdir "C:\path\to\jellyfin-web\dist"
   ```
   Use the path where you downloaded or built [jellyfin-web](https://github.com/jellyfin/jellyfin-web) (often the `dist` folder after building).

3. **Open the correct URL**  
   The web UI is served under `/web`. Use:
   - **http://localhost:8096** (redirects to `/web/`), or  
   - **http://localhost:8096/web/**

4. **If the server exits immediately**  
   Check the console message. If it says the web content directory is invalid or empty, the `--webdir` path is wrong or that folder has no files. Ensure the folder exists and contains the built web client (e.g. `index.html`).

## 3. Get an auth token

You need a Jellyfin API key (auth token) for authenticated requests.

**Option A – From the web UI**

1. Open http://localhost:8096
2. Log in as an admin user
3. Dashboard → API Keys (or open **http://localhost:8096/web/index.html#!/apikeys.html**) and create/copy a key

**Option B – From the API (first-time setup)**

If the server has no users yet, create one and get a token via the API (see [Jellyfin API docs](https://api.jellyfin.org/) for `/Users` and auth).

For the rest of this guide, `YOUR_JELLYFIN_TOKEN` is your Jellyfin API key.

## 4. Test the Arr integration endpoints

Use PowerShell or any HTTP client. Base URL: `http://localhost:8096`.

### 4.1 Get current settings (admin)

```powershell
$token = "YOUR_JELLYFIN_TOKEN"
$headers = @{
  "X-Emby-Token" = $token
  "Content-Type" = "application/json"
}
Invoke-RestMethod -Uri "http://localhost:8096/ArrIntegration/Settings" -Headers $headers -Method Get
```

### 4.2 Save Radarr/Sonarr settings (admin)

```powershell
$body = @{
  radarrBaseUrl   = "http://localhost:7878"
  radarrApiKey    = "YOUR_RADARR_API_KEY"
  radarrEnabled   = $true
  sonarrBaseUrl   = "http://localhost:8989"
  sonarrApiKey    = "YOUR_SONARR_API_KEY"
  sonarrEnabled   = $true
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:8096/ArrIntegration/Settings" -Headers $headers -Method Post -Body $body
```

### 4.3 Test Radarr connection (admin)

```powershell
$testBody = @{ baseUrl = "http://localhost:7878"; apiKey = "YOUR_RADARR_API_KEY" } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8096/ArrIntegration/Radarr/Test" -Headers $headers -Method Post -Body $testBody
# Expect: { "success": true }
```

### 4.4 Test Sonarr connection (admin)

```powershell
$testBody = @{ baseUrl = "http://localhost:8989"; apiKey = "YOUR_SONARR_API_KEY" } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8096/ArrIntegration/Sonarr/Test" -Headers $headers -Method Post -Body $testBody
# Expect: { "success": true }
```

### 4.5 Get combined downloads (any authenticated user)

```powershell
Invoke-RestMethod -Uri "http://localhost:8096/ArrIntegration/Downloads" -Headers $headers -Method Get
# Returns array of queue items from Radarr and Sonarr
```

### 4.6 Search movies (Radarr lookup)

```powershell
Invoke-RestMethod -Uri "http://localhost:8096/ArrIntegration/Radarr/Movies/Lookup?term=Inception" -Headers $headers -Method Get
```

### 4.7 Search series (Sonarr lookup)

```powershell
Invoke-RestMethod -Uri "http://localhost:8096/ArrIntegration/Sonarr/Series/Lookup?term=Breaking%20Bad" -Headers $headers -Method Get
```

### 4.8 Add a movie (admin or authenticated user)

1. Get a movie from lookup (e.g. step 4.6).
2. Take one of the returned movie objects and POST it:

```powershell
# Example: add first result and trigger search
$movies = Invoke-RestMethod -Uri "http://localhost:8096/ArrIntegration/Radarr/Movies/Lookup?term=Inception" -Headers $headers -Method Get
$movieToAdd = $movies[0]
Invoke-RestMethod -Uri "http://localhost:8096/ArrIntegration/Radarr/Movies?searchForMovie=true" -Headers $headers -Method Post -Body ($movieToAdd | ConvertTo-Json -Depth 10)
```

### 4.9 Add a series (admin or authenticated user)

Same idea: get series from Sonarr lookup, then POST one to add (and optionally search for episodes):

```powershell
$series = Invoke-RestMethod -Uri "http://localhost:8096/ArrIntegration/Sonarr/Series/Lookup?term=Breaking%20Bad" -Headers $headers -Method Get
$seriesToAdd = $series[0]
Invoke-RestMethod -Uri "http://localhost:8096/ArrIntegration/Sonarr/Series?searchForMissingEpisodes=true" -Headers $headers -Method Post -Body ($seriesToAdd | ConvertTo-Json -Depth 10)
```

## 5. Swagger UI (interactive)

With the server running, open:

**http://localhost:8096/api-docs/swagger**

(Or **http://localhost:8096/api-docs/swagger/index.html** if your browser needs the file explicitly.)

Find the **ArrIntegration** group and try the endpoints there. Click **Authorize**, paste your Jellyfin API key (the token string only), and confirm. The UI sends it in the `X-Emby-Token` header, which the server accepts for all API requests.

Alternative API docs (ReDoc): **http://localhost:8096/api-docs/redoc**

## 6. Quick checklist

| Step | Action | Expected |
|------|--------|----------|
| 1 | Run server | Server listens on 8096 |
| 2 | Get Jellyfin API key | Token string |
| 3 | GET `/ArrIntegration/Settings` | 200, JSON (may be empty defaults) |
| 4 | POST `/ArrIntegration/Settings` with Radarr/Sonarr URLs and keys | 204 |
| 5 | POST `/ArrIntegration/Radarr/Test` | 200, `{ "success": true }` if Radarr is reachable |
| 6 | POST `/ArrIntegration/Sonarr/Test` | 200, `{ "success": true }` if Sonarr is reachable |
| 7 | GET `/ArrIntegration/Downloads` | 200, array (may be empty) |
| 8 | GET `/ArrIntegration/Radarr/Movies/Lookup?term=...` | 200, array of movies |
| 9 | GET `/ArrIntegration/Sonarr/Series/Lookup?term=...` | 200, array of series |

## 7. Troubleshooting

- **401 Unauthorized** – Use a valid Jellyfin API key in the `X-Emby-Token` header.
- **403 Forbidden** – Settings and Test endpoints require an **admin** user; use an admin’s API key.
- **Radarr/Sonarr test returns `success: false`** – Check base URL (no trailing slash), API key, and that Radarr/Sonarr are running and reachable from the machine running Jellyfin (firewall, Docker network, etc.).
- **Add movie/series fails** – Ensure Radarr/Sonarr have at least one root folder and one quality profile; the integration uses the first of each if the request doesn’t specify them.
