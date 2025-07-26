# Build Processor Payment
Set-Location payment-processor-infra
docker compose down -v
docker compose up -d

# Buld Back-end
Set-Location ..
dotnet build
docker compose down -v
docker compose build --no-cache
docker compose up -d

# Realizar os teste
Set-Location .\rinha-test
k6 run -e MAX_REQUESTS=550 .\rinha.js

Set-Location ..\