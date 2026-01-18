# Fair Work Awards - Rule Builder and Rule Engine

A .NET 8 API framework for managing Australian Fair Work Awards, calculating pay rates, and generating rule combinations for multi-tenant SaaS applications.

## Overview

This system provides a complete solution for:
1. **Rule Builder**: Generates all possible pay rule combinations from awards data
2. **Rule Engine**: Validates and calculates pay rates based on conditions
3. **Multi-tenant Support**: Allows tenants to configure their own award rules

## Architecture

The solution follows Clean Architecture principles:

```
src/
├── FairWorkAwards.Domain/         # Domain entities and business logic
├── FairWorkAwards.Application/    # Application interfaces and DTOs
├── FairWorkAwards.Infrastructure/ # Data access, EF Core, services
└── FairWorkAwards.Api/            # ASP.NET Core Web API

tests/
├── FairWorkAwards.Tests.Unit/        # Unit tests
└── FairWorkAwards.Tests.Integration/ # Integration tests
```

## Key Features

### 1. Rule Builder
- Generates pre-computed pay rules for fast lookups
- Creates all combinations of: Award × Employment Type × Classification × Penalty Rate
- Supports tags for grouping penalties and allowances
- Batch generation for all awards or specific award

### 2. Rule Engine
- Runtime calculation of pay rates based on conditions
- Validation of input parameters
- Support for:
  - Base pay rates
  - Penalty rates (overtime, weekends, holidays)
  - Allowances (expense and wage-based)
  - Custom tags
  - Tenant overrides

### 3. Domain Models

**Core Entities:**
- `Award`: Fair Work Award definition (e.g., MA000004 - Retail)
- `Classification`: Employee levels (1-8) with base rates
- `PenaltyRate`: Additional rates for overtime, weekends, etc.
- `Allowance`: Additional payments (meals, location, etc.)
- `Tag`: Custom groupings of penalties/allowances
- `ComputedPayRule`: Pre-generated rules for fast lookups

**Multi-tenant:**
- `Tenant`: Organization using the system
- `TenantAward`: Award assignment to tenant
- Tenant-specific overrides and configurations

## API Endpoints

### Pay Rate Calculation
```http
POST /api/v1/payrates/calculate
Content-Type: application/json

{
  "awardId": 1,
  "employmentTypeCode": "FT",
  "classificationLevel": 1,
  "allowanceIds": [1, 3],
  "tagIds": [2, 4],
  "tenantId": 123,
  "effectiveDate": "2025-07-01"
}
```

### Rule Generation (Admin)
```http
POST /api/v1/admin/rules/generate/{awardId}?effectiveFrom=2025-07-01

POST /api/v1/admin/rules/regenerate-all?effectiveFrom=2025-07-01
```

## Database Schema

The system uses SQL Server with the following key tables:

- **Awards**: Award definitions
- **Classifications**: Employee classification levels
- **PenaltyRates**: Penalty rate definitions
- **Allowances**: Allowance definitions
- **Tags**: Custom grouping tags
- **ComputedPayRules**: Pre-computed pay rules
- **Tenants**: Tenant organizations
- **TenantAwards**: Award assignments to tenants

## Getting Started

### Prerequisites
- .NET 8 SDK
- SQL Server (LocalDB or full instance)
- Visual Studio 2022 / VS Code / Rider

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/learntocodegpt4/fairworkawards.git
   cd fairworkawards
   ```

2. **Update connection string**
   Edit `src/FairWorkAwards.Api/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FairWorkAwards;Trusted_Connection=True;MultipleActiveResultSets=true"
     }
   }
   ```

3. **Create database** (migrations to be added)
   ```bash
   cd src/FairWorkAwards.Infrastructure
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

4. **Build and run**
   ```bash
   dotnet build
   cd src/FairWorkAwards.Api
   dotnet run
   ```

5. **Access Swagger UI**
   Navigate to: `https://localhost:5001/swagger`

## Usage Examples

### Calculate Pay Rates
Calculate pay rates for a Retail Employee Level 1, full-time, with First Aid allowance:

```csharp
POST /api/v1/payrates/calculate
{
  "awardId": 1,
  "employmentTypeCode": "FT",
  "classificationLevel": 1,
  "allowanceIds": [1],
  "tagIds": [],
  "effectiveDate": "2025-07-01"
}

Response:
{
  "awardCode": "MA000004",
  "awardName": "General Retail Industry Award 2020",
  "employmentType": {
    "code": "FT",
    "name": "Full-time"
  },
  "classification": {
    "level": 1,
    "name": "Retail Employee Level 1"
  },
  "basePay": {
    "hourlyRate": 26.55,
    "weeklyRate": 1008.90,
    "annualRate": 52462.80
  },
  "penaltyRates": [
    {
      "name": "Saturday - First 4 Hours",
      "multiplier": 1.5,
      "hourlyRate": 39.83,
      "weeklyRate": 1513.35
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
      "unit": "per_week",
      "weeklyEquivalent": 13.89
    }
  ],
  "totalAllowancesPerWeek": 13.89,
  "totalWeeklyPay": 1022.79,
  "effectiveDate": "2025-07-01"
}
```

### Generate Rules for an Award
Pre-compute all pay rules for fast lookups:

```http
POST /api/v1/admin/rules/generate/1?effectiveFrom=2025-07-01

Response:
{
  "awardId": 1,
  "generatedRulesCount": 96,
  "effectiveFrom": "2025-07-01",
  "generatedAt": "2025-01-15T19:30:00Z"
}
```

## Configuration

### Employment Types
- **FT** (Full-time): Adult rate, 38 hours/week
- **PT** (Part-time): Adult rate, variable hours
- **CAS** (Casual): Adult rate + 25% loading
- **APP** (Apprentice): Apprentice rates
- **JUN** (Junior): Age-based percentage rates

### Rate Type Codes
- **AD**: Adult (21+ years)
- **JN**: Junior (under 21)
- **AA**: Adult Apprentice
- **AP**: Apprentice

## Testing

Run unit tests:
```bash
dotnet test tests/FairWorkAwards.Tests.Unit
```

Run integration tests:
```bash
dotnet test tests/FairWorkAwards.Tests.Integration
```

## Documentation

- [Architecture](./ARCHITECTURE.md) - System architecture overview
- [BA User Stories](./BA_USER_STORIES.md) - Business requirements
- [Rule Engine Technical Design](./rule-engine-technical-design.md) - Technical specifications
- [System Admin Rules Generator](./systemadmin-rules-generator.md) - Rule generation guide

## References

- [Fair Work Commission](https://www.fwc.gov.au/) - Official FWC website
- [Fair Work Awards](https://www.fairwork.gov.au/employment-conditions/awards) - Award information
- [FWC API Documentation](https://www.fwc.gov.au/awards-and-agreements/awards/modern-awards-database) - Awards database

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes (`git commit -am 'Add my feature'`)
4. Push to the branch (`git push origin feature/my-feature`)
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For issues and questions, please create an issue in the GitHub repository.
