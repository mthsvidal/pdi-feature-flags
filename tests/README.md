# Load Tests com k6

Testes de carga e validação do endpoint de processamento de contratos usando k6.

## Pré-requisitos

1. **k6 instalado**: [Download k6](https://k6.io/docs/getting-started/installation/)

2. **API rodando**: Certifique-se que a API está rodando em `http://localhost:5000`

```bash
cd src/PdiContracts.Api
dotnet run
```

3. **Redis e Flipt rodando**: Execute o docker-compose

```bash
docker-compose up -d
```

## Como executar os testes

### Teste básico de validação

```bash
cd tests
k6 run load-test.js
```

### Teste com configuração customizada

Para aumentar a carga ou iterações, use a flag `--vus` (virtual users) e `--iterations`:

```bash
k6 run --vus 5 --iterations 10 load-test.js
```

### Teste com duração (ao invés de iterações)

```bash
k6 run --vus 3 --duration 30s load-test.js
```

## O que o teste valida

1. **Resposta da API**:
   - Status HTTP 202 (Accepted)
   - Presença de `processingDetails` e `summary`
   - Campos corretos no summary

2. **Persistência de arquivos**:
   - Cada asset holder notificado gera um arquivo em `C:\temp`
   - Estrutura: `C:\temp\<asset_holder>\<idempotency_key>\<contract_reference>.json`

3. **Conteúdo dos arquivos**:
   - Arquivo deve ser JSON válido
   - Deve conter o `IdempotencyKey` correto
   - Deve conter o `OriginalAssetHolder` correto
   - Deve conter `ContractSpecifications` (array não vazio)

## Output esperado

```
=== Contract Processing Summary ===
IdempotencyKey: k6-test-abc123-1711270000000
Total Specifications: 9
Total Notified: 9
Asset Holders: 1

--- Asset Holder 1: 16501555000157 ---
Notification Path: C:\temp\16501555000157\k6-test-abc123-1711270000000\CG_15.json
Count: 9
✓ File found: C:\temp\16501555000157\k6-test-abc123-1711270000000\CG_15.json
  - Specifications in file: 9

=== Summary ===
Paths Found: 1/1
Paths Not Found: 0/1
```

## Estrutura do arquivo de teste

- `open(CONTRACT_FILE)`: Lê o arquivo `contract.json` que será usado no payload
- `idempotencyKey`: Gerado dinamicamente para cada execução
- `check()`: Valida respostas da API e conteúdo dos arquivos
- `open(filePath)`: Verifica se arquivo foi criado e lê seu conteúdo

## Troubleshooting

### API não respondendo
```
Error: Failed to connect to http://localhost:5000
```
Certifique-se que a API está rodando com `dotnet run`

### Arquivos não encontrados
```
✗ File NOT found: C:\temp\...
```
Verifique se o path `C:\temp` existe e tem permissão de escrita

### Redis conexão recusada
Certifique-se que Redis está rodando:
```bash
docker-compose up -d redis
```

## Relatório de teste

k6 gera automaticamente um resumo ao fim da execução com:
- Número de requisições
- Taxa de erro
- Duração média
- P95, P99 das requisições
