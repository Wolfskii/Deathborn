Write-Host "Waiting for PostgreSQL..."
for ($i = 1; $i -le 30; $i++) {
    $null = docker compose --profile local exec -T db pg_isready -U deathborn -d deathborn 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "PostgreSQL is ready."
        exit 0
    }
    Start-Sleep -Seconds 1
}
Write-Host "PostgreSQL did not become ready in time. Is Docker running?"
exit 1
