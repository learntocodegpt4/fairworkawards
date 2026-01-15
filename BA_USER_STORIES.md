# Business Analyst User Stories

## Fair Work Awards Management System

This document contains user stories and acceptance criteria for the Fair Work Awards Management System, focusing on System Admin and Tenant Admin roles.

---

## Epic 1: System Administration - Award Management

### US-1.1: Import Awards from FWC

**As a** System Admin  
**I want to** import award data from the Fair Work Commission  
**So that** the system has the latest award information for all industries

**Acceptance Criteria:**
- [ ] System can connect to FWC data source (API or file import)
- [ ] Import includes all award components (classifications, pay rates, allowances, penalties)
- [ ] System validates data integrity during import
- [ ] Failed imports are logged with error details
- [ ] Successful imports show summary of changes
- [ ] Historical data is preserved (no overwrite of previous versions)

**Business Rules:**
- Awards must maintain version history
- Import should capture effective dates (operative_from, operative_to)
- System should flag awards with upcoming rate changes

**Data Reference:**
Based on repository data, import should handle:
- 156 total awards (All_Awards.csv)
- 380+ classification records per award (MA000004_classifications.csv)
- 7,900+ penalty rate records (MA000004_payrates.csv)

---

### US-1.2: View All Awards

**As a** System Admin  
**I want to** view a list of all Australian Fair Work Awards  
**So that** I can understand the complete award landscape

**Acceptance Criteria:**
- [ ] Display list of all 156 awards in a searchable/filterable table
- [ ] Show key information: Award Code, Name, Operative Date, Version
- [ ] Allow sorting by any column
- [ ] Provide search by award code or name
- [ ] Filter by industry category
- [ ] Show count of tenants using each award

**Sample Data Display:**

| Award Code | Award Name | Industry | Operative From | Tenants |
|------------|------------|----------|----------------|---------|
| MA000001 | Black Coal Mining Industry Award 2020 | Mining | 2010-01-01 | 5 |
| MA000004 | General Retail Industry Award 2020 | Retail | 2010-01-01 | 127 |
| MA000009 | Hospitality Industry (General) Award 2020 | Hospitality | 2010-01-01 | 89 |

---

### US-1.3: View Award Details

**As a** System Admin  
**I want to** view detailed information about a specific award  
**So that** I can understand all components and current rates

**Acceptance Criteria:**
- [ ] Display award header (code, name, effective dates)
- [ ] Show all classification levels with current rates
- [ ] List all applicable allowances (expense and wage)
- [ ] Display penalty rates organized by condition
- [ ] Show version history with change summary
- [ ] Export award details to PDF/Excel

**Screen Sections:**

1. **Classifications Tab**
   - Level, Name, Hourly Rate, Weekly Rate
   - Filter by employee type (AA, AD, AP, JN)

2. **Allowances Tab**
   - Expense allowances (meals, motor vehicle, clothing)
   - Wage allowances (location-based, disability)

3. **Penalty Rates Tab**
   - Weekend rates
   - Overtime rates
   - Public holiday rates
   - Shift allowances

---

### US-1.4: Manually Update Award Data

**As a** System Admin  
**I want to** manually update award data when needed  
**So that** I can correct errors or add missing information

**Acceptance Criteria:**
- [ ] Edit classification rates with effective date
- [ ] Update allowance amounts
- [ ] Modify penalty rates
- [ ] All changes require confirmation
- [ ] Changes are logged with reason
- [ ] Cannot modify locked (finalized) award versions

**Validation Rules:**
- Rates cannot be below minimum wage
- Effective dates cannot be in the past (except corrections)
- Percentage-based allowances must recalculate from base rate

---

## Epic 2: System Administration - Tenant Management

### US-2.1: Create New Tenant

**As a** System Admin  
**I want to** create a new tenant organization  
**So that** a business can start using the awards system

**Acceptance Criteria:**
- [ ] Capture organization details (name, ABN, address)
- [ ] Select primary industry type
- [ ] Auto-suggest applicable awards based on industry
- [ ] Set billing tier/plan
- [ ] Create initial Tenant Admin user
- [ ] Send invitation email to Tenant Admin

**Fields Required:**
- Organization Name (required)
- ABN (optional, validated format)
- Industry Type (required, dropdown)
- Primary Contact Name (required)
- Primary Contact Email (required)
- Phone Number (optional)
- Billing Address (required for paid plans)

**Industry to Award Mapping:**

| Industry Type | Suggested Awards |
|---------------|------------------|
| Retail | MA000004 |
| Hospitality | MA000009 |
| Fast Food | MA000003 |
| Healthcare | MA000027, MA000034 |
| Construction | MA000020 |

---

### US-2.2: Assign Award to Tenant

**As a** System Admin  
**I want to** assign one or more awards to a tenant  
**So that** the tenant can access relevant pay rate information

**Acceptance Criteria:**
- [ ] Search and select from available awards
- [ ] Set effective date for the assignment
- [ ] Configure initial award settings
- [ ] Enable/disable optional components
- [ ] Notify Tenant Admin of new assignment
- [ ] Log the assignment action

**Configuration Options:**
- Which classification levels apply
- Which allowances are active
- Default penalty rate rules
- Custom effective dates

---

### US-2.3: View Tenant List

**As a** System Admin  
**I want to** view all tenants in the system  
**So that** I can manage and support them

**Acceptance Criteria:**
- [ ] Display paginated list of all tenants
- [ ] Show: Name, Industry, Awards Count, Created Date, Status
- [ ] Filter by industry, status, award
- [ ] Search by tenant name
- [ ] Quick actions: View, Edit, Suspend, Delete
- [ ] Export list to CSV

**Status Values:**
- Active
- Suspended
- Trial
- Cancelled

---

### US-2.4: Manage Tenant Settings

**As a** System Admin  
**I want to** update tenant settings and configuration  
**So that** I can help tenants manage their account

**Acceptance Criteria:**
- [ ] Update organization details
- [ ] Change industry type (with award re-assignment prompt)
- [ ] Add/remove assigned awards
- [ ] Suspend/reactivate tenant
- [ ] Reset Tenant Admin password
- [ ] View tenant activity log

---

## Epic 3: Tenant Administration - Award Management

### US-3.1: View Assigned Awards

**As a** Tenant Admin  
**I want to** view awards assigned to my organization  
**So that** I understand what pay rules apply to us

**Acceptance Criteria:**
- [ ] Display list of assigned awards
- [ ] Show current version and effective dates
- [ ] Indicate if updates are pending
- [ ] Provide quick access to rate tables
- [ ] Show last updated date

**Dashboard Summary:**
```
Your Organization: ABC Retail Pty Ltd
Industry: Retail

Assigned Awards:
┌─────────────────────────────────────────────────────────┐
│ MA000004 - General Retail Industry Award 2020          │
│ Effective: 2010-01-01 | Version: 2025 | Last Updated: Jul 2025 │
│ Classifications: 8 levels | Employees: 45              │
│ [View Rates] [View Allowances] [Configure]             │
└─────────────────────────────────────────────────────────┘
```

---

### US-3.2: View Pay Rates

**As a** Tenant Admin  
**I want to** view current pay rates for our award  
**So that** I can ensure we are paying correctly

**Acceptance Criteria:**
- [ ] Display all classification levels
- [ ] Show hourly and weekly rates
- [ ] Filter by employee type (Adult, Junior, Apprentice)
- [ ] Compare current vs upcoming rates
- [ ] Export rate table to PDF/Excel
- [ ] Print-friendly format

**Rate Table Display (Based on MA000004 data):**

| Classification | Level | Hourly Rate | Weekly Rate |
|----------------|-------|-------------|-------------|
| Retail Employee Level 1 | 1 | $26.55 | $1,008.90 |
| Retail Employee Level 2 | 2 | $27.16 | $1,032.00 |
| Retail Employee Level 3 | 3 | $27.58 | $1,048.00 |
| Retail Employee Level 4 | 4 | $28.12 | $1,068.40 |
| Retail Employee Level 5 | 5 | $29.27 | $1,112.30 |
| Retail Employee Level 6 | 6 | $29.70 | $1,128.50 |
| Retail Employee Level 7 | 7 | $31.19 | $1,185.10 |
| Retail Employee Level 8 | 8 | $32.45 | $1,233.20 |

---

### US-3.3: View Penalty Rates

**As a** Tenant Admin  
**I want to** view penalty rates for overtime, weekends, and public holidays  
**So that** I can calculate correct pay for non-standard hours

**Acceptance Criteria:**
- [ ] Display penalty rates grouped by condition
- [ ] Show rate as percentage and calculated value
- [ ] Filter by day type (weekday, Saturday, Sunday, public holiday)
- [ ] Filter by shift type (day, afternoon, night)
- [ ] Show overtime multipliers
- [ ] Calculate example pay for given hours

**Penalty Rate Categories (from MA000004_penalty.csv):**
- Ordinary hours
- Saturday (first 4 hours, after 4 hours)
- Sunday
- Public Holiday
- Overtime (first 3 hours, after 3 hours)
- Shift work (afternoon, rotating night, permanent night)

---

### US-3.4: View Allowances

**As a** Tenant Admin  
**I want to** view applicable allowances  
**So that** I can include them in employee pay calculations

**Acceptance Criteria:**
- [ ] Display expense allowances (meal, vehicle, clothing)
- [ ] Display wage allowances (location, disability)
- [ ] Show current amounts and frequency
- [ ] Indicate which allowances are enabled for our organization
- [ ] Provide calculation examples

**Expense Allowances (MA000004_allowances.csv):**

| Allowance | Amount | Frequency | Clause |
|-----------|--------|-----------|--------|
| Meal - overtime > 1hr without notice | $23.59 | per occasion | 19.2(b)(i) |
| Meal - further 4 hours overtime | $21.39 | per occasion | 19.2(c) |
| Motor vehicle | $0.98 | per km | 19.7 |
| Special clothing - full-time | $6.25 | per week | 19.3(c)(i) |
| Special clothing - part-time/casual | $1.25 | per shift | 19.3(c)(ii) |

**Wage Allowances (MA000004_wage_allowances.csv):**

| Allowance | Rate | Amount | Clause |
|-----------|------|--------|--------|
| Broken Hill | 4.28% | $1.20/hr | 19.13 |
| Cold work disability | 1.3% | $0.37/hr | 19.9(b) |
| Cold work - below 0°C | 2% | $0.56/hr | 19.9(c) |
| First aid allowance | 1.3% | $13.89/wk | 19.10(b) |
| Liquor licence | 3.1% | $33.12/wk | 19.12 |

---

### US-3.5: Configure Award Settings

**As a** Tenant Admin  
**I want to** configure which award components apply to my organization  
**So that** we only see relevant rates and allowances

**Acceptance Criteria:**
- [ ] Enable/disable specific allowances
- [ ] Select applicable classification levels
- [ ] Set organization-specific defaults
- [ ] Changes take effect from specified date
- [ ] All changes are logged

**Configuration Options:**
- Active classification levels (e.g., only using Levels 1-5)
- Enabled allowances (e.g., First Aid, but not Broken Hill)
- Default shift patterns
- Public holiday calendar selection

---

## Epic 4: Tenant Administration - Employee Management

### US-4.1: Assign Employee Classification

**As a** Tenant Admin  
**I want to** assign a classification level to an employee  
**So that** they are paid correctly according to the award

**Acceptance Criteria:**
- [ ] Select employee from list
- [ ] Choose classification level
- [ ] Set effective date
- [ ] System validates against award rules
- [ ] Employee notified of classification
- [ ] Historical classifications retained

**Validation Rules:**
- Junior rates only apply to employees under 21
- Apprentice rates require active apprenticeship
- Classification changes require reason

---

### US-4.2: Calculate Employee Pay

**As a** Tenant Admin  
**I want to** calculate an employee's pay based on hours worked  
**So that** I can ensure accurate payroll

**Acceptance Criteria:**
- [ ] Enter hours by day type (ordinary, overtime, weekend)
- [ ] System applies correct rates automatically
- [ ] Include applicable allowances
- [ ] Show itemized breakdown
- [ ] Calculate super contribution
- [ ] Export calculation to payroll system

**Calculation Example:**

```
Employee: John Smith
Classification: Retail Employee Level 3
Week: 1-7 July 2025

Hours Worked:
├── Monday-Friday: 38 hours @ $27.58/hr = $1,048.04
├── Saturday (first 4 hrs): 4 hours @ $41.37/hr = $165.48
├── Saturday (after 4 hrs): 2 hours @ $55.16/hr = $110.32
└── Public Holiday: 8 hours @ $82.74/hr = $661.92

Allowances:
└── First Aid Allowance: $13.89

Gross Pay: $1,999.65
Super (11.5%): $229.96
```

---

## Epic 5: Reporting & Compliance

### US-5.1: Generate Pay Rate Report

**As a** Tenant Admin  
**I want to** generate a report of current pay rates  
**So that** I can share it with managers and HR

**Acceptance Criteria:**
- [ ] Select report date (current or future)
- [ ] Choose classification levels to include
- [ ] Include/exclude allowances
- [ ] Export to PDF, Excel, or print
- [ ] Brand report with organization logo

---

### US-5.2: View Award Update History

**As a** Tenant Admin  
**I want to** view history of award changes  
**So that** I can understand how rates have changed over time

**Acceptance Criteria:**
- [ ] Display timeline of award updates
- [ ] Show what changed in each update
- [ ] Compare any two versions
- [ ] Filter by date range
- [ ] Export history to PDF

**Historical Data (from MA000004_id_or_Code.csv):**

| Year | Version | Effective From |
|------|---------|----------------|
| 2025 | 2 | 2025-07-01 |
| 2024 | 2 | 2024-07-01 |
| 2023 | 2 | 2023-07-01 |
| 2022 | 2 | 2022-07-01 |
| 2021 | 2 | 2021-09-01 |

---

### US-5.3: Compliance Audit Report

**As a** Tenant Admin  
**I want to** generate a compliance audit report  
**So that** I can demonstrate we are paying according to the award

**Acceptance Criteria:**
- [ ] Show all employees and their classifications
- [ ] Verify rates meet or exceed award minimums
- [ ] Flag any potential underpayments
- [ ] Include allowance compliance
- [ ] Generate dated certification

---

## Epic 6: Notifications & Alerts

### US-6.1: Award Update Notification

**As a** Tenant Admin  
**I want to** receive notifications when award rates change  
**So that** I can update our payroll in time

**Acceptance Criteria:**
- [ ] Email notification sent when rates change
- [ ] In-app notification displayed on login
- [ ] Summary of changes included
- [ ] Link to view detailed changes
- [ ] Acknowledge receipt option

**Notification Content:**
```
Subject: Award Update - MA000004 Effective 1 July 2025

Dear ABC Retail Admin,

The General Retail Industry Award 2020 (MA000004) has been 
updated with new rates effective 1 July 2025.

Summary of Changes:
- Classification rates increased by 3.5%
- Meal allowance increased from $22.99 to $23.59
- Motor vehicle allowance unchanged

Please review and update your payroll systems.

[View Full Changes]
```

---

### US-6.2: Upcoming Changes Dashboard

**As a** Tenant Admin  
**I want to** see upcoming award changes on my dashboard  
**So that** I can prepare in advance

**Acceptance Criteria:**
- [ ] Display changes effective in next 90 days
- [ ] Show countdown to effective date
- [ ] Highlight significant changes (>3% increase)
- [ ] Provide comparison tool
- [ ] Link to preparation checklist

---

## Non-Functional Requirements

### NFR-1: Performance
- Award data should load within 2 seconds
- Reports should generate within 10 seconds
- System should support 1000+ concurrent users

### NFR-2: Security
- Role-based access control (RBAC)
- Tenant data isolation
- All data encrypted at rest and in transit
- Audit logging for all changes

### NFR-3: Availability
- 99.9% uptime SLA
- Automatic failover
- Daily backups retained for 7 years (compliance)

### NFR-4: Compliance
- Data retained for 7 years (Fair Work Act requirement)
- Audit trail for all pay rate access
- Export capability for legal discovery

---

## Glossary

| Term | Definition |
|------|------------|
| **Award** | A legal document setting minimum employment conditions |
| **Classification** | Employee category determining minimum pay rate |
| **Penalty Rate** | Additional pay for overtime, weekends, or holidays |
| **Allowance** | Additional payment for specific conditions or expenses |
| **FWC** | Fair Work Commission - Australia's workplace tribunal |
| **Tenant** | An organization using the platform |
| **System Admin** | Platform administrator managing all awards and tenants |
| **Tenant Admin** | Organization administrator managing their own award configuration |
