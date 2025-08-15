#!/usr/bin/env bash

# Script básico para automatizar testes
# esse código foi baseado no repositorio principal da rinha com alguma modificações para atender minha necessidade.
# créditos: https://github.com/zanfranceschi/rinha-de-backend-2025/blob/main/rinha-test/run-tests.sh

dockerlogs=docker-compose.logs
param=${1:-"none"}

stopContainers() {
    # Derruba o payment process
    pushd ./tests/payment-processor > /dev/null
        docker compose down -v
    popd > /dev/null

    # Derruba o back-end
    docker compose down -v

    # Destruir redes e volumes para evitar problema
    docker network prune --force
    docker volume prune  --force
}

startContainers(){

    # Apaga o arquivo de log caso não for o modo "logs"
    if [ "$param" != "logs" ] && [ -f $dockerlogs ]; then
        rm -f $dockerlogs
    fi

    # Build Payment Processor
    pushd ./tests/payment-processor > /dev/null
        docker compose up -d 

    # Build Back-End
    popd > /dev/null

    if ! dotnet build; then
        echo "dotnet build error, build again."
        exit 1
    fi

    docker compose up  -d --build
    if [ "$param" = "logs" ]; then
        echo "Stdout in $dockerlogs file."
        echo "" > $dockerlogs
        docker compose down
        nohup docker compose up >> $dockerlogs &
    fi
}   

MAX_REQUESTS=550
testar() {
    # Realizar os teste
    pushd ./tests/rinha-test > /dev/null
    
    if [ "$param" = "dash" ]; then
        export K6_WEB_DASHBOARD=true
        export K6_WEB_DASHBOARD_PORT=5665
    fi
    
    k6 run -e MAX_REQUESTS=$MAX_REQUESTS ./rinha.js

    popd > /dev/null
}

stopContainers
startContainers

# Verificando se o servidor subiu
success=1
max_attempts=10
attempt=1
while [ $success -ne 0 ] && [ $max_attempts -ge $attempt ]; do
    curl -f -s http://localhost:9999/payments-summary
    success=$?
    echo "tried $attempt out of $max_attempts..."
    sleep 5
    ((attempt++))
done

if [ $success -eq 0 ]; then
    testar
    stopContainers

    if [ "$param" = "logs" ]; then
        # Trucando em 1000 linhas
        sed -i '1001,$d' $dockerlogs
    fi

else
    echo "[$(date)] Seu backend não respondeu nenhuma das $max_attempts tentativas de GET para http://localhost:9999/payments-summary. Teste abortado."
    echo "Could not get a successful response from backend... aborting test for $participant"
    exit
fi