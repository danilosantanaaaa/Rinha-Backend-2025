# 🐔 Danilo Santana - Rinha 2025

## Resumo

Desafio de backend disponível neste repositório: <https://github.com/zanfranceschi/rinha-de-backend-2025>.
Coloca em prática o processamento de pagamentos com recursos limitados, lidando com uma grande carga de requisições mesmo com pouca infraestrutura disponível, focando na criação de um backend resiliente.

Foi estabelecido restrinção de uso de CPU e Memória em 1,5 unidades de CPU e 350MB de memória entre todos os serviços declarados. As limitações de hardware mais detalhado para cada serviço está no [docker-compose.yml](./docker-compose.yml) e em cada serviço tem o `deploy.resources.limits`.

Veja as inscruções da rinha nesse link: <https://github.com/zanfranceschi/rinha-de-backend-2025/blob/main/INSTRUCOES.md>.

Minha contribuição está nesse diretório com todas as informações de testes realizado pela rinha: <https://github.com/zanfranceschi/rinha-de-backend-2025/tree/main/participantes/danilosantana-dotnet>.

## System Design

Um esquema basico de como irá funcionar a API.

![Systen Design](/assets/SystemDesign.svg)

## Especificação técnica

- .NET 9
- PostgreSQL
- Nginx
- Redis
- Docker
- Docker Compose

<h2 id="resultado-parcial">Resultado Parcial</h2>

Aqui é o resultado feito com os testes simplificado da rinha, com tempo de duração de 1 minuto e com máximo de 550 requests. Na seção logo abaixo terá o [resultado final](#resultado-final) .

```json
{
  "participante": "danilosantana-dotnet",
  "total_liquido": 357055.20275,
  "total_bruto": 333185.7,
  "total_taxas": 22702.915,
  "descricao": "'total_liquido' é sua pontuação final. Equivale ao seu lucro. Fórmula: total_liquido + (total_liquido * p99.bonus) - (total_liquido * multa.porcentagem)",
  "p99": {
    "valor": "3.66ms",
    "bonus": "15%",
    "max_requests": "550",
    "descricao": "Fórmula para o bônus: max((11 - p99.valor) * 0.02, 0)"
  },
  "multa": {
    "porcentagem": 0,
    "total": 0,
    "composicao": {
      "num_inconsistencias": 0,
      "descricao": "Se 'num_inconsistencias' > 0, há multa de 35%."
    }
  },
  "caixa_dois": {
    "detectado": false,
    "descricao": "Se 'lag' for negativo, significa que seu backend registrou mais pagamentos do que solicitado, automaticamente desclassificando sua submissão!"
  },
  "lag": {
    "num_pagamentos_total": 16743,
    "num_pagamentos_solicitados": 16743,
    "lag": 0,
    "descricao": "Lag é a diferença entre a quantidade de solicitações de pagamentos e o que foi realmente computado pelo backend. Mostra a perda de pagamentos possivelmente por estarem enfileirados."
  },
  "pagamentos_solicitados": {
    "qtd_sucesso": 16743,
    "qtd_falha": 0,
    "descricao": "'qtd_sucesso' foram requests bem sucedidos para 'POST /payments' e 'qtd_falha' os requests com erro."
  },
  "pagamentos_realizados_default": {
    "total_bruto": 272749.4,
    "num_pagamentos": 13706,
    "total_taxas": 13637.47,
    "descricao": "Informações do backend sobre solicitações de pagamento para o Payment Processor Default."
  },
  "pagamentos_realizados_fallback": {
    "total_bruto": 60436.3,
    "num_pagamentos": 3037,
    "total_taxas": 9065.445,
    "descricao": "Informações do backend sobre solicitações de pagamento para o Payment Processor Fallback."
  }
}
```
Fonte: <https://github.com/zanfranceschi/rinha-de-backend-2025/blob/main/participantes/danilosantana-dotnet/partial-results.json>

<h2 id="resultado-final">Resultado Final</h2>

Aqui é o resultado feito com os testes final da rinha, com tempo de duração de 6 minutos aproximadamente e com máximo de 603 requests.

```json
{
  "timestamp": "2025-08-21T06:20:32.355Z",
  "participante": "danilosantana-dotnet",
  "total_liquido": 1922071.024575,
  "total_bruto": 1816570.1,
  "total_taxas": 173774.3525,
  "descricao": "'total_liquido' é sua pontuação final. Equivale ao seu lucro. Fórmula: total_liquido + (total_liquido * p99.bonus) - (total_liquido * multa.porcentagem)",
  "p99": {
    "valor": "2.53ms",
    "bonus": "17%",
    "max_requests": "603",
    "descricao": "Fórmula para o bônus: max((11 - p99.valor) * 0.02, 0)"
  },
  "multa": {
    "porcentagem": 0,
    "total": 0,
    "composicao": {
      "num_inconsistencias": 0,
      "descricao": "Se 'num_inconsistencias' > 0, há multa de 35%."
    }
  },
  "caixa_dois": {
    "detectado": false,
    "descricao": "Se 'lag' for negativo, significa que seu backend registrou mais pagamentos do que solicitado, automaticamente desclassificando sua submissão!"
  },
  "lag": {
    "num_pagamentos_total": 90602,
    "num_pagamentos_solicitados": 90602,
    "lag": 0,
    "descricao": "Lag é a diferença entre a quantidade de solicitações de pagamentos e o que foi realmente computado pelo backend. Mostra a perda de pagamentos possivelmente por estarem enfileirados."
  },
  "pagamentos_solicitados": {
    "qtd_sucesso": 90602,
    "qtd_falha": 0,
    "descricao": "'qtd_sucesso' foram requests bem sucedidos para 'POST /payments' e 'qtd_falha' os requests com erro."
  },
  "pagamentos_realizados_default": {
    "total_bruto": 262755.25,
    "num_pagamentos": 13105,
    "total_taxas": 18392.8675,
    "descricao": "Informações do backend sobre solicitações de pagamento para o Payment Processor Default."
  },
  "pagamentos_realizados_fallback": {
    "total_bruto": 1553814.85,
    "num_pagamentos": 77497,
    "total_taxas": 155381.485,
    "descricao": "Informações do backend sobre solicitações de pagamento para o Payment Processor Fallback."
  }
}
```
Fonte: <https://github.com/zanfranceschi/rinha-de-backend-2025/blob/main/participantes/danilosantana-dotnet/final-results.json>
