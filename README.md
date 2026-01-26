# DPP Plugin

[![Made by M&M Software](https://img.shields.io/badge/Made_by_M%26M_Software-364955?style=flat-square)](https://www.mm-software.com/)
[![Apache License](https://img.shields.io/badge/License-Apache-364955.svg?style=flat-square)](https://www.apache.org/licenses/)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4)](https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-10.0&WT.mc_id=dotnet-35129-website) 

## Overview

The **DPP Plugin** is a lightweight, open-source API that provides data for **Digital Product Passports (DPP)** and **Asset Administration Shell (AAS)** submodels.

It works together with **DataEngine**:

- **DataEngine** builds AAS submodels from standard templates  
- **Plugin** supplies the actual data values for requested **Semantic IDs**

The Plugin focuses **only on data storage and data access** and does **not** contain AAS or template logic.

By default, the DPP Plugin uses a **relational database** (for example PostgreSQL) as its primary data storage. The AAS-related data is modeled in normalized tables and accessed via JSON-based APIs, which gives you:

- Strong consistency guarantees for DPP-relevant data  
- Flexible querying and indexing using standard SQL  
- A schema-driven way to evolve the data model without changing the API contract

---

## How It Works

1. **DataEngine** requests data for a Submodel  
2. The request contains a **Semantic ID–based JSON Schema**  
3. The **Plugin**:
   - Maps Semantic IDs to its database  
   - Returns available values  
4. **DataEngine** assembles and returns a complete **AAS Submodel**

> The Plugin does **not** need to understand AAS internals it works only with **schemas and data**.

---

## Features Overview

- **Schema-driven API**  
  Operates entirely on JSON Schema definitions without requiring AAS-specific logic.

- **Semantic ID–based data mapping**  
  Maps Semantic IDs to stored values to resolve data dynamically and consistently.

- **Database-agnostic design**  
  Supports different database technologies without changing the API contract.

- **Customer-owned data**  
  Customers fully control and manage their own data; TwinEngine provides only schemas.

- **JSON-based communication**  
  Uses JSON for all requests and responses to ensure simplicity and interoperability.

- **Open-source and extensible**  
  Designed to be easily extended and integrated into custom environments.


---
### Use Cases

- **Asset Metadata Retrieval**: Fetch shell descriptors and asset information from PostgreSQL databases
- **Submodel Data Access**: Query specific submodel data using JSON Schema-based filters
- **Manifest Management**: Access plugin capabilities and configuration details

## Key Technical Features

- **PostgreSQL Native**: Full support for PostgreSQL 10+ with optimized JSON queries
- **OpenTelemetry Support**: Built-in observability with distributed tracing
- **Serilog Integration**: Structured logging with support for console, file, and OpenTelemetry-based export to centralized logging infrastructures
- **API Versioning**: Support for multiple API versions via headers
- **Docker Ready**: Production-grade Docker support with multi-stage builds
- **Swagger/OpenAPI**: Auto-generated API documentation

### Supported DPP Submodels

The DPP Plugin provides data for the following submodels that are part of the **Digital Product Passport (DPP)**:

- **Nameplate v3.0.1**
- **ContactInformation v1.0**
- **HandoverDocumentation v2.0.1**
- **TechnicalData v1.2.1**
- **CarbonFootprint v1.0.1**

Data is modeled using **AAS Submodels**, and this Plugin provides the **actual values** for those models.

## Quick Start

### Running the Setup

1. **Clone or extract this repository:**
   ```bash
   git clone https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine.git
   cd AAS.TwinEngine.DataEngine\example
   ```

2. **Start all services:**
   ```bash
   docker-compose up -d
   ```

3. **Access the Web UI:**
   Open your browser and navigate to:
   ```
   http://localhost:8080/aas-ui/

- For more info : [TwinEngine Demonstrator Setup WIth DPP Plugin](https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/blob/develop/example/README.md)

---

## Installation & Setup

### Local Development Setup

#### 1. Clone the Repository

```bash
git clone https://github.com/AAS-TwinEngine/AAS.TwinEngine.Plugin.RelationalDatabase.git
cd source/Aas.TwinEngine.Plugin.RelationalDatabase
```

#### 2. Restore Dependencies

```bash
dotnet restore
```

#### 3. Configure PostgreSQL Connection

Edit `appsettings.development.json`:

```json
{
  "RelationalDatabaseConfiguration": {
    "ConnectionString": "Host=localhost;Port=5432;Database="";Username="your_username";Password="your_password""
  }
}
```

#### 4. Build the Project

```bash
dotnet build
```

#### 5. Start the Development Server

```bash
dotnet run
```

The API will be available at: `https://localhost:5058`

Access the Swagger documentation: `https://localhost:5058/swagger`

## Configuration

### Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `RelationalDatabaseConfiguration__ConnectionString` | PostgreSQL connection string | `Host=localhost;Port=5432;Database=twin_engine_db;Username=postgres;Password=password` |

### Semantic ID Mapping

The file `Data/mapping.json` contains mappings from database columns to AAS semantic IDs:

```json
[
  {
    "column": "dbo.TableName.ColumnName",
    "semanticId": ["Semantic-ID"]
  }
]
```