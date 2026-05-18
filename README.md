# ArchLens - Upload Service

[![CI](https://github.com/archlens-platform/archlens-upload-service/actions/workflows/ci.yml/badge.svg)](https://github.com/archlens-platform/archlens-upload-service/actions/workflows/ci.yml)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=archlens-platform_archlens-upload-service&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=archlens-platform_archlens-upload-service)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=archlens-platform_archlens-upload-service&metric=coverage)](https://sonarcloud.io/summary/new_code?id=archlens-platform_archlens-upload-service)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=archlens-platform_archlens-upload-service&metric=bugs)](https://sonarcloud.io/summary/new_code?id=archlens-platform_archlens-upload-service)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=archlens-platform_archlens-upload-service&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=archlens-platform_archlens-upload-service)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=archlens-platform_archlens-upload-service&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=archlens-platform_archlens-upload-service)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=archlens-platform_archlens-upload-service&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=archlens-platform_archlens-upload-service)
[![Maintainability](https://sonarcloud.io/api/project_badges/measure?project=archlens-platform_archlens-upload-service&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=archlens-platform_archlens-upload-service)

> **Microsserviço de Upload e Gestão de Diagramas Arquiteturais**
> Hackathon FIAP - Fase 5 | Pós-Tech Software Architecture + IA para Devs
>
> **Autor:** Rafael Henrique Barbosa Pereira (RM366243)

[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![Docker](https://img.shields.io/badge/Docker-Container-2496ED)](https://www.docker.com/)
[![Clean Architecture](https://img.shields.io/badge/Architecture-Clean-00ADD8)](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-4169E1)](https://www.postgresql.org/)
[![MinIO](https://img.shields.io/badge/MinIO-S3--Compatible-C72E49)](https://min.io/)
[![RabbitMQ](https://img.shields.io/badge/RabbitMQ-3.x-FF6600)](https://www.rabbitmq.com/)

## 📋 Descrição

O **Upload Service** é o microsserviço responsável pelo recebimento, validação e armazenamento de diagramas arquiteturais no ecossistema ArchLens. Suporta upload de arquivos PNG, JPG e PDF com validação por **magic bytes**, deduplicação via **SHA256 hash**, e limite de tamanho. Implementa o **Outbox Pattern** para garantir entrega confiável de eventos de domínio — os eventos são salvos junto com a entidade na mesma transação e publicados por um background processor.

## 🏗️ Arquitetura

O projeto segue os princípios de **Clean Architecture**:

```mermaid
graph TB
    subgraph "API Layer"
        A[Endpoints / Controllers]
        B[Multipart Upload Handler]
        C[Exception Handlers]
    end

    subgraph "Application Layer"
        D[Use Cases]
        E[Handlers]
        F[Validators]
    end

    subgraph "Domain Layer"
        G[Entities - DiagramUpload]
        H[Domain Events]
        I[Business Rules]
    end

    subgraph "Infrastructure Layer"
        J[PostgreSQL Repositories]
        K[MinIO Storage Client]
        L[Outbox Processor]
        M[MassTransit Messaging]
    end

    A --> D
    D --> G
    G --> J
    G --> K
    L --> M
    M --> N[RabbitMQ]
```

## 🔄 Outbox Pattern - Entrega Confiável de Eventos

Este serviço implementa o **Outbox Pattern** para garantir consistência entre o estado da entidade e a publicação de eventos:

```mermaid
sequenceDiagram
    participant C as Cliente
    participant UP as Upload Service
    participant PG as PostgreSQL
    participant MINIO as MinIO
    participant BG as Background Processor
    participant RMQ as RabbitMQ
    participant ORCH as Orchestrator

    C->>UP: POST /api/upload/diagrams (multipart)
    Note over UP: Valida magic bytes + file size
    Note over UP: Calcula SHA256 hash (dedup)
    UP->>MINIO: Armazena arquivo no bucket

    rect rgb(200, 230, 200)
        Note over UP,PG: Transação Atômica
        UP->>PG: Salva DiagramUpload entity
        UP->>PG: Salva OutboxMessage (DiagramUploadedEvent)
    end

    UP-->>C: 201 Created

    rect rgb(220, 220, 255)
        Note over BG: Background Processor (polling)
        BG->>PG: Busca OutboxMessages pendentes
        BG->>RMQ: Publica DiagramUploadedEvent
        BG->>PG: Marca OutboxMessage como processada
    end

    RMQ->>ORCH: DiagramUploadedEvent
    Note over ORCH: Inicia saga de análise
```

## 🛠️ Tecnologias

| Tecnologia | Versão | Descrição |
|------------|--------|-----------|
| .NET | 9.0 | Framework principal |
| PostgreSQL | 17 | Banco de dados relacional |
| Entity Framework Core | 9.x | ORM para PostgreSQL |
| MinIO | latest | Object Storage S3-Compatible |
| MassTransit | 8.x | Message Broker abstraction |
| RabbitMQ | 3.x | Message Broker |
| OpenTelemetry | 1.x | Traces e Métricas |
| Serilog | 4.x | Logs Estruturados |
| Swagger/OpenAPI | 6.x | Documentação da API |

## 🔒 Isolamento de Banco de Dados

> ⚠️ **Requisito:** "Nenhum serviço pode acessar diretamente o banco de outro serviço."

Este serviço acessa **exclusivamente** seu próprio banco PostgreSQL (`archlens_upload`) e bucket MinIO (`archlens-diagrams`). A comunicação com outros serviços é feita **apenas via RabbitMQ (eventos)**:

```mermaid
graph LR
    UPLOAD[Upload Service<br/>:5066] --> PG[(PostgreSQL<br/>archlens_upload)]
    UPLOAD --> MINIO[(MinIO<br/>archlens-diagrams)]
    UPLOAD -.->|Eventos via Outbox| RMQ[RabbitMQ]

    ORCH[Orchestrator Service] -.->|Eventos| RMQ
    AUTH[Auth Service] -.->|Eventos| RMQ

    style PG fill:#4169E1,color:#fff
    style MINIO fill:#C72E49,color:#fff
    style RMQ fill:#ff6600,color:#fff
```

**Eventos publicados:** `DiagramUploadedEvent` (via Outbox Pattern)
**Eventos consumidos:** `UserAccountDeletedEvent`

## 📁 Estrutura do Projeto

```
archlens-upload-service/
├── src/
│   ├── ArchLens.Upload.Api/                # API Layer
│   │   ├── Endpoints/                      # Minimal APIs
│   │   │   └── DiagramUploadEndpoints.cs   # Upload, List, Status
│   │   ├── Middlewares/                     # CorrelationId
│   │   └── Program.cs                      # Entry point (:5066)
│   │
│   ├── ArchLens.Upload.Application/        # Application Layer
│   │   ├── UseCases/                       # Commands/Queries
│   │   └── Validators/                     # File Validation
│   │
│   ├── ArchLens.Upload.Domain/             # Domain Layer
│   │   ├── Entities/                       # DiagramUpload, OutboxMessage
│   │   ├── Events/                         # DiagramUploadedEvent
│   │   └── Interfaces/                     # Contratos
│   │
│   └── ArchLens.Upload.Infrastructure/     # Infrastructure Layer
│       ├── Persistence/                    # EF Core + PostgreSQL
│       ├── Storage/                        # MinIO Client
│       ├── Outbox/                         # Outbox Processor (Background)
│       └── Messaging/                      # MassTransit Publishers
│
└── tests/
    └── ArchLens.Upload.Tests/              # Testes unitários e integração
```

## 🚀 Como Executar

### Pré-requisitos
- .NET 9.0 SDK
- Docker (para PostgreSQL, MinIO e RabbitMQ)

### Passos

```bash
# 1. Subir infraestrutura
docker-compose up -d postgres minio rabbitmq

# 2. Executar a API
dotnet run --project src/ArchLens.Upload.Api
```

A API estará disponível em: `http://localhost:5066`

## 📡 Endpoints

### Diagramas (`/api/upload/diagrams`)

| Método | Endpoint | Auth | Descrição |
|--------|----------|------|-----------|
| POST | `/api/upload/diagrams` | 🔐 JWT | Upload de diagrama (multipart/form-data) |
| GET | `/api/upload/diagrams` | 🔐 JWT | Listar diagramas (paginado) |
| GET | `/api/upload/diagrams/{id}/status` | 🔐 JWT | Consultar status do processamento |

### Validações de Upload

| Validação | Regra |
|-----------|-------|
| Formatos aceitos | PNG, JPG/JPEG, PDF |
| Magic Bytes | Validação binária do header do arquivo |
| Tamanho máximo | Configurável via settings |
| Deduplicação | SHA256 hash — rejeita arquivos duplicados |

### Exemplo de Upload

```bash
curl -X POST http://localhost:5066/api/upload/diagrams \
  -H "Authorization: Bearer <jwt-token>" \
  -F "file=@diagrama-arquitetura.png"
```

## 📊 Diagrama de Entidades

```mermaid
erDiagram
    DIAGRAM_UPLOAD {
        guid Id PK
        string FileName
        string ContentType
        long FileSize
        string FileHash
        string StoragePath
        enum Status
        guid UserId
        datetime CreatedAt
    }

    OUTBOX_MESSAGE {
        guid Id PK
        string EventType
        string Payload
        datetime CreatedAt
        datetime ProcessedAt
        bool IsProcessed
    }

    DIAGRAM_UPLOAD ||--o{ OUTBOX_MESSAGE : generates
```

## 📈 Fluxo de Negócio

```mermaid
stateDiagram-v2
    [*] --> Uploaded: Arquivo recebido e armazenado
    Uploaded --> Processing: Orchestrator iniciou análise
    Processing --> Analyzed: IA completou análise
    Analyzed --> Completed: Relatório gerado
    Processing --> Failed: Erro na análise
```

## 📨 Eventos do Saga

### Eventos Publicados (via Outbox)

| Evento | Quando | Payload |
|--------|--------|---------|
| `DiagramUploadedEvent` | Arquivo validado e armazenado com sucesso | DiagramId, FileName, StoragePath, UserId |

### Eventos Consumidos

| Evento | Ação |
|--------|------|
| `UserAccountDeletedEvent` | Remove diagramas do usuário (LGPD cascade) |

## 🧪 Testes

```bash
# Rodar todos os testes
dotnet test

# Rodar com cobertura
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings

# Testes de integração (requer Docker)
dotnet test --filter "Category=Integration"
```

## 🔧 Configuração

### Variáveis de Ambiente

| Variável | Descrição |
|----------|-----------|
| `ConnectionStrings__DefaultConnection` | String de conexão PostgreSQL |
| `MinIO__Endpoint` | Endpoint do MinIO (ex: `localhost:9000`) |
| `MinIO__AccessKey` | Access Key do MinIO |
| `MinIO__SecretKey` | Secret Key do MinIO |
| `MinIO__BucketName` | Nome do bucket (`archlens-diagrams`) |
| `MinIO__UseSSL` | Usar SSL para conexão MinIO |
| `RabbitMQ__Host` | Host do RabbitMQ |
| `RabbitMQ__Username` | Usuário do RabbitMQ |
| `RabbitMQ__Password` | Senha do RabbitMQ |
| `Upload__MaxFileSizeBytes` | Tamanho máximo de arquivo |
| `OpenTelemetry__Endpoint` | Endpoint do OTLP Exporter |

## 🐳 Docker

```bash
docker build -t archlens-upload-service .
docker run -p 5066:8080 archlens-upload-service
```

## 📈 Observabilidade

O serviço possui integração completa com **OpenTelemetry** e **Serilog** para observabilidade:

```mermaid
graph LR
    subgraph "Upload Service"
        A[HTTP Request] --> B[AspNetCore Instrumentation]
        B --> C[EF Core Instrumentation]
        C --> D[HttpClient Instrumentation]
        D --> E[Outbox Processor Traces]
    end

    E --> F[OTLP Exporter]
    F --> G[Observability Backend]
```

**Instrumentações:**
- `AspNetCore` - Traces de requisições HTTP
- `EntityFrameworkCore` - Traces de operações PostgreSQL
- `HttpClient` - Traces de chamadas ao MinIO
- `MassTransit` - Traces de mensageria RabbitMQ

**Métricas:**
- Runtime (.NET metrics)
- Process (CPU, Memory)
- ASP.NET Core (requests, latência)
- Outbox (mensagens pendentes, processadas)

### Serilog (Logs Estruturados)

```json
{
  "Timestamp": "2026-03-15T00:00:00Z",
  "Level": "Information",
  "MessageTemplate": "Diagram {FileName} uploaded successfully with hash {FileHash}",
  "Properties": {
    "FileName": "architecture-diagram.png",
    "FileHash": "a1b2c3d4e5...",
    "FileSize": 245760,
    "UserId": "abc-123",
    "CorrelationId": "def-456",
    "ServiceName": "archlens-upload-service"
  }
}
```

---

FIAP - Pós-Tech Software Architecture + IA para Devs | Fase 5 - Hackathon (12SOAT + 6IADT)
