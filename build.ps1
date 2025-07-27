# Script básico para automatizar testes

# Derrubar todos os serviços
Set-Location payment-processor-infra
docker compose down -v ## Derruba o processor
Set-Location ..\
docker compose down -v # Derruba o backend

# Destruir todas networks e volumas
docker network prune -f
docker volume prune -f

# Build Processor Payment
Set-Location payment-processor-infra
docker compose up -d

# Buld Back-end
Set-Location ..

dotnet build
docker compose build --no-cache
docker compose up -d

# Realizar os teste
Set-Location .\rinha-test
k6 run -e MAX_REQUESTS=550 .\rinha.js

Set-Location ..\