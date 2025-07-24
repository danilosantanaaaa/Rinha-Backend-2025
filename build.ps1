# Build Processor Payment
cd payment-processor-infra
docker compose down -v
docker compose up -d

# Buld Back-end
cd ..
dotnet build
docker compose build --no-cache
docker compose down -v
docker compose up -d

# Realizar os teste
cd .\rinha-test
k6 run -e MAX_REQUESTS=550 .\rinha.js