# ThermoCity Weather API

API de monitoramento climático desenvolvida em **.NET 10**, responsável pela coleta, armazenamento e disponibilização de dados meteorológicos para o projeto ThermoCity.

O sistema utiliza dados da OpenWeather para acompanhar as condições climáticas de Presidente Prudente - SP, calcular índices de calor, armazenar previsões meteorológicas e consolidar indicadores diários.

A solução foi estruturada para separar a coleta de dados da disponibilização das informações, utilizando uma API REST para consultas e um Worker independente para executar os processos em segundo plano.

---

## 1. Tecnologias utilizadas

| Tecnologia | Finalidade |
|---|---|
| .NET 10 | Plataforma de desenvolvimento |
| C# | Linguagem principal |
| ASP.NET Core | Construção da API REST |
| BackgroundService | Execução de tarefas em segundo plano |
| MongoDB | Persistência documental |
| PostgreSQL | Persistência relacional |
| Entity Framework Core | Mapeamento objeto-relacional |
| Npgsql | Comunicação com PostgreSQL |
| MongoDB.Driver | Comunicação com MongoDB |
| OpenWeather API | Fonte de dados meteorológicos |
| Serilog | Logging estruturado |
| Swagger / Swashbuckle | Documentação e testes da API |
| xUnit | Testes automatizados |
| Moq | Simulação de dependências nos testes |

---

## 2. Arquitetura do projeto

A solução utiliza uma organização em camadas, separando as responsabilidades de domínio, aplicação, infraestrutura e exposição dos endpoints.

```text
thermocity-weather-api/
│
├── README.md
├── ThermoCity.Weather.slnx
│
├── src/
│   ├── ThermoCity.Weather.Api/
│   │   ├── Controllers/
│   │   ├── HealthChecks/
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   ├── ThermoCity.Weather.Application/
│   │   ├── Interfaces/
│   │   └── Services/
│   │
│   ├── ThermoCity.Weather.Domain/
│   │   ├── Entities/
│   │   └── Enums/
│   │
│   ├── ThermoCity.Weather.Infrastructure/
│   │   ├── Mongo/
│   │   ├── Repositories/
│   │   ├── OpenWeather/
│   │   └── PostgreSql/
│   │       ├── Configurations/
│   │       ├── Migrations/
│   │       └── Repositories/
│   │
│   └── ThermoCity.Weather.Worker/
│       ├── Workers/
│       ├── Program.cs
│       └── appsettings.json
│
└── tests/
    ├── ThermoCity.Weather.UnitTests/
    │   ├── Controllers/
    │   └── Services/
    │
    └── ThermoCity.Weather.IntegrationTests/
        ├── Fixtures/
        ├── HealthCheckTests.cs
        └── WeatherEndpointsTests.cs
```

### Responsabilidades das camadas

**Domain**
- Entidades e enums do sistema.
- Representação dos dados meteorológicos.
- Não depende das camadas externas.

**Application**
- Interfaces de serviços e repositórios.
- Regras de negócio e processamento dos dados.
- Cálculo do índice de calor.
- Coleta e consolidação das informações.

**Infrastructure**
- Integração com a OpenWeather.
- Persistência no MongoDB e PostgreSQL.
- Implementações concretas dos repositórios.
- Configurações de banco de dados e mapeamentos.

**API**
- Disponibilização dos endpoints REST.
- Consultas aos dados persistidos.
- Documentação Swagger.
- Health checks e controle de requisições.

**Worker**
- Execução autônoma dos processos de coleta.
- Atualização das previsões meteorológicas.
- Consolidação dos indicadores diários.

---

## 3. Funcionamento do sistema

O ThermoCity possui três processos principais de processamento de dados.

### 3.1. Coleta de condições climáticas

O Worker consulta a API da OpenWeather para obter as condições meteorológicas atuais de Presidente Prudente.

Dados coletados:

- Temperatura atual.
- Sensação térmica.
- Umidade relativa do ar.
- Pressão atmosférica.
- Velocidade do vento.
- Descrição das condições climáticas.
- Data e horário da medição.

A coleta considera as coordenadas:

| Parâmetro | Valor |
|---|---|
| Latitude | -22.12 |
| Longitude | -51.39 |
| Localidade | Presidente Prudente - SP |
| Intervalo de teste | 5 minutos |

O sistema também calcula o índice de calor utilizando temperatura e umidade relativa do ar.

Registros incompletos são identificados com o status `Partial`, enquanto registros com os dados necessários são classificados como `Complete`.

Valores ausentes são representados por `null`, evitando a utilização de valores artificiais.

### 3.2. Coleta de previsões meteorológicas

O Worker também consulta a previsão de cinco dias da OpenWeather, disponibilizada em intervalos de três horas.

Os dados são agrupados por dia, considerando o horário local da cidade.

Para cada dia são armazenados:

- Temperatura mínima.
- Temperatura máxima.
- Temperatura do período mais quente.
- Sensação térmica do período mais quente.
- Umidade relativa.
- Pressão atmosférica.
- Velocidade do vento.
- Descrição das condições climáticas.
- Índice de calor estimado.

Cada coleta de previsão mantém sua própria data de emissão. Isso permite comparar previsões realizadas em momentos diferentes com as condições efetivamente observadas.

### 3.3. Consolidação diária

O processo de consolidação é executado diariamente à 01:00, considerando o dia anterior no horário de Brasília.

São calculados:

- Temperatura mínima, média e máxima.
- Umidade mínima, média e máxima.
- Índice de calor mínimo, médio e máximo.
- Quantidade de observações válidas.

Somente observações com status `Complete` são consideradas.

Os resultados são armazenados permanentemente, permitindo consultas históricas sem depender dos registros temporários de coleta.

### Intervalos de execução

| Processo | Intervalo |
|---|---|
| Coleta atual | 5 minutos |
| Coleta de previsões | 5 minutos |
| Consolidação diária | 01:00 |

Os intervalos de cinco minutos foram definidos para facilitar os testes durante o desenvolvimento. Antes de utilizar essa frequência em produção, é necessário verificar os limites e as condições de uso da OpenWeather, que podem exigir intervalos maiores.

---

## 4. Modelo de dados

O sistema trabalha com três entidades principais.

### WeatherReading

Representa uma observação meteorológica realizada em determinado momento.

Principais propriedades:

- `Id`
- `Source`
- `Status`
- `MeasurementDate`
- `CollectionDate`
- `TemperatureCelsius`
- `FeelsLikeCelsius`
- `HumidityPercentage`
- `PressureHpa`
- `WindSpeedMs`
- `WeatherDescription`
- `HeatIndexCelsius`

### DailyForecast

Representa a previsão meteorológica consolidada para um dia.

Principais propriedades:

- `Id`
- `Source`
- `ForecastDate`
- `ForecastEmissionDate`
- `MinimumTemperatureCelsius`
- `MaximumTemperatureCelsius`
- `DayTemperatureCelsius`
- `DayFeelsLikeCelsius`
- `HumidityPercentage`
- `PressureHpa`
- `WindSpeedMs`
- `WeatherDescription`
- `EstimatedHeatIndexCelsius`
- `Status`

### DailyWeatherSummary

Representa o resumo meteorológico diário.

Principais propriedades:

- `Id`
- `Source`
- `ReferenceDate`
- `MinimumTemperatureCelsius`
- `AverageTemperatureCelsius`
- `MaximumTemperatureCelsius`
- `MinimumHumidityPercentage`
- `AverageHumidityPercentage`
- `MaximumHumidityPercentage`
- `MinimumHeatIndexCelsius`
- `AverageHeatIndexCelsius`
- `MaximumHeatIndexCelsius`
- `ObservationCount`
- `ConsolidationDate`

### Coleções MongoDB

| Coleção | Finalidade |
|---|---|
| `TMC_LEITURA_CLIMA` | Observações meteorológicas |
| `TMC_PREVISAO_DIARIA` | Previsões meteorológicas |
| `TMC_RESUMO_CLIMA_DIARIO` | Consolidações diárias |

As observações meteorológicas são planejadas para retenção temporária de sete dias. Os resumos diários são históricos e devem ser mantidos permanentemente.

### Tabelas PostgreSQL

| Tabela | Finalidade |
|---|---|
| `weather_readings` | Observações meteorológicas |
| `daily_forecasts` | Previsões meteorológicas |
| `daily_weather_summaries` | Consolidações diárias |

Os identificadores das entidades utilizam `ObjectId` e são convertidos para texto no PostgreSQL, preservando a identificação entre os modelos de persistência.

As datas que representam dias de calendário, como `ForecastDate` e `ReferenceDate`, devem utilizar o tipo `date` no PostgreSQL.

---

## 5. Pré-requisitos

Antes de executar o projeto, instale:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [MongoDB](https://www.mongodb.com/try/download/community) ou uma instância MongoDB Atlas.
- [PostgreSQL](https://www.postgresql.org/download/) ou uma instância Azure Database for PostgreSQL.
- Uma chave de API da [OpenWeather](https://openweathermap.org/api).
- Git.

Para verificar a instalação do .NET:

```bash
dotnet --version
```

Para verificar a instalação do Git:

```bash
git --version
```

---

## 6. Clonando o projeto

Clone o repositório:

```bash
git clone <URL_DO_REPOSITORIO>
```

Entre na pasta do projeto:

```bash
cd thermocity-weather-api
```

Restaure as dependências:

```bash
dotnet restore
```

Compile a solução:

```bash
dotnet build
```

---

## 7. Configuração das variáveis de ambiente

As configurações sensíveis não devem ser armazenadas diretamente nos arquivos versionados do projeto.

O sistema utiliza variáveis de ambiente para configurar as conexões e credenciais.

Os exemplos abaixo consideram macOS ou Linux, utilizando Bash ou Zsh.

### 7.1. OpenWeather

Crie uma conta na OpenWeather e gere uma chave de API.

```bash
export OpenWeather__ApiKey="SUA_NOVA_CHAVE_OPENWEATHER"
```

As demais configurações da integração podem permanecer nos arquivos `appsettings.json`:

```json
{
  "OpenWeather": {
    "BaseUrl": "https://api.openweathermap.org/data/2.5/",
    "Latitude": -22.12,
    "Longitude": -51.39,
    "Language": "pt_br"
  }
}
```

### 7.2. MongoDB

Configure a conexão com o MongoDB.

Exemplo local:

```bash
export MongoDb__ConnectionString="mongodb://admin:admin123@localhost:27017/thermocity-weather?authSource=admin"
```

Configure o nome do banco:

```bash
export MongoDb__DatabaseName="thermocity-weather"
```

Caso utilize MongoDB Atlas, substitua a connection string pela fornecida pela plataforma.

### 7.3. PostgreSQL

Configure a conexão com o PostgreSQL.

```bash
export ConnectionStrings__PostgreSql="Host=SEU_HOST;Port=5432;Database=thermocity;Username=SEU_USUARIO;Password=SUA_SENHA;SSL Mode=Require"
```

Para uma instância Azure PostgreSQL, utilize o endereço do servidor disponibilizado no portal Azure.

O parâmetro `SSL Mode=Require` deve ser utilizado quando o servidor exigir conexão criptografada.

### 7.4. Seleção do banco de leitura da API

A API permite selecionar qual banco será utilizado para realizar as consultas.

Para utilizar MongoDB:

```bash
export Database__ReadProvider="MongoDB"
```

Para utilizar PostgreSQL:

```bash
export Database__ReadProvider="PostgreSQL"
```

Essa configuração altera o provedor utilizado pelos repositórios de leitura da API.

### 7.5. Configurações do Worker

As configurações operacionais podem ser mantidas no `appsettings.json` do Worker:

```json
{
  "Worker": {
    "WeatherCollectionIntervalMinutes": 5,
    "ForecastCollectionIntervalMinutes": 5,
    "ConsolidationHour": 1,
    "ConsolidationMinute": 0,
    "ForecastDays": 3
  }
}
```

### 7.6. Aplicando todas as variáveis de ambiente

Para facilitar a execução, todas as variáveis podem ser configuradas em sequência no mesmo terminal.

```bash
export OpenWeather__ApiKey="SUA_NOVA_CHAVE_OPENWEATHER"

export MongoDb__ConnectionString="mongodb://admin:admin123@localhost:27017/thermocity-weather?authSource=admin"
export MongoDb__DatabaseName="thermocity-weather"

export ConnectionStrings__PostgreSql="Host=SEU_HOST;Port=5432;Database=thermocity;Username=SEU_USUARIO;Password=SUA_SENHA;SSL Mode=Require"

export Database__ReadProvider="MongoDB"
```

**Importante:** as variáveis de ambiente são aplicadas ao processo iniciado pelo terminal. Caso execute API e Worker em terminais diferentes, configure as variáveis nos dois terminais.

Não compartilhe chaves de API, senhas ou connection strings em commits ou arquivos públicos.

---

## 8. Configuração do PostgreSQL e migrations

O projeto utiliza Entity Framework Core para mapear as entidades para tabelas relacionais.

As configurações de mapeamento estão em:

```text
ThermoCity.Weather.Infrastructure/
└── PostgreSql/
    ├── Configurations/
    ├── Migrations/
    └── ThermoCityDbContext.cs
```

### 8.1. Instalar a ferramenta EF Core

Caso ainda não tenha a ferramenta instalada:

```bash
dotnet tool install --global dotnet-ef --version 10.0.0
```

Se já estiver instalada, atualize:

```bash
dotnet tool update --global dotnet-ef --version 10.0.0
```

Verifique:

```bash
dotnet ef --version
```

### 8.2. Criar uma migration

Caso seja necessário criar uma nova migration após alterações no modelo:

```bash
dotnet ef migrations add NomeDaMigration \
  --project src/ThermoCity.Weather.Infrastructure \
  --startup-project src/ThermoCity.Weather.Worker \
  --output-dir PostgreSql/Migrations
```

### 8.3. Aplicar as migrations

Com a variável `ConnectionStrings__PostgreSql` configurada no terminal:

```bash
dotnet ef database update \
  --project src/ThermoCity.Weather.Infrastructure \
  --startup-project src/ThermoCity.Weather.Worker
```

Esse comando aplica as migrations pendentes e cria ou atualiza as tabelas no banco configurado.

**Atenção:** o banco de dados `thermocity` precisa existir antes da execução. A migration cria as tabelas, mas não necessariamente o próprio banco.

---

## 9. Executando o projeto

A API e o Worker são processos independentes. Para o funcionamento completo, ambos devem estar em execução.

### 9.1. Iniciar o Worker

Abra um terminal na raiz do projeto.

Configure as variáveis de ambiente da seção 7.

Execute:

```bash
dotnet run --project src/ThermoCity.Weather.Worker
```

O Worker inicia os processos de coleta e consolidação.

Os processos de coleta são executados imediatamente na inicialização e, posteriormente, respeitam seus respectivos intervalos configurados.

A consolidação diária permanece agendada para o horário definido na configuração.

### 9.2. Iniciar a API

Abra um segundo terminal na raiz do projeto.

Configure as variáveis de ambiente da seção 7.

Execute:

```bash
dotnet run --project src/ThermoCity.Weather.Api --urls http://localhost:5050
```

A API ficará disponível em:

```text
http://localhost:5050
```

A documentação Swagger estará disponível em:

```text
http://localhost:5050/swagger
```

A API é responsável pela consulta dos dados já persistidos. A coleta meteorológica não depende de chamadas aos endpoints REST, pois é realizada diretamente pelo Worker.

---

## 10. Endpoints disponíveis

### 10.1. Leituras meteorológicas

| Método | Endpoint | Descrição |
|---|---|---|
| GET | `/api/readings/latest` | Consulta a leitura mais recente |
| GET | `/api/readings` | Consulta leituras por período |

Exemplo:

```http
GET http://localhost:5050/api/readings/latest?source=OpenWeather
```

Consulta por período:

```http
GET http://localhost:5050/api/readings?source=OpenWeather&startDate=2026-11-01T00:00:00Z&endDate=2026-11-14T00:00:00Z
```

### 10.2. Previsões meteorológicas

| Método | Endpoint | Descrição |
|---|---|---|
| GET | `/api/forecasts` | Consulta previsões por período |

Exemplo:

```http
GET http://localhost:5050/api/forecasts?source=OpenWeather&startDate=2026-11-14&endDate=2026-11-16
```

### 10.3. Resumos diários

| Método | Endpoint | Descrição |
|---|---|---|
| GET | `/api/daily-summaries/{date}` | Consulta resumo de uma data |
| GET | `/api/daily-summaries` | Consulta resumos por período |

Exemplo:

```http
GET http://localhost:5050/api/daily-summaries/2026-11-13?source=OpenWeather
```

Consulta por período:

```http
GET http://localhost:5050/api/daily-summaries?source=OpenWeather&startDate=2026-11-01&endDate=2026-11-14
```

Os endpoints de período utilizam a data final como inclusiva na API. Internamente, a consulta utiliza o início do dia seguinte como limite exclusivo.

### 10.4. Health checks

| Método | Endpoint | Descrição |
|---|---|---|
| GET | `/health/live` | Verifica se a aplicação está em execução |
| GET | `/health/ready` | Verifica a disponibilidade das dependências configuradas |

Exemplo:

```bash
curl http://localhost:5050/health/live
```

```bash
curl http://localhost:5050/health/ready
```

---

## 11. Testes automatizados

O projeto utiliza xUnit e Moq para validar o comportamento das camadas da aplicação.

Os testes unitários verificam:

- Cálculo do índice de calor.
- Coleta das condições meteorológicas.
- Coleta e processamento de previsões.
- Consolidação diária.
- Comportamento dos controllers.
- Validação de parâmetros e respostas HTTP.

Os testes de integração verificam:

- Inicialização da API.
- Disponibilidade dos endpoints.
- Respostas HTTP.
- Documentação Swagger.
- Health checks.

### 11.1. Executar todos os testes

Na raiz do projeto:

```bash
dotnet test
```

### 11.2. Executar somente os testes unitários

```bash
dotnet test tests/ThermoCity.Weather.UnitTests
```

### 11.3. Executar somente os testes de integração

```bash
dotnet test tests/ThermoCity.Weather.IntegrationTests
```

Os testes de integração utilizam dependências simuladas para validar os endpoints sem exigir uma instância real do MongoDB ou PostgreSQL.

---

## 12. Persistência e consistência dos dados

O projeto possui implementações de repositórios para MongoDB e PostgreSQL.

Os repositórios são responsáveis por realizar operações de inserção, atualização e consulta das entidades.

As operações de atualização utilizam chaves de negócio para evitar duplicidade lógica:

| Entidade | Chave de negócio |
|---|---|
| WeatherReading | Source + MeasurementDate |
| DailyForecast | Source + ForecastEmissionDate + ForecastDate |
| DailyWeatherSummary | Source + ReferenceDate |

O PostgreSQL utiliza índices únicos compostos para reforçar essas restrições.

A seleção do provedor de leitura é realizada por configuração, permitindo consultar os dados pelo MongoDB ou PostgreSQL sem alterar os controllers.

Quando a aplicação utiliza dois bancos para persistência, não existe uma transação distribuída automática entre eles. Uma falha durante a gravação pode deixar os bancos temporariamente inconsistentes. Para um ambiente de produção, é recomendável implementar uma estratégia de retentativa, reconciliação ou Outbox.

---

## 13. Logging e monitoramento

O projeto utiliza Serilog para registrar eventos importantes durante a execução.

Os logs auxiliam no acompanhamento de:

- Inicialização dos processos.
- Execuções de coleta.
- Falhas na comunicação com a OpenWeather.
- Erros de persistência.
- Execuções da consolidação diária.
- Falhas de processamento.

O tratamento de cancelamento respeita o `CancellationToken`, permitindo encerrar os processos sem tratar um encerramento solicitado como erro inesperado.

---

## 14. Fluxo completo para testar o sistema

Para executar o projeto do zero, siga esta sequência:

**1.** Clone o repositório e entre na pasta:

```bash
git clone <URL_DO_REPOSITORIO>
cd thermocity-weather-api
```

**2.** Restaure e compile:

```bash
dotnet restore
dotnet build
```

**3.** Configure as variáveis de ambiente:

```bash
export OpenWeather__ApiKey="SUA_NOVA_CHAVE_OPENWEATHER"

export MongoDb__ConnectionString="mongodb://admin:admin123@localhost:27017/thermocity-weather?authSource=admin"
export MongoDb__DatabaseName="thermocity-weather"

export ConnectionStrings__PostgreSql="Host=SEU_HOST;Port=5432;Database=thermocity;Username=SEU_USUARIO;Password=SUA_SENHA;SSL Mode=Require"

export Database__ReadProvider="MongoDB"
```

**4.** Aplique as migrations do PostgreSQL:

```bash
dotnet ef database update \
  --project src/ThermoCity.Weather.Infrastructure \
  --startup-project src/ThermoCity.Weather.Worker
```

**5.** Execute os testes:

```bash
dotnet test
```

**6.** Inicie o Worker no primeiro terminal:

```bash
dotnet run --project src/ThermoCity.Weather.Worker
```

**7.** Configure as mesmas variáveis em um segundo terminal e inicie a API:

```bash
dotnet run --project src/ThermoCity.Weather.Api --urls http://localhost:5050
```

**8.** Acesse a documentação Swagger:

```text
http://localhost:5050/swagger
```

**9.** Verifique a disponibilidade da API:

```bash
curl http://localhost:5050/health/live
```

**10.** Consulte a leitura meteorológica mais recente:

```bash
curl "http://localhost:5050/api/readings/latest?source=OpenWeather"
```

**11.** Consulte as previsões:

```bash
curl "http://localhost:5050/api/forecasts?source=OpenWeather&startDate=2026-11-14&endDate=2026-11-16"
```

**12.** Consulte os resumos diários após a execução da consolidação:

```bash
curl "http://localhost:5050/api/daily-summaries/2026-11-13?source=OpenWeather"
```

---

## 15. Considerações para produção

Antes de disponibilizar a aplicação em produção, alguns pontos devem ser revisados:

- Ajustar a frequência de coleta de acordo com os limites da OpenWeather.
- Configurar credenciais específicas e permissões mínimas para cada banco.
- Garantir a criação dos índices únicos no MongoDB.
- Configurar o índice TTL de sete dias para a coleção de leituras temporárias.
- Implementar retentativas e reconciliação para persistência em múltiplos bancos.
- Configurar HTTPS e políticas de CORS conforme os consumidores da API.
- Configurar autenticação e autorização por JWT, incluindo validação de emissor, assinatura e expiração.
- Avaliar a estratégia de execução e monitoramento do Worker em ambiente de hospedagem.
- Evitar credenciais e chaves de API em arquivos versionados.

---

## 16. Objetivo do projeto

O ThermoCity Weather API constitui a camada de coleta, processamento e disponibilização de informações meteorológicas do ecossistema ThermoCity.

A solução permite manter um histórico de observações, acompanhar previsões, calcular indicadores relacionados ao calor e disponibilizar informações consolidadas para aplicações que necessitam monitorar condições climáticas urbanas.

A arquitetura foi organizada para permitir a evolução da solução, incluindo novas fontes meteorológicas, novos indicadores e integrações com outros serviços do ecossistema.