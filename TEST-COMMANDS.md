# Comandos de Teste - Temporal POC API

## 🚀 Como Testar

### 1. Iniciar o ambiente

```bash
docker-compose up -d
```

### 2. Verificar status dos containers

```bash
docker-compose ps
```

---

## 📋 Testes da API Principal (`Temporal.POC.api`)

### Criar uma transação (sem Nexus)

```bash
curl -X POST http://localhost:5000/v1/transaction \
  -H "Content-Type: application/json" \
  -d '{
    "profileId": 1010,
    "externalOperationId": "OP-TEST-001",
    "operationType": "RadiusMailOrder",
    "appCallerNm": "test-app",
    "webhookCallBackUrl": "http://temporal-poc-api:8080/v1/webhook",
    "movements": [
      {
        "order": 1,
        "subOrder": 1,
        "operationUuid": "UUID-001",
        "transactionUuid": "TXN-UUID-001",
        "transactionDestination": "Stripe",
        "transactionType": "ChargeStripeAccount",
        "externalId": "RadiusMailOrderId - 10000",
        "operationDt": "2024-01-01 10:00:00.000",
        "operationTotalVl": 79.75,
        "stripeFields": {
          "partnerProfileId": "XXXX",
          "extrasFields1": "ExtrasFields2"
        },
        "lines": [
          {
            "lineType": "Integration",
            "lineVl": 79.75,
            "uniqueId": ""
          }
        ]
      },
      {
        "order": 2,
        "subOrder": 1,
        "operationUuid": "UUID-001",
        "transactionUuid": "TXN-UUID-001",
        "transactionDestination": "Balance",
        "transactionType": "CreditStripeFunds",
        "externalId": "RadiusMailOrderId - 10000",
        "operationDt": "2024-01-01 10:00:00.000",
        "operationTotalVl": 79.75,
        "balanceFields": {
          "profileId": 1010
        }
      },
      {
        "order": 2,
        "subOrder": 2,
        "operationUuid": "UUID-001",
        "transactionUuid": "TXN-UUID-001",
        "transactionDestination": "Balance",
        "transactionType": "ChargeStripeAccount",
        "externalId": "RadiusMailOrderId - 10000",
        "operationDt": "2024-01-01 10:00:00.000",
        "operationTotalVl": -79.75,
        "balanceFields": {
          "profileId": 1010
        },
        "lines": [
          {
            "lineType": "LargeHandwrittenCardA8",
            "lineVl": -70.75,
            "uniqueId": ""
          },
          {
            "lineType": "FirstClassPostage",
            "lineVl": -6.25,
            "uniqueId": ""
          },
          {
            "lineType": "RecipientData",
            "lineVl": -2.75,
            "uniqueId": ""
          }
        ]
      }
    ]
  }'
```

### Enviar sinal Stripe para o workflow

#### ✅ Sinal de SUCESSO

```bash
curl -X POST http://localhost:5000/v1/stripe-signal \
  -H "Content-Type: application/json" \
  -d '{
    "ExternalOperationId": "OP-TEST-001",
    "OperationType": "RadiusMailOrder",
    "Success": true,
    "Message": "Stripe payment confirmed successfully"
  }'
```

#### ❌ Sinal de FALHA (dispara Rollback)

```bash
curl -X POST http://localhost:5000/v1/stripe-signal \
  -H "Content-Type: application/json" \
  -d '{
    "ExternalOperationId": "OP-TEST-001",
    "OperationType": "RadiusMailOrder",
    "Success": false,
    "Message": "Stripe payment failed"
  }'
```

**Nota**: O `ExternalOperationId` e `OperationType` devem corresponder ao workflow que você deseja sinalizar.

---

## 🔗 Testes via Nexus (`Temporal.POC.ExternalDomain.api`)

### Chamar TransactionWorkflow via Nexus

```bash
curl -X POST http://localhost:6000/v1/external-domain/transaction \
  -H "Content-Type: application/json" \
  -d '{
    "profileId": 1010,
    "externalOperationId": "OP-NEXUS-001",
    "operationType": "RadiusMailOrder"
  }'
```

**Resposta esperada:**

```json
{
  "workflowId": "external-domain-RadiusMailOrder-OP-NEXUS-001",
  "runId": "019af...",
  "message": "ExternalDomainWorkflow started successfully. It will create TransactionRequest internally and call TransactionWorkflow via Nexus.",
  "request": {
    "profileId": 1010,
    "externalOperationId": "OP-NEXUS-001",
    "operationType": "RadiusMailOrder"
  }
}
```

### Enviar sinal Stripe para workflow criado via Nexus

#### ✅ Sinal de SUCESSO para workflow Nexus

```bash
curl -X POST http://localhost:5000/v1/stripe-signal \
  -H "Content-Type: application/json" \
  -d '{
    "ExternalOperationId": "OP-NEXUS-001",
    "OperationType": "RadiusMailOrder",
    "Success": true,
    "Message": "Stripe payment confirmed successfully"
  }'
```

#### ❌ Sinal de FALHA para workflow Nexus (dispara Rollback)

```bash
curl -X POST http://localhost:5000/v1/stripe-signal \
  -H "Content-Type: application/json" \
  -d '{
    "ExternalOperationId": "OP-NEXUS-001",
    "OperationType": "RadiusMailOrder",
    "Success": false,
    "Message": "Stripe payment failed - triggering rollback"
  }'
```

---

## 📊 Verificar Logs

### Logs da API Principal

```bash
docker logs temporal-poc-api -f
```

### Logs da API Externa (Nexus)

```bash
docker logs temporal-poc-external-domain-api -f
```

### Logs do Temporal Server

```bash
docker logs temporal -f
```

### Verificar se Nexus completou com sucesso

```bash
docker logs temporal-poc-external-domain-api | grep "Transaction completed via Nexus"
```

---

## 🌐 Acessar a UI do Temporal

Abra no navegador:

```
http://localhost:8080
```

### Filtrar workflows:

- **ExternalDomainWorkflow**: `WorkflowType = "ExternalDomainWorkflow"`
- **TransactionWorkflow**: `WorkflowType = "TransactionWorkflow"`

---

## 🧪 Testes Rápidos

### Teste 1: Transaction simples (sem Nexus)

```bash
curl -X POST http://localhost:5000/v1/transaction \
  -H "Content-Type: application/json" \
  -d '{"profileId":1010,"externalOperationId":"OP-QUICK-001","operationType":"RadiusMailOrder","appCallerNm":"test","webhookCallBackUrl":"http://temporal-poc-api:8080/v1/webhook","movements":[{"order":1,"subOrder":1,"operationUuid":"UUID-Q-001","transactionUuid":"TXN-Q-001","transactionDestination":"Stripe","transactionType":"ChargeStripeAccount","externalId":"Test-001","operationDt":"2024-01-01 10:00:00.000","operationTotalVl":50.00,"stripeFields":{"partnerProfileId":"TEST"},"lines":[{"lineType":"Integration","lineVl":50.00}]}]}'
```

### Teste 2: Nexus (recomendado)

```bash
curl -X POST http://localhost:6000/v1/external-domain/transaction \
  -H "Content-Type: application/json" \
  -d '{"profileId":1010,"externalOperationId":"OP-NEXUS-QUICK-001","operationType":"RadiusMailOrder"}'
```

---

## 🔍 Verificar Nexus Endpoint

```bash
docker exec temporal temporal operator nexus endpoint list --address temporal:7233
```

---

## 🛑 Parar o ambiente

```bash
docker-compose down
```

### Limpar volumes (remover dados persistidos)

```bash
docker-compose down -v
```

