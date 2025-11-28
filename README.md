# Fair Work Awards Data Repository

## Overview

This repository contains extracted data from the Fair Work Commission (FWC) Australia, specifically for the General Retail Industry Award [MA000004]. The data demonstrates the structure and complexity of Australian Fair Work Awards, which classify employees into eight levels based on their skills, duties, and responsibilities, with each level determining the minimum pay rate.

## Data Files

| File | Description |
|------|-------------|
| `All_Awards.csv` | Complete list of 156 Australian Fair Work Awards |
| `MA000004_classifications.csv` | Employee classification levels (1-8) with base pay rates |
| `MA000004_payrates.csv` | Detailed penalty rates for various conditions |
| `MA000004_penalty.csv` | Penalty rate data for overtime, weekends, and holidays |
| `MA000004_allowances.csv` | Expense allowances (meal, motor vehicle, clothing) |
| `MA000004_wage_allowances.csv` | Wage-based allowances (location, disability, first aid) |
| `MA000004_id_or_Code.csv` | Award ID/code reference by year |

## Key Data Points (MA000004 - 2025)

### Classification Levels

| Level | Classification | Hourly Rate | Weekly Rate |
|-------|---------------|-------------|-------------|
| 1 | Retail Employee Level 1 | $26.55 | $1,008.90 |
| 2 | Retail Employee Level 2 | $27.16 | $1,032.00 |
| 3 | Retail Employee Level 3 | $27.58 | $1,048.00 |
| 4 | Retail Employee Level 4 | $28.12 | $1,068.40 |
| 5 | Retail Employee Level 5 | $29.27 | $1,112.30 |
| 6 | Retail Employee Level 6 | $29.70 | $1,128.50 |
| 7 | Retail Employee Level 7 | $31.19 | $1,185.10 |
| 8 | Retail Employee Level 8 | $32.45 | $1,233.20 |

### Penalty Rates (Examples)

| Condition | Rate |
|-----------|------|
| Saturday (first 4 hours) | 150% |
| Saturday (after 4 hours) | 200% |
| Sunday | 200% |
| Public Holiday | 300% |
| Overtime (first 3 hours) | 150% |
| Overtime (after 3 hours) | 200% |

### Allowances

| Allowance | Amount | Frequency |
|-----------|--------|-----------|
| Meal (overtime without notice) | $23.59 | per occasion |
| Motor Vehicle | $0.98 | per km |
| First Aid | $13.89 | per week |
| Liquor Licence | $33.12 | per week |

## System Architecture Documentation

This repository includes comprehensive documentation for building a Fair Work Awards Management System:

### 📋 [Architecture Overview](./docs/ARCHITECTURE.md)
System architecture for managing awards in a multi-tenant SaaS application, including:
- Multi-tenant architecture design
- Role hierarchy (System Admin, Tenant Admin, Tenant User)
- Domain model and entity relationships
- API design
- Data flow diagrams
- Security considerations

### 📝 [BA User Stories](./docs/BA_USER_STORIES.md)
Detailed user stories and acceptance criteria for:
- Award management workflows
- Tenant onboarding process
- Pay rate and allowance viewing
- Employee classification management
- Compliance reporting

### 🎨 [UX Guidelines](./docs/UX_GUIDELINES.md)
UX design specifications including:
- Information architecture
- Wireframe suggestions
- Component specifications
- Accessibility guidelines
- Design system recommendations

### ⚙️ [System Admin Rules Generator](./docs/systemadmin-rules-generator.md)
Technical documentation for rule creation and management:
- Entity relationship diagrams for rule generation
- SQL join examples for combining classifications, penalties, and allowances
- Rule combination matrix (employee types × levels × conditions × allowances)
- Step-by-step rule builder interface wireframes
- API endpoints for rule management and tenant assignment

## Use Cases

This data and documentation can be used for:

1. **HR/Payroll Systems**: Integrating award data for accurate pay calculations
2. **Compliance Tools**: Ensuring businesses meet minimum wage requirements
3. **Award Interpretation**: Understanding complex penalty and allowance structures
4. **Multi-Tenant Platforms**: Building SaaS applications for award management

## Data Source

Data extracted from Fair Work Commission (FWC) Australia. All rates are subject to annual reviews and updates by the FWC.

## License

See [LICENSE](./LICENSE) for details.

## Contributing

For questions about the architecture or data structure, please open an issue.