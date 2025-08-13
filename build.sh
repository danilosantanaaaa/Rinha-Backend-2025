# Script básico para automatizar testes

# Derrubar todos os serviços
docker compose -f ./payment-processor/docker-compose.yml  down -v ## Derruba o processor
docker compose down -v # Derruba o backend

# Destruir todas networks e volumes
docker network prune -f
docker volume prune -f

# Build Payment Processor
docker compose -f ./payment-processor/docker-compose.yml up -d

# Build Back-End
dotnet build
docker compose up -d --build

# Verificando se o servidor subiu
success=1
max_attempts=15
attempt=1
while [ $success -ne 0 ] && [ $max_attempts -ge $attempt ]; do
    curl -f -s http://localhost:9999/payments-summary
    success=$?
    echo "tried $attempt out of $max_attempts..."
    sleep 5
    ((attempt++))
done

if [ $success -eq 0 ]; then
    # Realizar os teste
    cd ./rinha-test
    k6 run -e MAX_REQUESTS=550 ./rinha.js

    cd ../
else
    echo "[$(date)] Seu backend não respondeu nenhuma das $max_attempts tentativas de GET para http://localhost:9999/payments-summary. Teste abortado."
    echo "Could not get a successful response from backend... aborting test for $participant"
    exit
fi