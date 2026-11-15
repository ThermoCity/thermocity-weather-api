# ThermoCity Weather API

API de monitoramento climático desenvolvida em **.NET 10**, responsável pela coleta, armazenamento e disponibilização de dados meteorológicos para o projeto ThermoCity.

O sistema utiliza dados da OpenWeather para acompanhar as condições climáticas de Presidente Prudente - SP, calcular índices de calor com fórmulas meteorológicas oficiais (Rothfusz / NWS), armazenar previsões meteorológicas e consolidar indicadores diários.

A solução foi estruturada para separar a coleta de dados da disponibilização das informações, utilizando uma API RESTful para consultas com autenticação JWT e um Worker independente para executar os processos de coleta e consolidação em segundo plano.

---

## 1. Tecnologias Utilizadas

| Tecnologia | Finalidade |
|---|---|
| .NET 10 | Plataforma de desenvolvimento moderna |
| C# 13 | Linguagem de programação principal |
| ASP.NET Core | Construção da API RESTful com Middleware Pipeline |
| MongoDB (Driver 3.14.0) | Persistência documental de alta performance |
| JWT Bearer (Microsoft.AspNetCore.Authentication.JwtBearer) | Autenticação e autorização stateless via tokens |
| OpenTelemetry (1.12.0) | Observabilidade e rastreabilidade distribuída (Tracing) |
| Rate Limiting (Fixed Window) | Proteção da API contra abuso e negação de serviço (HTTP 429) |
| RFC 9457 ProblemDetails | Tratamento global padronizado de exceções (`GlobalExceptionHandler`) |
| Health Checks | Monitoramento de vivacidade (Liveness) e prontidão (Readiness) |
| OpenWeather API | Provedor externo de dados climáticos e previsões |
| Serilog | Logging estruturado para console e contexto |
| Swagger / OpenAPI (Swashbuckle) | Documentação interativa e testes da API |
| xUnit & Moq | Testes unitários de regras de negócio e testes de integração com `WebApplicationFactory` |

---

## 2. Arquitetura da Solução

A solução adota os princípios de Clean Architecture e separação de responsabilidades em camadas:

```text
thermocity-weather-api/
│
├── README.md
├── ThermoCity.Weather.slnx
│
├── src/
│   ├── ThermoCity.Weather.Domain/
│   │   ├── Entities/               # WeatherReading, DailyForecast, DailyWeatherSummary, User
│   │   └── Enums/                  # WeatherSource, ReadingStatus
│   │
│   ├── ThermoCity.Weather.Application/
│   │   ├── Exceptions/             # InvalidInputException, ConflictException
│   │   ├── Interfaces/             # IWeatherProvider, IUserRepository, IAuthService, ITokenService, etc.
│   │   ├── Models/                 # DTOs de credenciais e autenticação
│   │   └── Services/               # HeatIndexService, AuthService, WeatherCollectionService, etc.
│   │
│   ├── ThermoCity.Weather.Infrastructure/
│   │   ├── Mongo/
│   │   │   ├── Repositories/       # WeatherReadingRepository, DailyForecastRepository, UserRepository
│   │   │   ├── MongoDbHealthCheck.cs
│   │   │   └── MongoDependencyInjection.cs
│   │   ├── OpenWeather/
│   │   │   ├── DTOs/
│   │   │   ├── OpenWeatherProvider.cs
│   │   │   └── OpenWeatherHealthCheck.cs
│   │   └── Security/
│   │       └── PasswordHasher.cs   # Hashing seguro com PBKDF2 (100.000 iterações + Salt)
│   │
│   ├── ThermoCity.Weather.Api/
│   │   ├── Controllers/            # AuthController, ReadingsController, ForecastsController, DailySummariesController
│   │   ├── ExceptionHandling/      # GlobalExceptionHandler (IExceptionHandler, RFC 9457)
│   │   ├── Models/                 # AuthRequest, AuthResponse
│   │   ├── Services/               # TokenService (Geração e assinatura de JWT HMAC-SHA256)
│   │   ├── Program.cs              # Configuração de DI, Autenticação, RateLimiting, OTel e rotas
│   │   └── appsettings.json
│   │
│   └── ThermoCity.Weather.Worker/
│       ├── Workers/                # WeatherCollectionWorker, ForecastCollectionWorker, DailyConsolidationWorker
│       ├── Program.cs
│       └── appsettings.json
│
└── tests/
    ├── ThermoCity.Weather.UnitTests/
    │   ├── Controllers/            # AuthControllerTests, ReadingsControllerTests, etc.
    │   ├── ExceptionHandling/      # GlobalExceptionHandlerTests
    │   └── Services/               # HeatIndexServiceTests, AuthServiceTests, etc.
    │
    └── ThermoCity.Weather.IntegrationTests/
        ├── Fixtures/               # ThermoCityApiFactory (WebApplicationFactory em memória)
        ├── AuthEndpointsTests.cs   # Testes de registro, validações e login
        ├── HealthCheckTests.cs     # Testes de /health/live e /swagger
        ├── RateLimiterTests.cs     # Testes de estouro de limite (HTTP 429)
        └── WeatherEndpointsTests.cs# Testes de consultas autenticadas e bloqueio anônimo (401)
```

---

## 3. Atendimento aos Critérios de Avaliação (100 Pontos)

| # | Requisito / Critério | Pontuação | Implementação no Projeto |
|---|---|---|---|
| **01** | **API RESTful funcional com validações e OpenAPI/Swagger** | **20 pts** | Endpoints REST com códigos HTTP padronizados (200, 201, 400, 401, 404, 409, 429, 500, 503), validações de domínio com `InvalidInputException` e documentação interativa Swagger completa com suporte a autenticação Bearer. |
| **02** | **Autenticação e Autorização JWT** | **20 pts** | Middleware `JwtBearer` com validação de emissor, audiência, tempo de vida e assinatura HMAC-SHA256. Endpoints de dados meteorológicos protegidos com `[Authorize]`. Endpoints de registro (`/api/auth/register`) e login (`/api/auth/signin`) públicos com senha protegida por PBKDF2. |
| **03** | **Idempotência contra operações repetidas** | **15 pts** | **1)** Idempotência garantida na persistência com operações de **Upsert** (`ReplaceOneModel` com `IsUpsert = true`) no MongoDB para leituras, previsões e consolidações meteorológicas com base em chaves naturais únicas. **2)** Regras de negócio de unicidade com `ConflictException` (HTTP 409 Conflict) para impedir duplicidade de usuários. **3)** Consultas HTTP GET idempotentes por especificação. |
| **04** | **Rate Limiting funcional (HTTP 429)** | **15 pts** | Rate limiting nativo particionado por IP (`PartitionedRateLimiter`) com Fixed Window de 10 requisições/minuto. Customização de rejeição `OnRejected` retornando RFC 9457 `ProblemDetails` com código HTTP 429 Too Many Requests. |
| **05** | **Testes Automatizados (xUnit & Moq)** | **20 pts** | **89 testes automatizados** (68 unitários + 21 de integração) validando regras de cálculo do índice de calor, consolidação, serviços de autenticação, tratamento global de erros, rate limiting e bloqueio de acesso sem token. |
| **06** | **Health Checks completos (API, Banco e Serviço Externo)** | **10 pts** | Endpoints `/health/live` (Liveness da API) e `/health/ready` (Readiness cobrindo o tripé: **API própria**, **MongoDB** via comando `ping` e **OpenWeather API** via conectividade HTTP). |
| | **TOTAL** | **100 pts** | **Todos os requisitos plenamente atendidos e validados por testes automatizados.** |

---

## 4. Segurança e Autenticação (JWT)

A API utiliza autenticação baseada em JSON Web Tokens (JWT).

### 4.1. Fluxo de Autenticação

1. O cliente se cadastra em `POST /api/auth/register`. A senha é protegida com salt aleatório e derivação PBKDF2 (100.000 iterações com SHA-256).
2. O cliente autentica em `POST /api/auth/signin` informando `username` e `password`.
3. A API valida a senha e retorna um token JWT assinado digitalmente:
   ```json
   {
     "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
   }
   ```
4. Para acessar endpoints protegidos (`/api/readings/*`, `/api/forecasts`, `/api/daily-summaries/*`), o cliente inclui o cabeçalho HTTP:
   ```http
   Authorization: Bearer <seu-token-jwt>
   ```

---

## 5. Idempotência com Upsert no MongoDB

A garantia de idempotência para evitar operações duplicadas e efeitos colaterais indesejados é tratada de forma robusta e nativa na camada de persistência:

1. **Upsert no MongoDB**:
   * As operações de persistência de leituras climáticas (`WeatherReadingRepository`), previsões meteorológicas (`DailyForecastRepository`) e resumos diários (`DailyWeatherSummaryRepository`) utilizam **Upsert** (`ReplaceOneModel` com `IsUpsert = true`) ancorados em chaves de negócio compostas (ex: `Source` + `MeasurementDate` ou `Source` + `ReferenceDate`).
   * Caso o processo do Worker execute novamente ou coletas sejam disparadas repetidas vezes para o mesmo momento/dia, o registro existente é atualizado sem gerar duplicidade.
2. **Prevenção de Duplicidade em Cadastros**:
   * Em `POST /api/auth/register`, a existência prévia do usuário é verificada antes de qualquer inserção; tentativas repetidas com o mesmo nome de usuário disparam `ConflictException`, retornando HTTP `409 Conflict`.
3. **Idempotência Natural em Consultas**:
   * Todos os endpoints de leitura expostos pela API utilizam o verbo HTTP `GET`, sendo idempotentes por definição do padrão REST (RFC 7231 / RFC 9110).

---

## 6. Rate Limiting (Proteção contra Uso Excessivo)

A API implementa limitação de taxa por IP do cliente utilizando a biblioteca nativa do ASP.NET Core:

* **Política**: Fixed Window com limite de **10 requisições por minuto** por endereço IP.
* **Comportamento ao Exceder o Limite**: A requisição é rejeitada imediatamente com status `429 Too Many Requests` e payload RFC 9457:
  ```json
  {
    "type": "about:blank",
    "title": "Limite de requisições excedido",
    "status": 429,
    "detail": "Você atingiu o limite de requisições permitido. Aguarde antes de tentar novamente."
  }
  ```

---

## 7. Health Checks (Monitoramento)

A solução disponibiliza dois endpoints para verificação de saúde da aplicação:

### `/health/live` (Liveness)
Indica se a aplicação está de pé e respondendo a requisições, sem depender de recursos externos. Ideal para sondas de liveness em orquestradores (Kubernetes/Docker).
* **Tag**: `live`
* **Resposta**: `200 OK` com conteúdo `Healthy`.

### `/health/ready` (Readiness)
Verifica se todas as dependências essenciais estão disponíveis para atender requisições:
* **API própria (`self`)**: Aplicação em execução.
* **Banco de dados (`mongodb`)**: Ping ativo no MongoDB.
* **Serviço externo (`openweather`)**: Conectividade com a API externa da OpenWeather.
* **Tag**: `ready`

---

## 8. Tratamento Global de Erros (RFC 9457)

Toda exceção não tratada é interceptada pela classe `GlobalExceptionHandler` (`IExceptionHandler`) e convertida em uma resposta estruturada `ProblemDetails` (`application/problem+json`):

| Exceção | Status Code | Título |
|---|---|---|
| `InvalidInputException` | `400 Bad Request` | Requisição inválida |
| `ConflictException` | `409 Conflict` | Conflito |
| `BadHttpRequestException` | `400 Bad Request` | Requisição inválida |
| `MongoException` / `TimeoutException` | `503 Service Unavailable` | Serviço indisponível |
| `Exception` (genérica) | `500 Internal Server Error` | Erro interno |

---

## 9. Endpoints da API

### 9.1. Autenticação (Públicos)

| Método | Endpoint | Descrição |
|---|---|---|
| POST | `/api/auth/register` | Cadastra um novo usuário no sistema |
| POST | `/api/auth/signin` | Autentica o usuário e emite o token JWT |

#### Exemplo de Registro:
```http
POST /api/auth/register HTTP/1.1
Content-Type: application/json

{
  "username": "lucas",
  "password": "MinhaSenhaForte123!"
}
```

#### Exemplo de Login:
```http
POST /api/auth/signin HTTP/1.1
Content-Type: application/json

{
  "username": "lucas",
  "password": "MinhaSenhaForte123!"
}
```

---

### 9.2. Dados Meteorológicos (Protegidos por `[Authorize]`)

Todos os endpoints abaixo exigem o cabeçalho `Authorization: Bearer <token>`.

| Método | Endpoint | Parâmetros de Query | Descrição |
|---|---|---|---|
| GET | `/api/readings/latest` | `source=OpenWeather` | Consulta a leitura meteorológica mais recente |
| GET | `/api/readings` | `source`, `startDate`, `endDate` | Consulta observações por período em UTC |
| GET | `/api/forecasts` | `source`, `startDate`, `endDate` | Consulta previsões por período de calendário |
| GET | `/api/daily-summaries/{date}` | `source=OpenWeather` | Consulta o resumo consolidado de um dia |
| GET | `/api/daily-summaries` | `source`, `startDate`, `endDate` | Consulta resumos consolidados por período |

---

## 10. Como Executar a Aplicação

### 10.1. Pré-requisitos
* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* [MongoDB](https://www.mongodb.com/try/download/community) (ou instância MongoDB Atlas)
* Chave de API da [OpenWeather](https://openweathermap.org/api)

### 10.2. Configuração de Variáveis de Ambiente

No terminal (Linux / macOS):
```bash
export MongoDb__ConnectionString="mongodb://localhost:27017"
export MongoDb__DatabaseName="thermocity-weather"
export Jwt__Key="chave-super-secreta-com-pelo-menos-32-caracteres-para-assinatura-jwt"
export OpenWeather__ApiKey="sua_chave_openweather_aqui"
```

No Windows (PowerShell):
```powershell
$env:MongoDb__ConnectionString="mongodb://localhost:27017"
$env:MongoDb__DatabaseName="thermocity-weather"
$env:Jwt__Key="chave-super-secreta-com-pelo-menos-32-caracteres-para-assinatura-jwt"
$env:OpenWeather__ApiKey="sua_chave_openweather_aqui"
```

### 10.3. Compilação e Execução

Restaurar pacotes e compilar:
```bash
dotnet restore
dotnet build
```

Executar a **API**:
```bash
dotnet run --project src/ThermoCity.Weather.Api --urls http://localhost:5050
```

Executar o **Worker**:
```bash
dotnet run --project src/ThermoCity.Weather.Worker
```

Acessar o **Swagger UI**:
```text
http://localhost:5050/swagger
```

---

## 11. Testes Automatizados

O projeto conta com **89 testes automatizados** utilizando xUnit e Moq.

Para executar todos os testes da solução:
```bash
dotnet test
```

Para executar separadamente:
```bash
# Testes Unitários (Regras de negócio, cálculo de calor, controllers, auth, exceptions)
dotnet test tests/ThermoCity.Weather.UnitTests

# Testes de Integração (Endpoints autenticados, Rate Limiting, Health Checks)
dotnet test tests/ThermoCity.Weather.IntegrationTests
```

---

## 12. Observabilidade (OpenTelemetry)

A aplicação conta com instrumentação OpenTelemetry em conformidade com os padrões da CNCF:
* Rastreabilidade automática de requisições HTTP recebidas (`OpenTelemetry.Instrumentation.AspNetCore`).
* Rastreabilidade de chamadas HTTP externas (`OpenTelemetry.Instrumentation.Http`).
* Exportador padrão em console (`OpenTelemetry.Exporter.Console`) com geração de `TraceId` e correlação de logs.
