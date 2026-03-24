# PDI Contracts: Feature Flags com Flipt

Projeto demonstrando API de contratos em .NET com feature flags usando Flipt.

## Sobre

A API processa contratos e usa feature flags para:
- rollout gradual de funcionalidades
- kill switch
- validacao condicionada por contexto
- suporte a idempotencia no processamento

## Estrutura

- `src/PdiContracts.Api`: API REST
- `src/PdiContracts.Domain`: biblioteca de dominio e cliente de feature flags
- `docker-compose.yml`: stack local do Flipt com interface grafica
- `.env.example`: variaveis de ambiente de exemplo

## Quick Start

1. Criar variaveis de ambiente

```bash
cp .env.example .env
```

2. Subir Flipt local

```bash
docker-compose up -d
```

3. Criar namespace e feature flags no Flipt

- Acesse `http://localhost:8080`
- Use o namespace `default` (ou defina outro)
- Gere/defina token de API e coloque em `FLIPT_API_TOKEN` no `.env`

4. Rodar a API

```bash
cd src/PdiContracts.Api
dotnet run
```

Swagger: `http://localhost:5211`

## Testes com k6

Para executar os testes de carga e validação:

```bash
cd tests
k6 run load-test.js
```

## Configuracao da API

A API le as variaveis abaixo no startup (veja `.env.example`):

- `FLIPT_URL`: URL do servidor Flipt
- `FLIPT_API_TOKEN`: Token de autenticacao do Flipt
- `FLIPT_NAMESPACE_KEY`: Namespace das flags no Flipt
- `FLIPT_TIMEOUT_SECONDS`: Timeout para requisicoes ao Flipt
- `REDIS_CONNECTION_STRING`: Endereco do Redis para cache

## Exemplo de uso

No processamento, a API avalia a flag `process-contract-specifications` no Flipt para cada especificacao de contrato:

```csharp
var contextJson = JsonSerializer.Serialize(new
{
    entityId = $"{request.IdempotencyKey}|{contract.Reference}|{specification.OriginalAssetHolder}|{specification.PaymentScheme}",
    originalAssetHolder = specification.OriginalAssetHolder
});

var isEnabled = await _featureFlagService.IsEnabledAsync(
    FeatureFlagNames.ProcessContractSpecifications,
    contextJson,
    defaultValue: false
);

// Cache: chave gerada automaticamente a partir do JSON
// Exemplo: flipt:feature:ProcessContractSpecifications-originalAssetHolder_09015607000110
// TTL: 1 minuto
```

## Observacoes

- O cliente usa a API REST do Flipt via `POST /evaluate/v1/batch`.
- A autenticacao usa header `Authorization: Bearer <FLIPT_API_TOKEN>`.
- **Cache em Redis**: Chave de cache e construida a partir dos campos do contexto JSON (exceto `entityId`), permitindo reutilizacao entre requisicoes com mesmo contexto.
- O `entityId` deve ser estavel por CS para o split percentual permanecer deterministico.
- TTL do cache: 1 minuto

## Cadastro da flag no Flipt

Passo 1 - Criar a feature flag

- Va em Flags -> New Flag.
- Escolha o tipo Boolean (nao Variant), pois e o ideal para on/off com rollout gradual.
- Defina o name como `Feature Flag para Split de AssetHolder`.
- Defina a key como `process-contract-specifications`.
- Marque a flag como habilitada.

Passo 2 - Criar o segmento com constraint

- Va em Segments -> New Segment.
- Use match type: ANY. Isso significa que qualquer constraint verdadeiro ja inclui o usuario.
- Dentro do segmento, adicione um constraint:
    - Property: `originalAssetHolder`
    - Operator: `==`
    - Value: `16501555000157`
- Para adicionar outros CNPJs no futuro, basta adicionar mais constraints com o mesmo operador `==` no mesmo segmento.
- Como o match type e ANY, qualquer CNPJ que bater entra.

Passo 3 - Rollout por segmento (CNPJs especificos)

- Volte a sua flag e va em Rollouts -> New Rollout.
- Escolha o tipo Segment.
- Aponte para o segmento `cnpj-allowlist`.
- Defina o valor como `true`.
- Isso garante prioridade maxima: esses CNPJs sempre recebem a flag ativa.

Passo 4 - Rollout gradual por threshold

- Adicione um segundo rollout do tipo Threshold.
- Comece com 0%, valor `true`, Rank 2.
- Esse rollout se aplica a todos os usuarios fora do segmento.
- Conforme o rollout avanca, suba a porcentagem: 10% -> 25% -> 50% -> 100%, sem nenhum redeploy.
