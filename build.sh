# Script básico para automatizar testes

# Derrubar todos os serviços
docker compose -f ./payment-processor/docker-compose.yml  down -v ## Derruba o processor
docker compose down -v # Derruba o backend

# Destruir todas networks e volumas
docker network prune -f
docker volume prune -f

# Build Payment Processor
docker compose -f ./payment-processor/docker-compose.yml up -d

# Build Back-End
dotnet build
docker compose build --no-cache
docker compose up -d

# Realizar os teste
cd ./rinha-test
k6 run -e MAX_REQUESTS=550 ./rinha.js

cd ../