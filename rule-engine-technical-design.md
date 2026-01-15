# Rule Engine Technical Design

## Overview

This document provides a technical solution for building a Rule Engine that generates all pay rate combinations based on 5 dropdown conditions. The solution covers database schema design for MSSQL, rule generation strategies (runtime vs pre-computed), and the Tenant Admin pay rate summary interface.

---

## 5 Condition Dropdowns

The system generates pay rules based on these 5 hierarchical conditions:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     5 CONDITION DROPDOWNS (FILTERS)                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  1. INDUSTRY AWARD                                                           │
│     ┌────────────────────────────────────────────────────────────────────┐  │
│     │ [General Retail Industry Award MA000004 ▼]                         │  │
│     │  • Hospitality Industry Award MA000009                             │  │
│     │  • Fast Food Industry Award MA000003                               │  │
│     │  • Health Professionals Award MA000027                             │  │
│     └────────────────────────────────────────────────────────────────────┘  │
│                               ▼                                              │
│  2. EMPLOYMENT TYPE                                                          │
│     ┌────────────────────────────────────────────────────────────────────┐  │
│     │ [Full-time ▼]                                                      │  │
│     │  • Part-time                                                       │  │
│     │  • Casual                                                          │  │
│     │  • Apprentice                                                      │  │
│     │  • Junior                                                          │  │
│     └────────────────────────────────────────────────────────────────────┘  │
│                               ▼                                              │
│  3. CLASSIFICATION / LEVEL                                                   │
│     ┌────────────────────────────────────────────────────────────────────┐  │
│     │ [Level 1 - Retail Employee ▼]                                      │  │
│     │  • Level 2 - Retail Employee                                       │  │
│     │  • Level 3 - Retail Employee                                       │  │
│     │  • Level 4 - Supervisor                                            │  │
│     │  • Level 5 - Department Manager                                    │  │
│     │  ... up to Level 8                                                 │  │
│     └────────────────────────────────────────────────────────────────────┘  │
│                               ▼                                              │
│  4. ALLOWANCES (Multi-Select)                                                │
│     ┌────────────────────────────────────────────────────────────────────┐  │
│     │ ☑ First Aid Allowance ($13.89/week)                                │  │
│     │ ☑ Cold Work Allowance ($0.37/hour)                                 │  │
│     │ ☐ Meal Allowance ($23.59/occasion)                                 │  │
│     │ ☐ Motor Vehicle ($0.98/km)                                         │  │
│     │ ☐ Liquor Licence ($33.12/week)                                     │  │
│     │ ☐ Broken Hill Location ($1.20/hour)                                │  │
│     └────────────────────────────────────────────────────────────────────┘  │
│                               ▼                                              │
│  5. ADDITIONAL TAGS (Multi-Select, Created by System Admin)                 │
│     ┌────────────────────────────────────────────────────────────────────┐  │
│     │ ☑ Shift Worker                                                     │  │
│     │ ☑ Weekend Roster                                                   │  │
│     │ ☐ Night Shift                                                      │  │
│     │ ☐ Public Holiday Eligible                                          │  │
│     │ ☐ Overtime Eligible                                                │  │
│     │ + [Add Custom Tag]                                                 │  │
│     └────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## MSSQL Database Schema

### Core Tables

```sql
-- =====================================================
-- MASTER DATA TABLES (Managed by System Admin)
-- =====================================================

-- Awards Master Table
CREATE TABLE dbo.Awards (
    AwardId INT IDENTITY(1,1) PRIMARY KEY,
    AwardCode VARCHAR(20) NOT NULL UNIQUE,        -- e.g., 'MA000004'
    AwardName NVARCHAR(200) NOT NULL,              -- e.g., 'General Retail Industry Award 2020'
    IndustryId INT NOT NULL,
    OperativeFrom DATE NOT NULL,
    OperativeTo DATE NULL,
    VersionNumber INT NOT NULL DEFAULT 1,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Awards_Industry FOREIGN KEY (IndustryId) REFERENCES dbo.Industries(IndustryId)
);

-- Employment Types
CREATE TABLE dbo.EmploymentTypes (
    EmploymentTypeId INT IDENTITY(1,1) PRIMARY KEY,
    Code VARCHAR(10) NOT NULL UNIQUE,              -- 'FT', 'PT', 'CAS', 'APP', 'JUN'
    Name NVARCHAR(50) NOT NULL,                    -- 'Full-time', 'Part-time', etc.
    RateTypeCode VARCHAR(5) NOT NULL,              -- 'AD', 'JN', 'AA', 'AP'
    CasualLoadingPercent DECIMAL(5,2) NULL,        -- 25% for casuals
    Description NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

-- Classifications/Levels
CREATE TABLE dbo.Classifications (
    ClassificationId INT IDENTITY(1,1) PRIMARY KEY,
    AwardId INT NOT NULL,
    ClassificationLevel INT NOT NULL,              -- 1-8
    ClassificationName NVARCHAR(200) NOT NULL,     -- 'Retail Employee Level 1'
    EmploymentTypeId INT NOT NULL,
    BaseHourlyRate DECIMAL(10,4) NOT NULL,
    BaseWeeklyRate DECIMAL(10,2) NOT NULL,
    BaseAnnualRate DECIMAL(12,2) NULL,
    OperativeFrom DATE NOT NULL,
    OperativeTo DATE NULL,
    VersionNumber INT NOT NULL DEFAULT 1,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Classifications_Award FOREIGN KEY (AwardId) REFERENCES dbo.Awards(AwardId),
    CONSTRAINT FK_Classifications_EmpType FOREIGN KEY (EmploymentTypeId) REFERENCES dbo.EmploymentTypes(EmploymentTypeId),
    CONSTRAINT UQ_Classification UNIQUE (AwardId, ClassificationLevel, EmploymentTypeId, OperativeFrom)
);

-- Allowances
CREATE TABLE dbo.Allowances (
    AllowanceId INT IDENTITY(1,1) PRIMARY KEY,
    AwardId INT NOT NULL,
    AllowanceCode VARCHAR(50) NOT NULL,
    AllowanceName NVARCHAR(200) NOT NULL,
    AllowanceType VARCHAR(20) NOT NULL,            -- 'EXPENSE', 'WAGE', 'FLAT'
    Amount DECIMAL(10,4) NOT NULL,
    Unit VARCHAR(20) NOT NULL,                     -- 'per_hour', 'per_week', 'per_occasion', 'per_km'
    RatePercent DECIMAL(5,2) NULL,                 -- For wage-based allowances
    IsAllPurpose BIT NOT NULL DEFAULT 0,
    ClauseReference VARCHAR(50) NULL,              -- '19.10(b)'
    OperativeFrom DATE NOT NULL,
    OperativeTo DATE NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Allowances_Award FOREIGN KEY (AwardId) REFERENCES dbo.Awards(AwardId)
);

-- Penalty Rates
CREATE TABLE dbo.PenaltyRates (
    PenaltyRateId INT IDENTITY(1,1) PRIMARY KEY,
    AwardId INT NOT NULL,
    PenaltyCode VARCHAR(50) NOT NULL,
    PenaltyName NVARCHAR(200) NOT NULL,            -- 'Saturday First 4 Hours'
    PenaltyCategory VARCHAR(50) NOT NULL,          -- 'WEEKEND', 'OVERTIME', 'SHIFT', 'HOLIDAY'
    RateMultiplier DECIMAL(5,2) NOT NULL,          -- 1.5, 2.0, 2.5, 3.0
    ApplicableDays VARCHAR(50) NULL,               -- 'SAT', 'SUN', 'MON-FRI'
    ApplicableHours VARCHAR(50) NULL,              -- 'FIRST_4', 'AFTER_4', 'ALL'
    ClauseReference VARCHAR(50) NULL,
    OperativeFrom DATE NOT NULL,
    OperativeTo DATE NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_PenaltyRates_Award FOREIGN KEY (AwardId) REFERENCES dbo.Awards(AwardId)
);

-- Tags (Created by System Admin)
CREATE TABLE dbo.Tags (
    TagId INT IDENTITY(1,1) PRIMARY KEY,
    TagCode VARCHAR(50) NOT NULL UNIQUE,
    TagName NVARCHAR(100) NOT NULL,
    TagCategory VARCHAR(50) NOT NULL,              -- 'SHIFT', 'ROSTER', 'ELIGIBILITY'
    Description NVARCHAR(500) NULL,
    AffectsPenalties BIT NOT NULL DEFAULT 0,
    AffectsAllowances BIT NOT NULL DEFAULT 0,
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsActive BIT NOT NULL DEFAULT 1
);

-- Tag to Penalty/Allowance Mappings
CREATE TABLE dbo.TagPenaltyMappings (
    TagId INT NOT NULL,
    PenaltyRateId INT NOT NULL,
    PRIMARY KEY (TagId, PenaltyRateId),
    CONSTRAINT FK_TagPenalty_Tag FOREIGN KEY (TagId) REFERENCES dbo.Tags(TagId),
    CONSTRAINT FK_TagPenalty_Penalty FOREIGN KEY (PenaltyRateId) REFERENCES dbo.PenaltyRates(PenaltyRateId)
);

CREATE TABLE dbo.TagAllowanceMappings (
    TagId INT NOT NULL,
    AllowanceId INT NOT NULL,
    PRIMARY KEY (TagId, AllowanceId),
    CONSTRAINT FK_TagAllowance_Tag FOREIGN KEY (TagId) REFERENCES dbo.Tags(TagId),
    CONSTRAINT FK_TagAllowance_Allowance FOREIGN KEY (AllowanceId) REFERENCES dbo.Allowances(AllowanceId)
);
```

### Pre-Computed Rules Table

```sql
-- =====================================================
-- PRE-COMPUTED RULES TABLE (Generated by Rule Engine)
-- =====================================================

CREATE TABLE dbo.ComputedPayRules (
    RuleId BIGINT IDENTITY(1,1) PRIMARY KEY,
    
    -- 5 Condition Keys (Composite)
    AwardId INT NOT NULL,
    EmploymentTypeId INT NOT NULL,
    ClassificationId INT NOT NULL,
    PenaltyRateId INT NULL,                        -- NULL for base rates
    
    -- Rule Details
    RuleCode AS CONCAT(
        'R-', AwardId, '-', EmploymentTypeId, '-', 
        ClassificationId, '-', ISNULL(PenaltyRateId, 0)
    ) PERSISTED,
    
    -- Calculated Values
    BaseHourlyRate DECIMAL(10,4) NOT NULL,
    PenaltyMultiplier DECIMAL(5,2) NOT NULL DEFAULT 1.00,
    CalculatedHourlyRate AS (BaseHourlyRate * PenaltyMultiplier) PERSISTED,
    CalculatedWeeklyRate AS (BaseHourlyRate * PenaltyMultiplier * 38) PERSISTED,
    CalculatedAnnualRate AS (BaseHourlyRate * PenaltyMultiplier * 38 * 52) PERSISTED,
    
    -- Effective Dates
    EffectiveFrom DATE NOT NULL,
    EffectiveTo DATE NULL,
    
    -- Audit
    GeneratedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    GeneratedBy VARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Foreign Keys
    CONSTRAINT FK_ComputedRules_Award FOREIGN KEY (AwardId) REFERENCES dbo.Awards(AwardId),
    CONSTRAINT FK_ComputedRules_EmpType FOREIGN KEY (EmploymentTypeId) REFERENCES dbo.EmploymentTypes(EmploymentTypeId),
    CONSTRAINT FK_ComputedRules_Classification FOREIGN KEY (ClassificationId) REFERENCES dbo.Classifications(ClassificationId),
    CONSTRAINT FK_ComputedRules_Penalty FOREIGN KEY (PenaltyRateId) REFERENCES dbo.PenaltyRates(PenaltyRateId)
);

-- Allowances linked to rules (Many-to-Many)
CREATE TABLE dbo.ComputedRuleAllowances (
    RuleId BIGINT NOT NULL,
    AllowanceId INT NOT NULL,
    AllowanceAmount DECIMAL(10,4) NOT NULL,
    PRIMARY KEY (RuleId, AllowanceId),
    CONSTRAINT FK_RuleAllowances_Rule FOREIGN KEY (RuleId) REFERENCES dbo.ComputedPayRules(RuleId),
    CONSTRAINT FK_RuleAllowances_Allowance FOREIGN KEY (AllowanceId) REFERENCES dbo.Allowances(AllowanceId)
);

-- Tags linked to rules (Many-to-Many)
CREATE TABLE dbo.ComputedRuleTags (
    RuleId BIGINT NOT NULL,
    TagId INT NOT NULL,
    PRIMARY KEY (RuleId, TagId),
    CONSTRAINT FK_RuleTags_Rule FOREIGN KEY (RuleId) REFERENCES dbo.ComputedPayRules(RuleId),
    CONSTRAINT FK_RuleTags_Tag FOREIGN KEY (TagId) REFERENCES dbo.Tags(TagId)
);

-- Indexes for fast lookups
CREATE NONCLUSTERED INDEX IX_ComputedRules_Lookup 
ON dbo.ComputedPayRules (AwardId, EmploymentTypeId, ClassificationId, EffectiveFrom)
INCLUDE (CalculatedHourlyRate, CalculatedWeeklyRate);

CREATE NONCLUSTERED INDEX IX_ComputedRules_Active 
ON dbo.ComputedPayRules (IsActive, EffectiveFrom, EffectiveTo)
WHERE IsActive = 1;
```

### Tenant Overrides Table

```sql
-- =====================================================
-- TENANT OVERRIDES (Optional Higher Rates)
-- =====================================================

CREATE TABLE dbo.TenantPayOverrides (
    OverrideId INT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    RuleId BIGINT NOT NULL,
    
    -- Override Values (NULL = use computed value)
    OverrideHourlyRate DECIMAL(10,4) NULL,
    OverrideWeeklyRate DECIMAL(10,2) NULL,
    OverrideAnnualRate DECIMAL(12,2) NULL,
    
    -- Must be >= award minimum
    CONSTRAINT CK_Override_MinHourly CHECK (
        OverrideHourlyRate IS NULL OR 
        OverrideHourlyRate >= (SELECT CalculatedHourlyRate FROM dbo.ComputedPayRules WHERE RuleId = TenantPayOverrides.RuleId)
    ),
    
    -- Audit
    EffectiveFrom DATE NOT NULL,
    EffectiveTo DATE NULL,
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ApprovedBy INT NULL,
    ApprovedAt DATETIME2 NULL,
    
    CONSTRAINT FK_TenantOverrides_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
    CONSTRAINT FK_TenantOverrides_Rule FOREIGN KEY (RuleId) REFERENCES dbo.ComputedPayRules(RuleId)
);
```

---

## Rule Generation Strategies

### Strategy 1: Pre-Computed Rules (Recommended for MSSQL)

Generate all rule combinations when data is imported and store in `ComputedPayRules` table.

```sql
-- =====================================================
-- STORED PROCEDURE: Generate All Rules for an Award
-- =====================================================

CREATE PROCEDURE dbo.sp_GeneratePayRules
    @AwardId INT,
    @EffectiveFrom DATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Delete existing rules for this award/date (if regenerating)
    DELETE FROM dbo.ComputedPayRules 
    WHERE AwardId = @AwardId 
      AND EffectiveFrom = @EffectiveFrom;
    
    -- Generate all combinations: Classification × Employment Type × Penalty Rates
    INSERT INTO dbo.ComputedPayRules (
        AwardId,
        EmploymentTypeId,
        ClassificationId,
        PenaltyRateId,
        BaseHourlyRate,
        PenaltyMultiplier,
        EffectiveFrom
    )
    SELECT 
        c.AwardId,
        c.EmploymentTypeId,
        c.ClassificationId,
        pr.PenaltyRateId,
        c.BaseHourlyRate,
        ISNULL(pr.RateMultiplier, 1.00) AS PenaltyMultiplier,
        @EffectiveFrom
    FROM dbo.Classifications c
    CROSS JOIN dbo.PenaltyRates pr
    WHERE c.AwardId = @AwardId
      AND c.IsActive = 1
      AND pr.AwardId = @AwardId
      AND pr.IsActive = 1
      AND c.OperativeFrom <= @EffectiveFrom
      AND (c.OperativeTo IS NULL OR c.OperativeTo > @EffectiveFrom)
    
    UNION ALL
    
    -- Add base rate rules (no penalty)
    SELECT 
        c.AwardId,
        c.EmploymentTypeId,
        c.ClassificationId,
        NULL AS PenaltyRateId,
        c.BaseHourlyRate,
        1.00 AS PenaltyMultiplier,
        @EffectiveFrom
    FROM dbo.Classifications c
    WHERE c.AwardId = @AwardId
      AND c.IsActive = 1
      AND c.OperativeFrom <= @EffectiveFrom
      AND (c.OperativeTo IS NULL OR c.OperativeTo > @EffectiveFrom);
    
    -- Return count of generated rules
    SELECT @@ROWCOUNT AS GeneratedRulesCount;
END;
GO
```

### Strategy 2: Runtime Generation (Dynamic Query)

Generate rules on-demand based on selected dropdown values.

```sql
-- =====================================================
-- STORED PROCEDURE: Get Pay Rates for Selected Conditions
-- =====================================================

CREATE PROCEDURE dbo.sp_GetPayRatesByConditions
    @AwardId INT,
    @EmploymentTypeCode VARCHAR(10),
    @ClassificationLevel INT,
    @AllowanceIds dbo.IntListType READONLY,        -- Table-valued parameter
    @TagIds dbo.IntListType READONLY,              -- Table-valued parameter
    @TenantId INT = NULL,
    @EffectiveDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SET @EffectiveDate = ISNULL(@EffectiveDate, GETDATE());
    
    -- Get base classification rate
    DECLARE @BaseRate DECIMAL(10,4);
    DECLARE @ClassificationId INT;
    DECLARE @EmploymentTypeId INT;
    
    SELECT 
        @BaseRate = c.BaseHourlyRate,
        @ClassificationId = c.ClassificationId,
        @EmploymentTypeId = c.EmploymentTypeId
    FROM dbo.Classifications c
    INNER JOIN dbo.EmploymentTypes et ON c.EmploymentTypeId = et.EmploymentTypeId
    WHERE c.AwardId = @AwardId
      AND c.ClassificationLevel = @ClassificationLevel
      AND et.Code = @EmploymentTypeCode
      AND c.OperativeFrom <= @EffectiveDate
      AND (c.OperativeTo IS NULL OR c.OperativeTo > @EffectiveDate)
      AND c.IsActive = 1;
    
    -- Build result set with all penalty combinations
    SELECT 
        a.AwardCode,
        a.AwardName,
        et.Name AS EmploymentType,
        c.ClassificationName,
        c.ClassificationLevel,
        
        -- Base Rates
        @BaseRate AS BaseHourlyRate,
        @BaseRate * 38 AS BaseWeeklyRate,
        @BaseRate * 38 * 52 AS BaseAnnualRate,
        
        -- Penalty Rates
        pr.PenaltyName,
        pr.PenaltyCategory,
        pr.RateMultiplier,
        @BaseRate * pr.RateMultiplier AS PenaltyHourlyRate,
        @BaseRate * pr.RateMultiplier * 38 AS PenaltyWeeklyRate,
        
        -- Tenant Override (if applicable)
        tpo.OverrideHourlyRate,
        COALESCE(tpo.OverrideHourlyRate, @BaseRate * pr.RateMultiplier) AS EffectiveHourlyRate,
        
        -- Applicable Allowances
        (
            SELECT STRING_AGG(al.AllowanceName + ': $' + CAST(al.Amount AS VARCHAR) + '/' + al.Unit, ', ')
            FROM dbo.Allowances al
            INNER JOIN @AllowanceIds aids ON al.AllowanceId = aids.Id
            WHERE al.AwardId = @AwardId AND al.IsActive = 1
        ) AS SelectedAllowances,
        
        -- Total Allowance Value (per hour)
        (
            SELECT SUM(CASE 
                WHEN al.Unit = 'per_hour' THEN al.Amount
                WHEN al.Unit = 'per_week' THEN al.Amount / 38
                ELSE 0 
            END)
            FROM dbo.Allowances al
            INNER JOIN @AllowanceIds aids ON al.AllowanceId = aids.Id
            WHERE al.AwardId = @AwardId AND al.IsActive = 1
        ) AS TotalAllowancePerHour,
        
        -- Applicable Tags
        (
            SELECT STRING_AGG(t.TagName, ', ')
            FROM dbo.Tags t
            INNER JOIN @TagIds tids ON t.TagId = tids.Id
            WHERE t.IsActive = 1
        ) AS AppliedTags
        
    FROM dbo.Awards a
    INNER JOIN dbo.Classifications c ON a.AwardId = c.AwardId
    INNER JOIN dbo.EmploymentTypes et ON c.EmploymentTypeId = et.EmploymentTypeId
    LEFT JOIN dbo.PenaltyRates pr ON a.AwardId = pr.AwardId AND pr.IsActive = 1
    LEFT JOIN dbo.TenantPayOverrides tpo ON tpo.TenantId = @TenantId 
        AND tpo.RuleId IN (
            SELECT RuleId FROM dbo.ComputedPayRules 
            WHERE ClassificationId = @ClassificationId 
              AND PenaltyRateId = pr.PenaltyRateId
        )
        AND tpo.EffectiveFrom <= @EffectiveDate
        AND (tpo.EffectiveTo IS NULL OR tpo.EffectiveTo > @EffectiveDate)
    WHERE a.AwardId = @AwardId
      AND c.ClassificationId = @ClassificationId
      AND pr.OperativeFrom <= @EffectiveDate
      AND (pr.OperativeTo IS NULL OR pr.OperativeTo > @EffectiveDate)
    ORDER BY pr.PenaltyCategory, pr.RateMultiplier;
END;
GO
```

---

## System Admin Rule Configuration Interface

### Tag Management

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  SYSTEM ADMIN: Tag Management                                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Tags allow you to group penalty rates and allowances together.             │
│  Tenant Admin can select applicable tags for their employees.               │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ + Create New Tag                                                    │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  EXISTING TAGS:                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Tag Name       │ Category     │ Linked Penalties    │ Linked Allow. │   │
│  ├────────────────┼──────────────┼─────────────────────┼───────────────┤   │
│  │ Shift Worker   │ SHIFT        │ Afternoon Shift,    │ -             │   │
│  │                │              │ Night Shift         │               │   │
│  ├────────────────┼──────────────┼─────────────────────┼───────────────┤   │
│  │ Weekend Roster │ ROSTER       │ Saturday, Sunday    │ -             │   │
│  ├────────────────┼──────────────┼─────────────────────┼───────────────┤   │
│  │ Cold Storage   │ ELIGIBILITY  │ -                   │ Cold Work,    │   │
│  │                │              │                     │ Cold Work <0° │   │
│  ├────────────────┼──────────────┼─────────────────────┼───────────────┤   │
│  │ First Aider    │ ELIGIBILITY  │ -                   │ First Aid     │   │
│  ├────────────────┼──────────────┼─────────────────────┼───────────────┤   │
│  │ Overtime OK    │ ELIGIBILITY  │ OT First 3hrs,      │ Meal          │   │
│  │                │              │ OT After 3hrs       │ Allowance     │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  [Edit] [Delete] [Duplicate]                                                │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Create Tag Wizard

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  CREATE NEW TAG                                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Tag Name: [Night Worker                    ]                               │
│  Tag Code: [NIGHT_WORKER] (auto-generated)                                  │
│  Category: [SHIFT ▼]                                                        │
│                                                                              │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                              │
│  LINK PENALTY RATES (these will apply when tag is selected):                │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ ☑ Permanent Night Shift (130%)                                      │   │
│  │ ☑ Rotating Night Shift (130%)                                       │   │
│  │ ☐ Saturday First 4 Hours (150%)                                     │   │
│  │ ☐ Saturday After 4 Hours (200%)                                     │   │
│  │ ☐ Sunday (200%)                                                     │   │
│  │ ☐ Public Holiday (300%)                                             │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  LINK ALLOWANCES (these will apply when tag is selected):                   │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ ☑ Meal Allowance - overtime without notice ($23.59/occasion)        │   │
│  │ ☐ Motor Vehicle ($0.98/km)                                          │   │
│  │ ☐ First Aid ($13.89/week)                                           │   │
│  │ ☐ Cold Work ($0.37/hour)                                            │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  [Cancel]                                                    [Create Tag]   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Tenant Admin Pay Rate Summary Interface

When Tenant Admin selects the 5 conditions, the system displays a comprehensive pay rate summary:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TENANT ADMIN: Pay Rate Calculator                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  SELECT CONDITIONS:                                                          │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  1. Award:           [General Retail Industry MA000004 ▼]           │   │
│  │  2. Employment Type: [Full-time ▼]                                  │   │
│  │  3. Level:           [Level 1 - Retail Employee ▼]                  │   │
│  │  4. Allowances:      [✓ First Aid] [✓ Cold Work] [+ Add More]       │   │
│  │  5. Tags:            [✓ Shift Worker] [✓ Weekend Roster]            │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  ═══════════════════════════════════════════════════════════════════════   │
│                                                                              │
│  PAY RATE SUMMARY                                        [Export PDF]       │
│  ─────────────────                                                          │
│                                                                              │
│  ┌─ BASE PAY ──────────────────────────────────────────────────────────┐   │
│  │                                                                     │   │
│  │  Classification: Retail Employee Level 1                           │   │
│  │  Employment:     Full-time (Adult)                                  │   │
│  │                                                                     │   │
│  │  ┌────────────────────┬────────────────┬─────────────────────────┐ │   │
│  │  │                    │ Award Minimum  │ Your Rate (if override) │ │   │
│  │  ├────────────────────┼────────────────┼─────────────────────────┤ │   │
│  │  │ Hourly Rate        │ $26.55         │ [          ] (optional) │ │   │
│  │  │ Weekly Rate (38h)  │ $1,008.90      │ [          ] (optional) │ │   │
│  │  │ Annual Rate        │ $52,462.80     │ [          ] (optional) │ │   │
│  │  └────────────────────┴────────────────┴─────────────────────────┘ │   │
│  │                                                                     │   │
│  │  ⓘ You can override rates only if HIGHER than award minimum.       │   │
│  │                                                                     │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  ┌─ PENALTY RATES (Based on selected tags: Shift Worker, Weekend Roster)─┐ │
│  │                                                                        │ │
│  │  ┌────────────────────────────┬──────────┬───────────┬─────────────┐  │ │
│  │  │ Work Condition             │ Rate %   │ Hourly    │ Weekly      │  │ │
│  │  ├────────────────────────────┼──────────┼───────────┼─────────────┤  │ │
│  │  │ Ordinary Hours             │ 100%     │ $26.55    │ $1,008.90   │  │ │
│  │  │ Saturday - First 4 Hours   │ 150%     │ $39.83    │ $1,513.35   │  │ │
│  │  │ Saturday - After 4 Hours   │ 200%     │ $53.10    │ $2,017.80   │  │ │
│  │  │ Sunday                     │ 200%     │ $53.10    │ $2,017.80   │  │ │
│  │  │ Afternoon Shift            │ 115%     │ $30.53    │ $1,160.24   │  │ │
│  │  │ Night Shift (Rotating)     │ 130%     │ $34.52    │ $1,311.57   │  │ │
│  │  └────────────────────────────┴──────────┴───────────┴─────────────┘  │ │
│  │                                                                        │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  ┌─ ALLOWANCES (Selected: First Aid, Cold Work) ──────────────────────────┐ │
│  │                                                                        │ │
│  │  ┌────────────────────────────┬──────────┬──────────┬──────────────┐  │ │
│  │  │ Allowance                  │ Amount   │ Unit     │ Total/Week   │  │ │
│  │  ├────────────────────────────┼──────────┼──────────┼──────────────┤  │ │
│  │  │ First Aid Allowance        │ $13.89   │ /week    │ $13.89       │  │ │
│  │  │ Cold Work Disability       │ $0.37    │ /hour    │ $14.06       │  │ │
│  │  ├────────────────────────────┼──────────┼──────────┼──────────────┤  │ │
│  │  │ TOTAL ALLOWANCES           │          │          │ $27.95/week  │  │ │
│  │  └────────────────────────────┴──────────┴──────────┴──────────────┘  │ │
│  │                                                                        │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  ┌─ TOTAL WEEKLY PAY SUMMARY ─────────────────────────────────────────────┐ │
│  │                                                                        │ │
│  │  Base Weekly (38 ordinary hours)        $1,008.90                     │ │
│  │  + Allowances                           $   27.95                     │ │
│  │  ─────────────────────────────────────────────────                    │ │
│  │  TOTAL (Ordinary Hours Only)            $1,036.85                     │ │
│  │                                                                        │ │
│  │  📊 Example with 4hrs Saturday + 8hrs Sunday:                         │ │
│  │     26 Ordinary Hours:  $   690.30                                    │ │
│  │     4 Sat Hours (150%): $   159.32                                    │ │
│  │     8 Sun Hours (200%): $   424.80                                    │ │
│  │     Allowances:         $    27.95                                    │ │
│  │     ─────────────────────────────────                                 │ │
│  │     TOTAL:              $1,302.37                                     │ │
│  │                                                                        │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  [Save as Employee Template]  [Print Summary]  [Apply to Employees]          │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Rule Engine Architecture Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        RULE ENGINE ARCHITECTURE                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                     DATA LAYER (MSSQL)                                │  │
│  │                                                                       │  │
│  │  ┌───────────┐  ┌───────────┐  ┌───────────┐  ┌───────────┐         │  │
│  │  │  Awards   │  │ Classif.  │  │ Penalties │  │ Allowances│         │  │
│  │  └─────┬─────┘  └─────┬─────┘  └─────┬─────┘  └─────┬─────┘         │  │
│  │        │              │              │              │                │  │
│  │        └──────────────┴──────────────┴──────────────┘                │  │
│  │                               │                                       │  │
│  │                               ▼                                       │  │
│  │  ┌─────────────────────────────────────────────────────────────────┐ │  │
│  │  │              RULE GENERATION ENGINE                              │ │  │
│  │  │                                                                  │ │  │
│  │  │   ┌─────────────────────────────────────────────────────────┐   │ │  │
│  │  │   │  sp_GeneratePayRules (Batch - runs nightly/on import)  │   │ │  │
│  │  │   │  Generates all combinations → ComputedPayRules table   │   │ │  │
│  │  │   └─────────────────────────────────────────────────────────┘   │ │  │
│  │  │                              OR                                  │ │  │
│  │  │   ┌─────────────────────────────────────────────────────────┐   │ │  │
│  │  │   │  sp_GetPayRatesByConditions (Runtime - on dropdown)    │   │ │  │
│  │  │   │  Calculates on-demand based on 5 selected conditions   │   │ │  │
│  │  │   └─────────────────────────────────────────────────────────┘   │ │  │
│  │  │                                                                  │ │  │
│  │  └─────────────────────────────────────────────────────────────────┘ │  │
│  │                               │                                       │  │
│  │                               ▼                                       │  │
│  │  ┌─────────────────────────────────────────────────────────────────┐ │  │
│  │  │              COMPUTED PAY RULES TABLE                           │ │  │
│  │  │                                                                  │ │  │
│  │  │  RuleId │ Award │ EmpType │ Level │ Penalty │ Rate │ Weekly    │ │  │
│  │  │  ───────┼───────┼─────────┼───────┼─────────┼──────┼──────────  │ │  │
│  │  │  1      │ MA04  │ FT      │ 1     │ -       │26.55 │ 1,008.90  │ │  │
│  │  │  2      │ MA04  │ FT      │ 1     │ Sat 4h  │39.83 │ 1,513.35  │ │  │
│  │  │  3      │ MA04  │ FT      │ 1     │ Sunday  │53.10 │ 2,017.80  │ │  │
│  │  │  ...    │ ...   │ ...     │ ...   │ ...     │ ...  │ ...       │ │  │
│  │  └─────────────────────────────────────────────────────────────────┘ │  │
│  │                                                                       │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                    │                                        │
│                                    ▼                                        │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                     APPLICATION LAYER (API)                           │  │
│  │                                                                       │  │
│  │  ┌──────────────────────────────────────────────────────────────────┐│  │
│  │  │ GET /api/pay-rates                                               ││  │
│  │  │ POST Body:                                                       ││  │
│  │  │ {                                                                ││  │
│  │  │   "awardId": 4,                                                  ││  │
│  │  │   "employmentType": "FT",                                        ││  │
│  │  │   "classificationLevel": 1,                                      ││  │
│  │  │   "allowanceIds": [1, 3],                                        ││  │
│  │  │   "tagIds": [2, 4],                                              ││  │
│  │  │   "tenantId": 123,                                               ││  │
│  │  │   "effectiveDate": "2025-07-01"                                  ││  │
│  │  │ }                                                                ││  │
│  │  └──────────────────────────────────────────────────────────────────┘│  │
│  │                                                                       │  │
│  │  RESPONSE:                                                            │  │
│  │  {                                                                    │  │
│  │    "basePay": {                                                       │  │
│  │      "hourlyRate": 26.55,                                             │  │
│  │      "weeklyRate": 1008.90,                                           │  │
│  │      "annualRate": 52462.80                                           │  │
│  │    },                                                                 │  │
│  │    "penaltyRates": [...],                                             │  │
│  │    "allowances": [...],                                               │  │
│  │    "totalWeekly": 1036.85,                                            │  │
│  │    "tenantOverrides": null                                            │  │
│  │  }                                                                    │  │
│  │                                                                       │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                    │                                        │
│                                    ▼                                        │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                     PRESENTATION LAYER (UI)                           │  │
│  │                                                                       │  │
│  │  ┌────────────┐  ┌─────────────────────────────────────────────────┐ │  │
│  │  │  5 Dropdown │  │           PAY RATE SUMMARY                     │ │  │
│  │  │  Conditions │  │  ┌─────────────────────────────────────────┐  │ │  │
│  │  │  ─────────  │─▶│  │  Base Pay:     $26.55/hr               │  │ │  │
│  │  │  Award      │  │  │  + Penalties:  $39.83/hr (Sat)         │  │ │  │
│  │  │  Emp Type   │  │  │  + Allowances: $27.95/week             │  │ │  │
│  │  │  Level      │  │  │  ───────────────────────               │  │ │  │
│  │  │  Allowances │  │  │  TOTAL:        $1,036.85/week          │  │ │  │
│  │  │  Tags       │  │  └─────────────────────────────────────────┘  │ │  │
│  │  └────────────┘  └─────────────────────────────────────────────────┘ │  │
│  │                                                                       │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## API Endpoints

### Get Pay Rates by Conditions

```http
POST /api/v1/pay-rates/calculate
Content-Type: application/json

{
  "awardId": 4,
  "employmentType": "FT",
  "classificationLevel": 1,
  "allowanceIds": [1, 3],
  "tagIds": [2, 4],
  "tenantId": 123,
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
      "name": "Ordinary Hours",
      "multiplier": 1.00,
      "hourlyRate": 26.55,
      "weeklyRate": 1008.90
    },
    {
      "name": "Saturday - First 4 Hours",
      "multiplier": 1.50,
      "hourlyRate": 39.83,
      "weeklyRate": 1513.35
    },
    {
      "name": "Saturday - After 4 Hours",
      "multiplier": 2.00,
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
      "amount": 0.37,
      "unit": "per_hour",
      "weeklyEquivalent": 14.06
    }
  ],
  "totalAllowancesPerWeek": 27.95,
  "totalWeeklyPay": 1036.85,
  "appliedTags": ["Shift Worker", "Weekend Roster"],
  "tenantOverrides": null,
  "effectiveDate": "2025-07-01"
}
```

### Save Tenant Override

```http
POST /api/v1/tenants/{tenantId}/pay-overrides
Content-Type: application/json

{
  "ruleId": 12345,
  "overrideHourlyRate": 28.00,
  "overrideWeeklyRate": 1064.00,
  "effectiveFrom": "2025-07-01",
  "reason": "Enterprise Agreement rate"
}

Response:
{
  "overrideId": 789,
  "status": "PENDING_APPROVAL",
  "validation": {
    "isValid": true,
    "message": "Rate $28.00 meets award minimum of $26.55"
  }
}
```

### Get Dropdown Options

```http
GET /api/v1/dropdowns/awards?industryId=1

Response:
{
  "awards": [
    {"id": 4, "code": "MA000004", "name": "General Retail Industry Award 2020"},
    {"id": 83, "code": "MA000083", "name": "Commercial Sales Award 2020"}
  ]
}

GET /api/v1/dropdowns/classifications?awardId=4&employmentType=FT

Response:
{
  "classifications": [
    {"level": 1, "name": "Retail Employee Level 1", "hourlyRate": 26.55},
    {"level": 2, "name": "Retail Employee Level 2", "hourlyRate": 27.16},
    {"level": 3, "name": "Retail Employee Level 3", "hourlyRate": 27.58}
  ]
}

GET /api/v1/dropdowns/tags

Response:
{
  "tags": [
    {"id": 1, "code": "SHIFT_WORKER", "name": "Shift Worker", "category": "SHIFT"},
    {"id": 2, "code": "WEEKEND_ROSTER", "name": "Weekend Roster", "category": "ROSTER"},
    {"id": 3, "code": "FIRST_AIDER", "name": "First Aider", "category": "ELIGIBILITY"}
  ]
}
```

---

## Recommendation: Hybrid Approach

For optimal performance with MSSQL:

| Scenario | Approach | Reason |
|----------|----------|--------|
| **Initial Load** | Pre-computed | Generate all rules when award data is imported |
| **Daily Refresh** | Pre-computed | Scheduled job to regenerate rules for effective date changes |
| **Real-time Query** | Runtime | Calculate only when tenant selects specific allowances/tags |
| **Tenant Overrides** | Runtime | Check override table on each request |

This hybrid approach ensures:
- Fast queries (indexed pre-computed table)
- Flexibility for custom tag/allowance combinations
- Support for tenant-specific rate overrides

---

## Summary

The Rule Engine provides:

1. **5 Dropdown Conditions** for filtering pay rules
2. **Pre-computed rules** in MSSQL for fast lookups
3. **Runtime calculation** for custom combinations
4. **Tag system** for grouping related penalties/allowances
5. **Tenant overrides** with validation against award minimums
6. **Pay Rate Summary** showing complete breakdown
7. **API endpoints** for all operations
