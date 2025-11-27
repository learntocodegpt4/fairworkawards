# Fair Work Awards Management System Architecture

## Overview

This document outlines the system architecture for managing Australian Fair Work Commission (FWC) Awards in a multi-tenant SaaS application. The system enables System Administrators to manage awards and apply them to tenant organizations based on their industry type.

## System Context

### What are Fair Work Awards?

Fair Work Awards are legal documents that set minimum terms and conditions of employment in Australia. The General Retail Industry Award [MA000004] used in this repository is an example that classifies employees into eight levels based on their skills, duties, and responsibilities, with each level determining the minimum pay rate.

### Key Award Components

Based on the MA000004 data structure:

| Component | Description | Example |
|-----------|-------------|---------|
| **Classifications** | Employee levels (1-8) with base pay rates | Retail Employee Level 1: $26.55/hour |
| **Pay Rates** | Base hourly/weekly rates by classification | Weekly: $1,008.90 for Level 1 |
| **Penalty Rates** | Additional rates for overtime, weekends, holidays | 200% for Sunday work |
| **Allowances** | Additional payments for specific conditions | Meal allowance: $23.59/occasion |
| **Wage Allowances** | Location/role-based additions | Broken Hill: 4.28% extra |

## System Architecture

### Multi-Tenant Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     FAIR WORK AWARDS PLATFORM                    │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────┐     ┌─────────────────────────────────┐   │
│  │  System Admin   │     │      Awards Master Data          │   │
│  │    Portal       │────▶│  - All 156 FWC Awards           │   │
│  │                 │     │  - Classifications              │   │
│  └────────┬────────┘     │  - Pay Rates                    │   │
│           │              │  - Allowances                    │   │
│           │              │  - Penalty Rates                 │   │
│           ▼              └─────────────────────────────────┘   │
│  ┌─────────────────┐                                            │
│  │ Award Assignment│                                            │
│  │    Engine       │                                            │
│  └────────┬────────┘                                            │
│           │                                                      │
│           ▼                                                      │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                    TENANT LAYER                          │   │
│  │                                                          │   │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐              │   │
│  │  │ Tenant A │  │ Tenant B │  │ Tenant C │   ...        │   │
│  │  │ (Retail) │  │ (Hospo)  │  │ (Retail) │              │   │
│  │  │ MA000004 │  │ MA000009 │  │ MA000004 │              │   │
│  │  └──────────┘  └──────────┘  └──────────┘              │   │
│  │                                                          │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Role Hierarchy

```
┌─────────────────────────────────────────────────────────┐
│                    ROLE HIERARCHY                        │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  Level 1: SYSTEM ADMIN                                   │
│  ├── Manages all awards master data                     │
│  ├── Creates and manages tenants                        │
│  ├── Assigns awards to tenants by industry              │
│  ├── Updates award data when FWC publishes changes      │
│  └── System-wide configuration                          │
│                                                          │
│  Level 2: TENANT ADMIN                                   │
│  ├── Views assigned awards for their organization       │
│  ├── Configures employee classifications                │
│  ├── Manages which award rules apply                    │
│  ├── Sets up custom allowances (within award limits)    │
│  └── Generates compliance reports                       │
│                                                          │
│  Level 3: TENANT USER                                    │
│  ├── Views applicable pay rates                         │
│  ├── Calculates employee pay                            │
│  └── Accesses read-only award information              │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

## Domain Model

### Core Entities

```
┌─────────────────────────────────────────────────────────────────┐
│                       DOMAIN MODEL                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────┐         ┌──────────────────┐                  │
│  │    Award     │         │     Industry     │                  │
│  ├──────────────┤         ├──────────────────┤                  │
│  │ award_id     │◄───────▶│ industry_code    │                  │
│  │ code         │    *    │ name             │                  │
│  │ name         │         │ description      │                  │
│  │ operative_   │         └──────────────────┘                  │
│  │   from       │                                               │
│  └──────┬───────┘                                               │
│         │                                                        │
│         │1                                                       │
│         │                                                        │
│         ▼*                                                       │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐      │
│  │Classification│    │  Allowance   │    │ PenaltyRate  │      │
│  ├──────────────┤    ├──────────────┤    ├──────────────┤      │
│  │ level        │    │ type         │    │ condition    │      │
│  │ name         │    │ amount       │    │ rate_percent │      │
│  │ base_rate    │    │ frequency    │    │ day_type     │      │
│  └──────────────┘    └──────────────┘    └──────────────┘      │
│                                                                  │
│  ┌──────────────┐         ┌──────────────────┐                  │
│  │    Tenant    │         │  TenantAward     │                  │
│  ├──────────────┤    *    ├──────────────────┤                  │
│  │ tenant_id    │◄───────▶│ tenant_id        │                  │
│  │ name         │         │ award_id         │                  │
│  │ industry_id  │         │ effective_from   │                  │
│  │ created_at   │         │ configuration    │                  │
│  └──────────────┘         └──────────────────┘                  │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

## System Admin Workflows

### 1. Award Management

```
┌─────────────────────────────────────────────────────────────────┐
│            SYSTEM ADMIN: AWARD MANAGEMENT WORKFLOW               │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────┐    ┌─────────────┐    ┌──────────────┐            │
│  │  FWC    │───▶│ Import/Sync │───▶│ Award Master │            │
│  │ Updates │    │   Awards    │    │    Data      │            │
│  └─────────┘    └─────────────┘    └──────┬───────┘            │
│                                           │                      │
│                                           ▼                      │
│                                    ┌──────────────┐             │
│                                    │   Validate   │             │
│                                    │    Data      │             │
│                                    └──────┬───────┘             │
│                                           │                      │
│                                           ▼                      │
│                                    ┌──────────────┐             │
│                                    │   Publish    │             │
│                                    │  to Tenants  │             │
│                                    └──────────────┘             │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 2. Tenant Onboarding

```
┌─────────────────────────────────────────────────────────────────┐
│           SYSTEM ADMIN: TENANT ONBOARDING WORKFLOW              │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Step 1: Create Tenant                                          │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ • Organization name                                      │   │
│  │ • Industry type (e.g., Retail, Hospitality)              │   │
│  │ • Primary contact                                        │   │
│  │ • Billing information                                    │   │
│  └─────────────────────────────────────────────────────────┘   │
│                           ▼                                      │
│  Step 2: Auto-Suggest Awards                                    │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ System suggests applicable awards based on industry:     │   │
│  │ • Retail → MA000004 (General Retail Industry Award)      │   │
│  │ • Hospitality → MA000009 (Hospitality Industry Award)    │   │
│  │ • Fast Food → MA000003 (Fast Food Industry Award)        │   │
│  └─────────────────────────────────────────────────────────┘   │
│                           ▼                                      │
│  Step 3: Assign Awards                                          │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ • Select applicable award(s)                             │   │
│  │ • Set effective date                                     │   │
│  │ • Configure default classifications                      │   │
│  │ • Enable/disable optional allowances                     │   │
│  └─────────────────────────────────────────────────────────┘   │
│                           ▼                                      │
│  Step 4: Invite Tenant Admin                                    │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ • Send invitation email                                  │   │
│  │ • Set initial permissions                                │   │
│  │ • Provide onboarding documentation                       │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

## Tenant Admin Workflows

### 1. Award Configuration

```
┌─────────────────────────────────────────────────────────────────┐
│          TENANT ADMIN: AWARD CONFIGURATION WORKFLOW             │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌────────────────────────────────────────────────────────┐    │
│  │                    Dashboard                             │    │
│  │  • View assigned awards                                  │    │
│  │  • Current pay rates summary                            │    │
│  │  • Upcoming rate changes                                │    │
│  └───────────────────────────┬────────────────────────────┘    │
│                              │                                   │
│         ┌────────────────────┼────────────────────┐             │
│         ▼                    ▼                    ▼             │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐      │
│  │ Manage       │    │ Configure    │    │ Generate     │      │
│  │ Employees    │    │ Allowances   │    │ Reports      │      │
│  ├──────────────┤    ├──────────────┤    ├──────────────┤      │
│  │ • Assign     │    │ • Enable     │    │ • Pay        │      │
│  │   levels     │    │   meal       │    │   summaries  │      │
│  │ • Set rates  │    │   allowance  │    │ • Compliance │      │
│  │ • Track      │    │ • Configure  │    │ • Audit      │      │
│  │   changes    │    │   first aid  │    │   trails     │      │
│  └──────────────┘    └──────────────┘    └──────────────┘      │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

## API Design

### System Admin Endpoints

```
Award Management:
  GET    /api/v1/awards                    # List all awards
  GET    /api/v1/awards/{code}             # Get award details
  PUT    /api/v1/awards/{code}             # Update award
  POST   /api/v1/awards/sync               # Sync from FWC

Tenant Management:
  GET    /api/v1/tenants                   # List all tenants
  POST   /api/v1/tenants                   # Create tenant
  GET    /api/v1/tenants/{id}              # Get tenant details
  PUT    /api/v1/tenants/{id}              # Update tenant
  DELETE /api/v1/tenants/{id}              # Delete tenant

Award Assignment:
  POST   /api/v1/tenants/{id}/awards       # Assign award to tenant
  GET    /api/v1/tenants/{id}/awards       # List tenant's awards
  DELETE /api/v1/tenants/{id}/awards/{code}# Remove award from tenant
```

### Tenant Admin Endpoints

```
Award Viewing:
  GET    /api/v1/my-awards                 # List assigned awards
  GET    /api/v1/my-awards/{code}          # Get award details
  GET    /api/v1/my-awards/{code}/rates    # Get current pay rates

Configuration:
  GET    /api/v1/configuration             # Get tenant config
  PUT    /api/v1/configuration             # Update tenant config
  GET    /api/v1/configuration/allowances  # Get enabled allowances
  PUT    /api/v1/configuration/allowances  # Update allowances

Employees:
  GET    /api/v1/employees                 # List employees
  POST   /api/v1/employees                 # Add employee
  PUT    /api/v1/employees/{id}/level      # Set employee level
```

## Data Flow

### Award Update Propagation

```
┌────────────────────────────────────────────────────────────────┐
│              AWARD UPDATE PROPAGATION FLOW                      │
├────────────────────────────────────────────────────────────────┤
│                                                                 │
│  1. FWC publishes updated award (e.g., annual pay rise)        │
│                              │                                  │
│                              ▼                                  │
│  2. System Admin imports new data                              │
│     ┌──────────────────────────────────────────────┐           │
│     │ • New classifications                        │           │
│     │ • Updated pay rates                          │           │
│     │ • Changed allowances                         │           │
│     │ • New penalty rates                          │           │
│     └──────────────────────────────────────────────┘           │
│                              │                                  │
│                              ▼                                  │
│  3. Validation                                                  │
│     ┌──────────────────────────────────────────────┐           │
│     │ • Data integrity checks                      │           │
│     │ • Backward compatibility                     │           │
│     │ • Effective date validation                  │           │
│     └──────────────────────────────────────────────┘           │
│                              │                                  │
│                              ▼                                  │
│  4. Publish to tenants                                         │
│     ┌──────────────────────────────────────────────┐           │
│     │ • Notify affected tenants                    │           │
│     │ • Update tenant configurations               │           │
│     │ • Log audit trail                            │           │
│     └──────────────────────────────────────────────┘           │
│                              │                                  │
│                              ▼                                  │
│  5. Tenant Admin receives notification                         │
│     ┌──────────────────────────────────────────────┐           │
│     │ • Review changes                             │           │
│     │ • Acknowledge update                         │           │
│     │ • Update employee records if needed          │           │
│     └──────────────────────────────────────────────┘           │
│                                                                 │
└────────────────────────────────────────────────────────────────┘
```

## Security Considerations

### Access Control Matrix

| Action | System Admin | Tenant Admin | Tenant User |
|--------|:------------:|:------------:|:-----------:|
| View all awards | ✓ | ✗ | ✗ |
| Update awards | ✓ | ✗ | ✗ |
| Create tenants | ✓ | ✗ | ✗ |
| Assign awards to tenants | ✓ | ✗ | ✗ |
| View assigned awards | ✓ | ✓ | ✓ |
| Configure allowances | ✗ | ✓ | ✗ |
| Manage employees | ✗ | ✓ | ✗ |
| View pay rates | ✓ | ✓ | ✓ |
| Generate reports | ✓ | ✓ | ✗ |

### Data Isolation

- **Tenant data isolation**: Each tenant can only access their own data
- **Award versioning**: Historical award data preserved for compliance
- **Audit logging**: All changes tracked with user, timestamp, and details

## Technology Stack Recommendations

### Backend
- **API**: RESTful or GraphQL
- **Database**: PostgreSQL with multi-tenant schema
- **Caching**: Redis for frequently accessed award data
- **Queue**: RabbitMQ/AWS SQS for async notifications

### Frontend
- **Framework**: React, Angular, or Vue.js
- **State Management**: Redux or Zustand
- **UI Library**: Material-UI, Ant Design, or Tailwind CSS

### Infrastructure
- **Cloud**: AWS, Azure, or GCP
- **Containers**: Docker with Kubernetes
- **CI/CD**: GitHub Actions or GitLab CI

## Next Steps

1. **For BA**: Review [BA_USER_STORIES.md](./BA_USER_STORIES.md) for detailed user stories
2. **For UX Designer**: Review [UX_GUIDELINES.md](./UX_GUIDELINES.md) for wireframe suggestions
3. **For Development Team**: Create technical specifications based on this architecture
