# .NET Rule Engine Architecture & Design Document

## Executive Summary

This document provides a comprehensive design for building a Fair Work Awards Rule Engine using .NET 8, following CQRS design pattern, optimized for Azure cloud with cost-effective storage and caching strategies.

---

## Table of Contents

1. [Technology Stack Decision](#1-technology-stack-decision)
2. [Rule Engine Library Selection](#2-rule-engine-library-selection)
3. [Database Selection Analysis](#3-database-selection-analysis)
4. [Architecture Design (CQRS)](#4-architecture-design-cqrs)
5. [Azure Deployment Strategy](#5-azure-deployment-strategy)
6. [Caching Strategy](#6-caching-strategy)
7. [API vs Azure Functions Decision](#7-api-vs-azure-functions-decision)
8. [Solution Structure](#8-solution-structure)
9. [Implementation Details](#9-implementation-details)
10. [Cost Optimization](#10-cost-optimization)
11. [Performance Benchmarks](#11-performance-benchmarks)
12. [Development Roadmap](#12-development-roadmap)

---

## 1. Technology Stack Decision

### Recommended Stack

| Component | Technology | Justification |
|-----------|------------|---------------|
| **Runtime** | .NET 8 LTS | Long-term support, best performance, native AOT support |
| **API Framework** | ASP.NET Core Minimal APIs | Lightweight, fast startup, lower memory footprint |
| **Rule Engine** | RulesEngine (Microsoft) | Open-source, JSON-based rules, actively maintained |
| **Database** | Azure SQL Database (Serverless) | Cost-effective, auto-pause, familiar T-SQL |
| **Cache** | Azure Cache for Redis (Basic) | Fast lookups, distributed cache, session state |
| **Hosting** | Azure Container Apps | Serverless containers, scale-to-zero, cost-effective |
| **Messaging** | Azure Service Bus | For async command processing in CQRS |

### Technology Comparison Matrix

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    TECHNOLOGY SELECTION MATRIX                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  RULE ENGINE OPTIONS                                                         │
│  ┌────────────────────┬─────────┬──────────┬─────────┬────────────────────┐ │
│  │ Library            │ Stars   │ NuGet DL │ Active? │ Best For           │ │
│  ├────────────────────┼─────────┼──────────┼─────────┼────────────────────┤ │
│  │ RulesEngine (MS)   │ 3.5k+   │ 2M+      │ ✓ Yes   │ JSON rules, simple │ │
│  │ NRules             │ 1.5k+   │ 500k+    │ ✓ Yes   │ Complex rules      │ │
│  │ FluentValidation   │ 8k+     │ 100M+    │ ✓ Yes   │ Validation only    │ │
│  │ Drools.NET         │ 200+    │ 50k+     │ ○ Med   │ Enterprise rules   │ │
│  └────────────────────┴─────────┴──────────┴─────────┴────────────────────┘ │
│                                                                              │
│  ★ RECOMMENDED: RulesEngine (Microsoft)                                      │
│    - JSON-based rule definitions (no code deployment for rule changes)      │
│    - Supports nested rules, custom operators, lambda expressions            │
│    - Works well with dynamic data from dropdown conditions                  │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Rule Engine Library Selection

### Primary Choice: Microsoft RulesEngine

**GitHub**: https://github.com/microsoft/RulesEngine

**Why RulesEngine?**

1. **JSON-Based Rules**: Rules can be stored in database and modified without code deployment
2. **Expression Support**: Supports C# lambda expressions for complex calculations
3. **Workflow Support**: Can chain multiple rules in workflows
4. **Active Maintenance**: Microsoft-backed, regular updates
5. **Performance**: Compiled expressions cached for fast execution

### Rule Definition Example

```json
{
  "WorkflowName": "PayRateCalculation",
  "Rules": [
    {
      "RuleName": "BaseRateRule",
      "SuccessEvent": "Base rate applied",
      "Expression": "input1.ClassificationLevel >= 1 AND input1.ClassificationLevel <= 8",
      "Actions": {
        "OnSuccess": {
          "Name": "OutputExpression",
          "Context": {
            "Expression": "input1.BaseHourlyRate"
          }
        }
      }
    },
    {
      "RuleName": "CasualLoadingRule",
      "SuccessEvent": "Casual loading applied",
      "Expression": "input1.EmploymentType == \"Casual\"",
      "Actions": {
        "OnSuccess": {
          "Name": "OutputExpression",
          "Context": {
            "Expression": "input1.BaseHourlyRate * 1.25"
          }
        }
      }
    },
    {
      "RuleName": "SaturdayPenaltyRule",
      "SuccessEvent": "Saturday penalty applied",
      "Expression": "input1.Tags.Contains(\"Weekend\") AND input1.DayOfWeek == \"Saturday\"",
      "Actions": {
        "OnSuccess": {
          "Name": "OutputExpression",
          "Context": {
            "Expression": "input1.BaseHourlyRate * 1.5"
          }
        }
      }
    },
    {
      "RuleName": "SundayPenaltyRule",
      "Expression": "input1.Tags.Contains(\"Weekend\") AND input1.DayOfWeek == \"Sunday\"",
      "Actions": {
        "OnSuccess": {
          "Name": "OutputExpression",
          "Context": {
            "Expression": "input1.BaseHourlyRate * 2.0"
          }
        }
      }
    },
    {
      "RuleName": "PublicHolidayRule",
      "Expression": "input1.Tags.Contains(\"PublicHoliday\")",
      "Actions": {
        "OnSuccess": {
          "Name": "OutputExpression",
          "Context": {
            "Expression": "input1.BaseHourlyRate * 2.5"
          }
        }
      }
    }
  ]
}
```

### Alternative: NRules (For Complex Scenarios)

If rules become more complex with forward/backward chaining needs:

```csharp
// NRules example for complex rule chaining
public class OvertimeAfterThreeHoursRule : Rule
{
    public override void Define()
    {
        PayCalculation calc = null;
        
        When()
            .Match<PayCalculation>(() => calc, 
                c => c.OvertimeHours > 3,
                c => c.EmploymentType != "Casual");
        
        Then()
            .Do(ctx => calc.ApplyOvertimeRate(2.0m, hoursAfterThree));
    }
}
```

---

## 3. Database Selection Analysis

### Comparison Matrix

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     DATABASE COMPARISON FOR AZURE                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌───────────────────┬──────────────┬──────────────┬──────────────────────┐ │
│  │ Criteria          │ Azure SQL    │ PostgreSQL   │ Cosmos DB (MongoDB)  │ │
│  │                   │ (Serverless) │ (Flexible)   │                      │ │
│  ├───────────────────┼──────────────┼──────────────┼──────────────────────┤ │
│  │ Min Cost/Month    │ ~$5-15       │ ~$15-25      │ ~$25+ (400 RU/s)     │ │
│  │ Auto-Pause        │ ✓ Yes        │ ✗ No         │ ✓ Yes (Serverless)   │ │
│  │ Schema            │ Relational   │ Relational   │ Document             │ │
│  │ Complex Joins     │ ✓ Excellent  │ ✓ Excellent  │ ✗ Limited            │ │
│  │ .NET Support      │ ✓ Native     │ ✓ Npgsql     │ ✓ Native SDK         │ │
│  │ Stored Procs      │ ✓ T-SQL      │ ✓ PL/pgSQL   │ ✗ No                 │ │
│  │ Familiar to Team  │ ✓ Yes        │ ○ Medium     │ ✗ Learning curve     │ │
│  │ EF Core Support   │ ✓ Full       │ ✓ Full       │ ○ Partial            │ │
│  └───────────────────┴──────────────┴──────────────┴──────────────────────┘ │
│                                                                              │
│  ★ RECOMMENDED: Azure SQL Database (Serverless Tier)                         │
│                                                                              │
│  Reasons:                                                                    │
│  1. Auto-pause after 1 hour of inactivity (saves ~70% cost)                  │
│  2. Team already familiar with MSSQL                                         │
│  3. Complex joins for rule generation work best in relational DB             │
│  4. Stored procedures for batch rule computation                             │
│  5. Built-in query optimization for pay rate calculations                    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Azure SQL Serverless Pricing (Estimated)

| Usage Pattern | vCores | Cost/Month |
|--------------|--------|------------|
| Dev/Test (low traffic) | 0.5-1 | $5-15 |
| Production (moderate) | 1-2 | $30-60 |
| Production (high) | 2-4 | $60-120 |

**Key Feature**: Auto-pause charges only for storage when inactive (~$5/month for 10GB)

---

## 4. Architecture Design (CQRS)

### CQRS Pattern Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        CQRS ARCHITECTURE                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                              ┌──────────────┐                               │
│                              │   API Layer  │                               │
│                              │ (Minimal API)│                               │
│                              └──────┬───────┘                               │
│                                     │                                        │
│              ┌──────────────────────┴──────────────────────┐                │
│              │                                              │                │
│              ▼                                              ▼                │
│  ┌───────────────────────┐                    ┌───────────────────────┐     │
│  │    COMMAND SIDE       │                    │     QUERY SIDE        │     │
│  │    (Write Model)      │                    │     (Read Model)      │     │
│  ├───────────────────────┤                    ├───────────────────────┤     │
│  │                       │                    │                       │     │
│  │  Commands:            │                    │  Queries:             │     │
│  │  • CreateAward        │                    │  • GetPayRates        │     │
│  │  • UpdateClassif.     │                    │  • GetAwardDropdown   │     │
│  │  • SaveTenantOverride │                    │  • GetPenaltySummary  │     │
│  │  • CreateTag          │                    │  • GetAllowances      │     │
│  │                       │                    │                       │     │
│  │  ┌─────────────────┐  │                    │  ┌─────────────────┐  │     │
│  │  │ Command Handler │  │                    │  │  Query Handler  │  │     │
│  │  └────────┬────────┘  │                    │  └────────┬────────┘  │     │
│  │           │           │                    │           │           │     │
│  │           ▼           │                    │           ▼           │     │
│  │  ┌─────────────────┐  │                    │  ┌─────────────────┐  │     │
│  │  │  Domain Model   │  │                    │  │  Read DTOs      │  │     │
│  │  │  (Aggregates)   │  │                    │  │  (Projections)  │  │     │
│  │  └────────┬────────┘  │                    │  └────────┬────────┘  │     │
│  │           │           │                    │           │           │     │
│  └───────────┼───────────┘                    └───────────┼───────────┘     │
│              │                                            │                  │
│              ▼                                            ▼                  │
│  ┌───────────────────────┐                    ┌───────────────────────┐     │
│  │    Write Database     │                    │     Redis Cache       │     │
│  │    (Azure SQL)        │───────────────────▶│   (Read Projections)  │     │
│  │                       │   Sync/Events      │                       │     │
│  └───────────────────────┘                    └───────────────────────┘     │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

#### Command Side (Write Model)
- **Purpose**: Handle all state-changing operations
- **Database**: Azure SQL (normalized, transactional)
- **Operations**:
  - System Admin creates/updates awards, classifications, allowances
  - Tenant Admin saves rate overrides
  - Tag management

#### Query Side (Read Model)
- **Purpose**: Optimized read operations for rule calculation
- **Cache**: Redis for frequently accessed data
- **Operations**:
  - Calculate pay rates based on 5 dropdown conditions
  - Fetch dropdown options (cascading)
  - Generate pay summaries

### MediatR Implementation

```csharp
// Commands (Write Operations)
public record CreateAwardCommand(string AwardCode, string AwardName, int IndustryId) : IRequest<int>;
public record UpdateClassificationCommand(int ClassificationId, decimal NewRate) : IRequest<bool>;
public record SaveTenantOverrideCommand(int TenantId, int RuleId, decimal OverrideRate) : IRequest<bool>;

// Queries (Read Operations)  
public record CalculatePayRateQuery(
    int AwardId,
    string EmploymentType,
    int ClassificationLevel,
    List<int> AllowanceIds,
    List<int> TagIds,
    int? TenantId
) : IRequest<PayRateSummary>;

public record GetDropdownOptionsQuery(string DropdownType, int? ParentId) : IRequest<List<DropdownOption>>;
```

---

## 5. Azure Deployment Strategy

### Recommended: Azure Container Apps

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    AZURE DEPLOYMENT ARCHITECTURE                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                     AZURE CONTAINER APPS ENVIRONMENT                 │    │
│  │                                                                      │    │
│  │   ┌────────────────────┐     ┌────────────────────┐                 │    │
│  │   │  Rule Engine API   │     │  Background Jobs   │                 │    │
│  │   │  (Container App)   │     │  (Container App)   │                 │    │
│  │   │                    │     │                    │                 │    │
│  │   │  • Scale: 0-10     │     │  • Scale: 0-1      │                 │    │
│  │   │  • Min: 0 (scale   │     │  • Cache warming   │                 │    │
│  │   │    to zero)        │     │  • Rule sync       │                 │    │
│  │   │  • CPU: 0.25-1     │     │                    │                 │    │
│  │   └─────────┬──────────┘     └────────────────────┘                 │    │
│  │             │                                                        │    │
│  └─────────────┼────────────────────────────────────────────────────────┘    │
│                │                                                              │
│    ┌───────────┴───────────┐                                                 │
│    │                       │                                                 │
│    ▼                       ▼                                                 │
│  ┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐           │
│  │  Azure SQL DB   │   │  Redis Cache    │   │ Service Bus     │           │
│  │  (Serverless)   │   │  (Basic C0)     │   │ (Basic)         │           │
│  │                 │   │                 │   │                 │           │
│  │  • Auto-pause   │   │  • 250MB        │   │  • Command      │           │
│  │  • 1-2 vCores   │   │  • ~$16/month   │   │    queuing      │           │
│  │  • ~$5-30/month │   │                 │   │  • ~$10/month   │           │
│  └─────────────────┘   └─────────────────┘   └─────────────────┘           │
│                                                                              │
│  ESTIMATED MONTHLY COST (Low Traffic):                                       │
│  • Container Apps:    $0-15 (scale to zero)                                 │
│  • Azure SQL:         $5-15 (auto-pause)                                    │
│  • Redis Cache:       $16 (Basic C0)                                        │
│  • Service Bus:       $10 (Basic)                                           │
│  ─────────────────────────────────────                                      │
│  TOTAL:               $31-56/month                                          │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Alternative: Azure Functions (Consumption Plan)

| Aspect | Container Apps | Azure Functions |
|--------|---------------|-----------------|
| Cold Start | ~2-3 seconds | ~5-10 seconds (.NET) |
| Cost (low traffic) | $0-15/month | $0-5/month |
| Cost (high traffic) | Better | More expensive |
| Complexity | Medium | Lower |
| State Management | Full control | Stateless |
| Long-running | ✓ Yes | ✗ Limited (10 min) |

**Recommendation**: Start with **Azure Container Apps** for:
- Consistent performance (no cold starts after first request)
- Better for CQRS pattern (stateful command handling)
- Easier local development with containers
- Scale-to-zero still available

---

## 6. Caching Strategy

### Multi-Layer Cache Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      CACHING STRATEGY                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  REQUEST FLOW:                                                               │
│                                                                              │
│  Client Request                                                              │
│       │                                                                      │
│       ▼                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │ LAYER 1: In-Memory Cache (IMemoryCache)                             │    │
│  │                                                                      │    │
│  │ • Dropdown options (Awards, Employment Types, Levels)               │    │
│  │ • TTL: 5 minutes                                                    │    │
│  │ • Size: ~10MB max                                                   │    │
│  │ • Cost: FREE (included in app memory)                               │    │
│  │                                                                      │    │
│  │ HIT? ─────Yes────▶ Return immediately                               │    │
│  │  │                                                                   │    │
│  │  No                                                                  │    │
│  │  ▼                                                                   │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│       │                                                                      │
│       ▼                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │ LAYER 2: Redis Distributed Cache                                    │    │
│  │                                                                      │    │
│  │ • Computed pay rate combinations (by tenant + conditions hash)      │    │
│  │ • Rule engine workflow JSON                                         │    │
│  │ • TTL: 1 hour (or until data changes)                               │    │
│  │ • Size: ~250MB (Basic C0)                                           │    │
│  │ • Cost: ~$16/month                                                  │    │
│  │                                                                      │    │
│  │ HIT? ─────Yes────▶ Return (also populate L1)                        │    │
│  │  │                                                                   │    │
│  │  No                                                                  │    │
│  │  ▼                                                                   │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│       │                                                                      │
│       ▼                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │ LAYER 3: Azure SQL Database                                         │    │
│  │                                                                      │    │
│  │ • Execute rule engine calculation                                   │    │
│  │ • Fetch from normalized tables                                      │    │
│  │ • Populate both L1 and L2 cache                                     │    │
│  │                                                                      │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Cache Key Strategy

```csharp
public static class CacheKeys
{
    // Dropdown caches (5 min TTL)
    public static string Awards => "dropdown:awards";
    public static string EmploymentTypes(int awardId) => $"dropdown:emp:{awardId}";
    public static string Classifications(int awardId, string empType) => $"dropdown:class:{awardId}:{empType}";
    public static string Allowances(int awardId) => $"dropdown:allow:{awardId}";
    public static string Tags => "dropdown:tags";
    
    // Computed rate cache (1 hour TTL)
    public static string PayRate(int awardId, string empType, int level, string allowanceHash, string tagHash)
        => $"rate:{awardId}:{empType}:{level}:{allowanceHash}:{tagHash}";
    
    // Tenant override cache (1 hour TTL)
    public static string TenantOverride(int tenantId, string rateKey)
        => $"override:{tenantId}:{rateKey}";
    
    // Rule engine workflow cache (until invalidated)
    public static string RuleWorkflow(int awardId) => $"rules:workflow:{awardId}";
}
```

### Cache Invalidation Strategy

```csharp
public class CacheInvalidationService
{
    // Invalidate on data changes (Command handlers call this)
    public async Task InvalidateAwardCache(int awardId)
    {
        var keysToInvalidate = new[]
        {
            CacheKeys.Awards,
            CacheKeys.EmploymentTypes(awardId),
            CacheKeys.Allowances(awardId),
            CacheKeys.RuleWorkflow(awardId)
        };
        
        await _redis.KeyDeleteAsync(keysToInvalidate);
        
        // Invalidate all rate calculations for this award
        await _redis.ExecuteAsync("EVAL", 
            "return redis.call('del', unpack(redis.call('keys', ARGV[1])))", 
            0, $"rate:{awardId}:*");
    }
}
```

---

## 7. API vs Azure Functions Decision

### Decision Matrix

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                 REST API vs AZURE FUNCTIONS ANALYSIS                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  SCENARIO: Rule Engine for Pay Rate Calculation                             │
│                                                                              │
│  ┌────────────────────────┬─────────────────┬─────────────────────────────┐ │
│  │ Factor                 │ REST API        │ Azure Functions             │ │
│  │                        │ (Container Apps)│ (Consumption)               │ │
│  ├────────────────────────┼─────────────────┼─────────────────────────────┤ │
│  │ Cold Start             │ 2-3 sec         │ 5-10 sec (.NET)             │ │
│  │ Consistent Latency     │ ✓ Yes           │ ✗ Variable                  │ │
│  │ CQRS Pattern           │ ✓ Natural fit   │ ○ Awkward                   │ │
│  │ State Management       │ ✓ Full control  │ ✗ Stateless                 │ │
│  │ Redis Connection       │ ✓ Persistent    │ ✗ Reconnect each time       │ │
│  │ Local Development      │ ✓ Docker        │ ○ Func CLI                  │ │
│  │ Cost (Low Traffic)     │ $0-15           │ $0-5                        │ │
│  │ Cost (High Traffic)    │ Better          │ More expensive              │ │
│  │ Complexity             │ Medium          │ Lower                       │ │
│  └────────────────────────┴─────────────────┴─────────────────────────────┘ │
│                                                                              │
│  ★ RECOMMENDATION: REST API (Azure Container Apps)                          │
│                                                                              │
│  Reasons:                                                                    │
│  1. Rule engine requires consistent low latency (cold starts hurt UX)       │
│  2. CQRS pattern works better with always-on API                            │
│  3. Redis connection pooling more efficient                                 │
│  4. Scale-to-zero still available with Container Apps                       │
│  5. Easier to add background jobs for cache warming                         │
│                                                                              │
│  HYBRID APPROACH (Optional):                                                 │
│  • REST API: Real-time pay rate calculations                                │
│  • Azure Functions: Batch jobs (nightly rate refresh, reporting)            │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 8. Solution Structure

### Project Organization

```
FairWorkAwards.RuleEngine/
│
├── src/
│   ├── FairWorkAwards.Api/                      # API Layer (ASP.NET Core)
│   │   ├── Program.cs                           # Minimal API entry point
│   │   ├── Endpoints/
│   │   │   ├── PayRateEndpoints.cs              # GET/POST /api/pay-rates
│   │   │   ├── DropdownEndpoints.cs             # GET /api/dropdowns/*
│   │   │   ├── AdminEndpoints.cs                # System Admin operations
│   │   │   └── TenantEndpoints.cs               # Tenant-specific operations
│   │   ├── Middleware/
│   │   │   ├── CachingMiddleware.cs
│   │   │   └── TenantContextMiddleware.cs
│   │   └── appsettings.json
│   │
│   ├── FairWorkAwards.Application/              # Application Layer (CQRS)
│   │   ├── Commands/
│   │   │   ├── CreateAwardCommand.cs
│   │   │   ├── UpdateClassificationCommand.cs
│   │   │   ├── SaveTenantOverrideCommand.cs
│   │   │   └── Handlers/
│   │   │       ├── CreateAwardHandler.cs
│   │   │       ├── UpdateClassificationHandler.cs
│   │   │       └── SaveTenantOverrideHandler.cs
│   │   ├── Queries/
│   │   │   ├── CalculatePayRateQuery.cs
│   │   │   ├── GetDropdownOptionsQuery.cs
│   │   │   └── Handlers/
│   │   │       ├── CalculatePayRateHandler.cs
│   │   │       └── GetDropdownOptionsHandler.cs
│   │   ├── DTOs/
│   │   │   ├── PayRateSummaryDto.cs
│   │   │   ├── DropdownOptionDto.cs
│   │   │   └── RuleInputDto.cs
│   │   └── Validators/
│   │       ├── CalculatePayRateValidator.cs
│   │       └── TenantOverrideValidator.cs
│   │
│   ├── FairWorkAwards.Domain/                   # Domain Layer
│   │   ├── Entities/
│   │   │   ├── Award.cs
│   │   │   ├── Classification.cs
│   │   │   ├── EmploymentType.cs
│   │   │   ├── Allowance.cs
│   │   │   ├── PenaltyRate.cs
│   │   │   ├── Tag.cs
│   │   │   └── TenantOverride.cs
│   │   ├── ValueObjects/
│   │   │   ├── Money.cs
│   │   │   └── RateMultiplier.cs
│   │   ├── Aggregates/
│   │   │   └── PayRateCalculation.cs
│   │   └── Events/
│   │       ├── AwardUpdatedEvent.cs
│   │       └── RateOverrideCreatedEvent.cs
│   │
│   ├── FairWorkAwards.Infrastructure/           # Infrastructure Layer
│   │   ├── Persistence/
│   │   │   ├── FairWorkDbContext.cs
│   │   │   ├── Configurations/
│   │   │   │   ├── AwardConfiguration.cs
│   │   │   │   └── ClassificationConfiguration.cs
│   │   │   └── Repositories/
│   │   │       ├── AwardRepository.cs
│   │   │       └── PayRateRepository.cs
│   │   ├── Caching/
│   │   │   ├── RedisCacheService.cs
│   │   │   ├── CacheKeys.cs
│   │   │   └── CacheInvalidationService.cs
│   │   └── RulesEngine/
│   │       ├── PayRateRulesEngine.cs
│   │       ├── RuleWorkflowLoader.cs
│   │       └── Workflows/
│   │           ├── base-rate-rules.json
│   │           ├── penalty-rules.json
│   │           └── allowance-rules.json
│   │
│   └── FairWorkAwards.Shared/                   # Shared Kernel
│       ├── Constants/
│       │   └── EmploymentTypeCodes.cs
│       └── Exceptions/
│           └── RuleEngineException.cs
│
├── tests/
│   ├── FairWorkAwards.UnitTests/
│   │   ├── RulesEngine/
│   │   │   └── PayRateCalculationTests.cs
│   │   └── Handlers/
│   │       └── CalculatePayRateHandlerTests.cs
│   └── FairWorkAwards.IntegrationTests/
│       ├── Api/
│       │   └── PayRateEndpointsTests.cs
│       └── Database/
│           └── RepositoryTests.cs
│
├── docker/
│   ├── Dockerfile
│   └── docker-compose.yml
│
├── infrastructure/
│   ├── terraform/                               # Infrastructure as Code
│   │   ├── main.tf
│   │   ├── variables.tf
│   │   └── outputs.tf
│   └── bicep/                                   # Alternative: Azure Bicep
│       └── main.bicep
│
├── FairWorkAwards.sln
└── README.md
```

---

## 9. Implementation Details

### 9.1 Rule Engine Service

```csharp
// FairWorkAwards.Infrastructure/RulesEngine/PayRateRulesEngine.cs

using RulesEngine.Models;
using RulesEngine.Extensions;

public class PayRateRulesEngine : IPayRateRulesEngine
{
    private readonly RulesEngine.RulesEngine _rulesEngine;
    private readonly IRedisCacheService _cache;
    private readonly ILogger<PayRateRulesEngine> _logger;

    public PayRateRulesEngine(
        IRuleWorkflowLoader workflowLoader,
        IRedisCacheService cache,
        ILogger<PayRateRulesEngine> logger)
    {
        _cache = cache;
        _logger = logger;
        
        // Load rule workflows from JSON (cached)
        var workflows = workflowLoader.LoadWorkflowsAsync().Result;
        
        _rulesEngine = new RulesEngine.RulesEngine(
            workflows.ToArray(),
            new ReSettings
            {
                CustomTypes = new[] { typeof(PayRateInput), typeof(AllowanceList) }
            });
    }

    public async Task<PayRateSummary> CalculatePayRatesAsync(
        PayRateInput input, 
        CancellationToken ct = default)
    {
        // Check cache first
        var cacheKey = GenerateCacheKey(input);
        var cached = await _cache.GetAsync<PayRateSummary>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return cached;
        }

        // Execute rule engine
        var ruleParams = new RuleParameter[]
        {
            new("input1", input)
        };

        var results = await _rulesEngine.ExecuteAllRulesAsync("PayRateCalculation", ruleParams);

        // Build summary from rule results
        var summary = BuildPayRateSummary(input, results);

        // Cache result
        await _cache.SetAsync(cacheKey, summary, TimeSpan.FromHours(1));

        return summary;
    }

    private PayRateSummary BuildPayRateSummary(PayRateInput input, List<RuleResultTree> results)
    {
        var summary = new PayRateSummary
        {
            BaseHourlyRate = input.BaseHourlyRate,
            BaseWeeklyRate = input.BaseHourlyRate * 38,
            BaseAnnualRate = input.BaseHourlyRate * 38 * 52,
            PenaltyRates = new List<PenaltyRateSummary>(),
            Allowances = new List<AllowanceSummary>(),
            CalculatedAt = DateTime.UtcNow
        };

        foreach (var result in results.Where(r => r.IsSuccess))
        {
            var actionResult = result.ActionResult?.Output;
            
            switch (result.Rule.RuleName)
            {
                case var name when name.Contains("Penalty"):
                    summary.PenaltyRates.Add(new PenaltyRateSummary
                    {
                        Name = result.Rule.SuccessEvent,
                        Multiplier = (decimal)actionResult,
                        CalculatedRate = input.BaseHourlyRate * (decimal)actionResult
                    });
                    break;
                    
                case var name when name.Contains("Allowance"):
                    summary.Allowances.Add(new AllowanceSummary
                    {
                        Name = result.Rule.SuccessEvent,
                        Amount = (decimal)actionResult,
                        Frequency = "per_week" // From rule metadata
                    });
                    break;
            }
        }

        // Calculate totals
        summary.TotalAllowancesPerWeek = summary.Allowances.Sum(a => a.Amount);
        summary.TotalWeeklyPay = summary.BaseWeeklyRate + summary.TotalAllowancesPerWeek;

        return summary;
    }
}
```

### 9.2 CQRS Query Handler

```csharp
// FairWorkAwards.Application/Queries/Handlers/CalculatePayRateHandler.cs

public class CalculatePayRateHandler : IRequestHandler<CalculatePayRateQuery, PayRateSummary>
{
    private readonly IPayRateRepository _repository;
    private readonly IPayRateRulesEngine _rulesEngine;
    private readonly ITenantOverrideService _overrideService;
    private readonly ILogger<CalculatePayRateHandler> _logger;

    public CalculatePayRateHandler(
        IPayRateRepository repository,
        IPayRateRulesEngine rulesEngine,
        ITenantOverrideService overrideService,
        ILogger<CalculatePayRateHandler> logger)
    {
        _repository = repository;
        _rulesEngine = rulesEngine;
        _overrideService = overrideService;
        _logger = logger;
    }

    public async Task<PayRateSummary> Handle(
        CalculatePayRateQuery query, 
        CancellationToken ct)
    {
        // 1. Fetch base classification data
        var classification = await _repository.GetClassificationAsync(
            query.AwardId,
            query.EmploymentType,
            query.ClassificationLevel,
            ct);

        if (classification == null)
            throw new NotFoundException("Classification not found");

        // 2. Fetch selected allowances
        var allowances = await _repository.GetAllowancesAsync(
            query.AwardId,
            query.AllowanceIds,
            ct);

        // 3. Fetch applicable tags (for penalty rules)
        var tags = await _repository.GetTagsAsync(query.TagIds, ct);

        // 4. Build rule engine input
        var ruleInput = new PayRateInput
        {
            AwardId = query.AwardId,
            EmploymentType = query.EmploymentType,
            ClassificationLevel = query.ClassificationLevel,
            BaseHourlyRate = classification.BaseHourlyRate,
            AllowanceIds = query.AllowanceIds,
            Tags = tags.Select(t => t.Code).ToList(),
            TenantId = query.TenantId
        };

        // 5. Execute rule engine
        var summary = await _rulesEngine.CalculatePayRatesAsync(ruleInput, ct);

        // 6. Apply tenant overrides if present
        if (query.TenantId.HasValue)
        {
            summary = await _overrideService.ApplyOverridesAsync(
                query.TenantId.Value,
                summary,
                ct);
        }

        return summary;
    }
}
```

### 9.3 Minimal API Endpoints

```csharp
// FairWorkAwards.Api/Endpoints/PayRateEndpoints.cs

public static class PayRateEndpoints
{
    public static void MapPayRateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pay-rates")
            .WithTags("Pay Rates")
            .WithOpenApi();

        // Calculate pay rates based on 5 conditions
        group.MapPost("/calculate", CalculatePayRates)
            .WithName("CalculatePayRates")
            .WithSummary("Calculate pay rates for given conditions")
            .Produces<PayRateSummary>(200)
            .Produces<ProblemDetails>(400);

        // Get pay rate summary for display
        group.MapGet("/summary", GetPayRateSummary)
            .WithName("GetPayRateSummary")
            .Produces<PayRateSummary>(200);
    }

    private static async Task<IResult> CalculatePayRates(
        CalculatePayRateRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var query = new CalculatePayRateQuery(
            request.AwardId,
            request.EmploymentType,
            request.ClassificationLevel,
            request.AllowanceIds,
            request.TagIds,
            request.TenantId
        );

        var result = await mediator.Send(query, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetPayRateSummary(
        [FromQuery] int awardId,
        [FromQuery] string employmentType,
        [FromQuery] int level,
        [FromQuery] int[]? allowanceIds,
        [FromQuery] int[]? tagIds,
        [FromQuery] int? tenantId,
        IMediator mediator,
        CancellationToken ct)
    {
        var query = new CalculatePayRateQuery(
            awardId,
            employmentType,
            level,
            allowanceIds?.ToList() ?? new(),
            tagIds?.ToList() ?? new(),
            tenantId
        );

        var result = await mediator.Send(query, ct);
        return Results.Ok(result);
    }
}
```

### 9.4 Redis Cache Service

```csharp
// FairWorkAwards.Infrastructure/Caching/RedisCacheService.cs

public class RedisCacheService : IRedisCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _db = redis.GetDatabase();
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        await _db.StringSetAsync(key, json, expiry ?? TimeSpan.FromHours(1));
    }

    public async Task RemoveAsync(string key)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        var endpoints = _redis.GetEndPoints();
        var server = _redis.GetServer(endpoints.First());
        
        var keys = server.Keys(pattern: pattern).ToArray();
        if (keys.Any())
        {
            await _db.KeyDeleteAsync(keys);
            _logger.LogInformation("Invalidated {Count} cache keys matching {Pattern}", 
                keys.Length, pattern);
        }
    }
}
```

---

## 10. Cost Optimization

### Monthly Cost Breakdown (Low Traffic)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                   AZURE COST ESTIMATE (LOW TRAFFIC)                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Service                        │ Tier               │ Est. Cost/Month      │
│  ───────────────────────────────┼────────────────────┼─────────────────────│
│  Azure Container Apps           │ Consumption        │ $0-15                │
│  (scale to zero when idle)      │ 0.25 vCPU, 0.5GB   │                      │
│  ───────────────────────────────┼────────────────────┼─────────────────────│
│  Azure SQL Database             │ Serverless         │ $5-15                │
│  (auto-pause after 1 hour)      │ 0.5-1 vCores       │                      │
│  ───────────────────────────────┼────────────────────┼─────────────────────│
│  Azure Cache for Redis          │ Basic C0           │ $16                  │
│  (250 MB)                       │                    │                      │
│  ───────────────────────────────┼────────────────────┼─────────────────────│
│  Azure Service Bus              │ Basic              │ $0.05 per million    │
│  (for async commands)           │                    │ ~$1-2                │
│  ───────────────────────────────┼────────────────────┼─────────────────────│
│  Azure Container Registry       │ Basic              │ $5                   │
│  (for Docker images)            │                    │                      │
│  ───────────────────────────────┼────────────────────┼─────────────────────│
│  Azure Key Vault                │ Standard           │ $0.03 per 10k ops    │
│  (secrets management)           │                    │ ~$1                  │
│  ═══════════════════════════════════════════════════════════════════════════│
│                                                                              │
│  TOTAL ESTIMATED:               $28-55 per month (low traffic)              │
│                                                                              │
│  ───────────────────────────────────────────────────────────────────────────│
│                                                                              │
│  COST OPTIMIZATION TIPS:                                                     │
│                                                                              │
│  1. Azure SQL Serverless auto-pauses = pay only for storage when idle       │
│  2. Container Apps scale-to-zero = $0 when no requests                      │
│  3. Use Redis only for frequently accessed data (dropdowns, computed rates) │
│  4. Consider Azure Hybrid Benefit if you have SQL Server licenses           │
│  5. Use Azure Dev/Test pricing for non-production environments              │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Cost Optimization Strategies

1. **Auto-Pause SQL**: Configure auto-pause delay to 1 hour (minimum)
2. **Scale-to-Zero**: Container Apps automatically scale down when idle
3. **Redis Tier Selection**: Start with Basic C0 (250MB), upgrade only if needed
4. **Batch Updates**: Group admin changes to reduce database calls
5. **Aggressive Caching**: Cache dropdown options for 5+ minutes

---

## 11. Performance Benchmarks

### Expected Performance Targets

| Operation | Target Latency | With Cache | Without Cache |
|-----------|---------------|------------|---------------|
| Calculate Pay Rate | < 100ms | ~20ms | ~80ms |
| Get Dropdown Options | < 50ms | ~5ms | ~30ms |
| Get Pay Rate Summary | < 100ms | ~25ms | ~90ms |
| Save Tenant Override | < 200ms | N/A | ~150ms |

### Load Testing Scenarios

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     PERFORMANCE TEST SCENARIOS                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Scenario 1: Normal Operations                                               │
│  ─────────────────────────────────                                          │
│  • 100 concurrent users                                                     │
│  • 60% reads (pay rate queries)                                             │
│  • 30% dropdown fetches                                                     │
│  • 10% writes (admin updates)                                               │
│  • Expected: < 100ms p95 latency                                            │
│                                                                              │
│  Scenario 2: Bulk Payroll Processing                                        │
│  ─────────────────────────────────                                          │
│  • 1000 pay rate calculations in parallel                                   │
│  • All for same award (cache-friendly)                                      │
│  • Expected: < 5 seconds total                                              │
│                                                                              │
│  Scenario 3: Cache Miss Storm                                               │
│  ─────────────────────────────────                                          │
│  • Simulate Redis restart                                                   │
│  • 100 concurrent requests with empty cache                                 │
│  • Expected: < 500ms p95 (graceful degradation)                             │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 12. Development Roadmap

### Phase 1: Foundation (Weeks 1-2)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  PHASE 1: FOUNDATION                                                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Week 1:                                                                     │
│  ☐ Set up solution structure (Clean Architecture)                           │
│  ☐ Configure Azure SQL Database (Serverless)                                │
│  ☐ Implement Entity Framework Core migrations                               │
│  ☐ Seed database with MA000004 award data                                   │
│                                                                              │
│  Week 2:                                                                     │
│  ☐ Set up MediatR for CQRS                                                  │
│  ☐ Implement basic query handlers (GetAwards, GetClassifications)           │
│  ☐ Configure Redis cache service                                            │
│  ☐ Write unit tests for handlers                                            │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Phase 2: Rule Engine (Weeks 3-4)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  PHASE 2: RULE ENGINE                                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Week 3:                                                                     │
│  ☐ Integrate Microsoft RulesEngine                                          │
│  ☐ Create rule workflow JSONs (base rates, penalties, allowances)           │
│  ☐ Implement PayRateRulesEngine service                                     │
│  ☐ Test rule execution with sample inputs                                   │
│                                                                              │
│  Week 4:                                                                     │
│  ☐ Implement CalculatePayRateQuery handler                                  │
│  ☐ Build pay rate summary aggregation                                       │
│  ☐ Add caching layer for computed rates                                     │
│  ☐ Write integration tests for rule engine                                  │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Phase 3: API & Tenant Features (Weeks 5-6)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  PHASE 3: API & TENANT FEATURES                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Week 5:                                                                     │
│  ☐ Implement Minimal API endpoints                                          │
│  ☐ Add OpenAPI/Swagger documentation                                        │
│  ☐ Implement dropdown endpoints (cascading)                                 │
│  ☐ Add request validation (FluentValidation)                                │
│                                                                              │
│  Week 6:                                                                     │
│  ☐ Implement tenant override commands                                       │
│  ☐ Add tenant context middleware                                            │
│  ☐ Implement override validation (>= award minimum)                         │
│  ☐ Build pay rate summary response with overrides                           │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Phase 4: Deployment & Optimization (Weeks 7-8)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  PHASE 4: DEPLOYMENT & OPTIMIZATION                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Week 7:                                                                     │
│  ☐ Create Dockerfile for API                                                │
│  ☐ Set up Azure Container Apps environment                                  │
│  ☐ Configure GitHub Actions CI/CD                                           │
│  ☐ Deploy to Azure (dev environment)                                        │
│                                                                              │
│  Week 8:                                                                     │
│  ☐ Performance testing and optimization                                     │
│  ☐ Cache tuning (TTL, key strategy)                                         │
│  ☐ Add Application Insights monitoring                                      │
│  ☐ Documentation and handover                                               │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Appendix A: Sample API Requests/Responses

### Calculate Pay Rate

**Request:**
```http
POST /api/v1/pay-rates/calculate
Content-Type: application/json

{
  "awardId": 4,
  "employmentType": "FT",
  "classificationLevel": 1,
  "allowanceIds": [1, 3],
  "tagIds": [2, 4],
  "tenantId": 123
}
```

**Response:**
```json
{
  "awardCode": "MA000004",
  "awardName": "General Retail Industry Award 2020",
  "classification": {
    "level": 1,
    "name": "Retail Employee Level 1"
  },
  "employmentType": {
    "code": "FT",
    "name": "Full-time",
    "casualLoading": null
  },
  "basePay": {
    "hourlyRate": 26.55,
    "weeklyRate": 1008.90,
    "annualRate": 52462.80
  },
  "penaltyRates": [
    {
      "name": "Ordinary Hours",
      "multiplier": 1.0,
      "hourlyRate": 26.55,
      "weeklyRate": 1008.90
    },
    {
      "name": "Saturday (First 4 Hours)",
      "multiplier": 1.5,
      "hourlyRate": 39.83,
      "weeklyRate": 1513.35
    },
    {
      "name": "Saturday (After 4 Hours)",
      "multiplier": 2.0,
      "hourlyRate": 53.10,
      "weeklyRate": 2017.80
    },
    {
      "name": "Sunday",
      "multiplier": 2.0,
      "hourlyRate": 53.10,
      "weeklyRate": 2017.80
    }
  ],
  "allowances": [
    {
      "name": "First Aid Allowance",
      "amount": 13.89,
      "unit": "per_week"
    },
    {
      "name": "Cold Work Disability",
      "amount": 14.06,
      "unit": "per_week"
    }
  ],
  "summary": {
    "baseWeeklyPay": 1008.90,
    "totalAllowances": 27.95,
    "totalWeeklyPay": 1036.85
  },
  "tenantOverride": null,
  "calculatedAt": "2025-12-02T03:30:00Z",
  "cacheHit": true
}
```

---

## Appendix B: Environment Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "SqlConnection": "Server=tcp:fwa-sql.database.windows.net;Database=FairWorkAwards;",
    "RedisConnection": "fwa-redis.redis.cache.windows.net:6380,password=xxx,ssl=True"
  },
  "RulesEngine": {
    "WorkflowPath": "Workflows",
    "CacheWorkflows": true
  },
  "Caching": {
    "DropdownTtlMinutes": 5,
    "PayRateTtlMinutes": 60,
    "DefaultTtlMinutes": 30
  },
  "Azure": {
    "KeyVaultName": "fwa-keyvault"
  }
}
```

---

## Appendix C: NuGet Packages

### Required Packages

```xml
<!-- API Project -->
<PackageReference Include="MediatR" Version="12.2.0" />
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />

<!-- Application Project -->
<PackageReference Include="RulesEngine" Version="5.0.3" />

<!-- Infrastructure Project -->
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
<PackageReference Include="StackExchange.Redis" Version="2.7.10" />
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.0" />

<!-- Testing -->
<PackageReference Include="xunit" Version="2.6.2" />
<PackageReference Include="Moq" Version="4.20.70" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
<PackageReference Include="Testcontainers.MsSql" Version="3.6.0" />
<PackageReference Include="Testcontainers.Redis" Version="3.6.0" />
```

---

## Approval Checklist

Before proceeding to development, please confirm:

- [ ] Technology stack approved (.NET 8, Azure Container Apps, Azure SQL)
- [ ] Rule engine library approved (Microsoft RulesEngine)
- [ ] Database choice approved (Azure SQL Serverless)
- [ ] Caching strategy approved (Redis + In-Memory)
- [ ] Cost estimate acceptable (~$30-55/month low traffic)
- [ ] Architecture design approved (CQRS with MediatR)
- [ ] Development timeline acceptable (8 weeks)

---

*Document Version: 1.0*  
*Last Updated: December 2025*  
*Author: Architecture Team*
