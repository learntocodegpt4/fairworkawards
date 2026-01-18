using Microsoft.EntityFrameworkCore;
using FairWorkAwards.Domain.Entities;

namespace FairWorkAwards.Infrastructure.Data;

/// <summary>
/// Database context for Fair Work Awards system
/// </summary>
public class FairWorkAwardsDbContext : DbContext
{
    public FairWorkAwardsDbContext(DbContextOptions<FairWorkAwardsDbContext> options)
        : base(options)
    {
    }
    
    // Core entities
    public DbSet<Industry> Industries => Set<Industry>();
    public DbSet<Award> Awards => Set<Award>();
    public DbSet<EmploymentType> EmploymentTypes => Set<EmploymentType>();
    public DbSet<Classification> Classifications => Set<Classification>();
    public DbSet<Allowance> Allowances => Set<Allowance>();
    public DbSet<PenaltyRate> PenaltyRates => Set<PenaltyRate>();
    
    // Tag entities
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TagPenaltyMapping> TagPenaltyMappings => Set<TagPenaltyMapping>();
    public DbSet<TagAllowanceMapping> TagAllowanceMappings => Set<TagAllowanceMapping>();
    
    // Tenant entities
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantAward> TenantAwards => Set<TenantAward>();
    
    // Computed rules
    public DbSet<ComputedPayRule> ComputedPayRules => Set<ComputedPayRule>();
    public DbSet<ComputedRuleAllowance> ComputedRuleAllowances => Set<ComputedRuleAllowance>();
    public DbSet<ComputedRuleTag> ComputedRuleTags => Set<ComputedRuleTag>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure Industry
        modelBuilder.Entity<Industry>(entity =>
        {
            entity.HasKey(e => e.IndustryId);
            entity.Property(e => e.IndustryCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.IndustryCode).IsUnique();
        });
        
        // Configure Award
        modelBuilder.Entity<Award>(entity =>
        {
            entity.HasKey(e => e.AwardId);
            entity.Property(e => e.AwardCode).HasMaxLength(20).IsRequired();
            entity.Property(e => e.AwardName).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.AwardCode).IsUnique();
            
            entity.HasOne(e => e.Industry)
                .WithMany(i => i.Awards)
                .HasForeignKey(e => e.IndustryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // Configure EmploymentType
        modelBuilder.Entity<EmploymentType>(entity =>
        {
            entity.HasKey(e => e.EmploymentTypeId);
            entity.Property(e => e.Code).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.RateTypeCode).HasMaxLength(5).IsRequired();
            entity.Property(e => e.CasualLoadingPercent).HasPrecision(5, 2);
            entity.HasIndex(e => e.Code).IsUnique();
        });
        
        // Configure Classification
        modelBuilder.Entity<Classification>(entity =>
        {
            entity.HasKey(e => e.ClassificationId);
            entity.Property(e => e.ClassificationName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.BaseHourlyRate).HasPrecision(10, 4);
            entity.Property(e => e.BaseWeeklyRate).HasPrecision(10, 2);
            entity.Property(e => e.BaseAnnualRate).HasPrecision(12, 2);
            
            entity.HasOne(e => e.Award)
                .WithMany(a => a.Classifications)
                .HasForeignKey(e => e.AwardId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.EmploymentType)
                .WithMany(et => et.Classifications)
                .HasForeignKey(e => e.EmploymentTypeId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasIndex(e => new { e.AwardId, e.ClassificationLevel, e.EmploymentTypeId, e.OperativeFrom })
                .IsUnique();
        });
        
        // Configure Allowance
        modelBuilder.Entity<Allowance>(entity =>
        {
            entity.HasKey(e => e.AllowanceId);
            entity.Property(e => e.AllowanceCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.AllowanceName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.AllowanceType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(10, 4);
            entity.Property(e => e.Unit).HasMaxLength(20).IsRequired();
            entity.Property(e => e.RatePercent).HasPrecision(5, 2);
            entity.Property(e => e.ClauseReference).HasMaxLength(50);
            
            entity.HasOne(e => e.Award)
                .WithMany(a => a.Allowances)
                .HasForeignKey(e => e.AwardId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configure PenaltyRate
        modelBuilder.Entity<PenaltyRate>(entity =>
        {
            entity.HasKey(e => e.PenaltyRateId);
            entity.Property(e => e.PenaltyCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PenaltyName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PenaltyCategory).HasMaxLength(50).IsRequired();
            entity.Property(e => e.RateMultiplier).HasPrecision(5, 2);
            entity.Property(e => e.ApplicableDays).HasMaxLength(50);
            entity.Property(e => e.ApplicableHours).HasMaxLength(50);
            entity.Property(e => e.ClauseReference).HasMaxLength(50);
            
            entity.HasOne(e => e.Award)
                .WithMany(a => a.PenaltyRates)
                .HasForeignKey(e => e.AwardId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configure Tag
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.TagId);
            entity.Property(e => e.TagCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.TagName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.TagCategory).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.TagCode).IsUnique();
        });
        
        // Configure TagPenaltyMapping
        modelBuilder.Entity<TagPenaltyMapping>(entity =>
        {
            entity.HasKey(e => new { e.TagId, e.PenaltyRateId });
            
            entity.HasOne(e => e.Tag)
                .WithMany(t => t.TagPenalties)
                .HasForeignKey(e => e.TagId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.PenaltyRate)
                .WithMany()
                .HasForeignKey(e => e.PenaltyRateId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configure TagAllowanceMapping
        modelBuilder.Entity<TagAllowanceMapping>(entity =>
        {
            entity.HasKey(e => new { e.TagId, e.AllowanceId });
            
            entity.HasOne(e => e.Tag)
                .WithMany(t => t.TagAllowances)
                .HasForeignKey(e => e.TagId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Allowance)
                .WithMany()
                .HasForeignKey(e => e.AllowanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configure Tenant
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.TenantId);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ABN).HasMaxLength(50);
            entity.Property(e => e.PrimaryContactName).HasMaxLength(100);
            entity.Property(e => e.PrimaryContactEmail).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.BillingAddress).HasMaxLength(500);
            
            entity.HasOne(e => e.Industry)
                .WithMany()
                .HasForeignKey(e => e.IndustryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // Configure TenantAward
        modelBuilder.Entity<TenantAward>(entity =>
        {
            entity.HasKey(e => e.TenantAwardId);
            entity.Property(e => e.Configuration).HasColumnType("nvarchar(max)");
            
            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.TenantAwards)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Award)
                .WithMany()
                .HasForeignKey(e => e.AwardId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // Configure ComputedPayRule
        modelBuilder.Entity<ComputedPayRule>(entity =>
        {
            entity.HasKey(e => e.RuleId);
            entity.Property(e => e.BaseHourlyRate).HasPrecision(10, 4);
            entity.Property(e => e.PenaltyMultiplier).HasPrecision(5, 2);
            entity.Property(e => e.GeneratedBy).HasMaxLength(100);
            
            entity.HasOne(e => e.Award)
                .WithMany()
                .HasForeignKey(e => e.AwardId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.EmploymentType)
                .WithMany()
                .HasForeignKey(e => e.EmploymentTypeId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Classification)
                .WithMany()
                .HasForeignKey(e => e.ClassificationId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.PenaltyRate)
                .WithMany()
                .HasForeignKey(e => e.PenaltyRateId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasIndex(e => new { e.AwardId, e.EmploymentTypeId, e.ClassificationId, e.EffectiveFrom });
        });
        
        // Configure ComputedRuleAllowance
        modelBuilder.Entity<ComputedRuleAllowance>(entity =>
        {
            entity.HasKey(e => new { e.RuleId, e.AllowanceId });
            entity.Property(e => e.AllowanceAmount).HasPrecision(10, 4);
            
            entity.HasOne(e => e.Rule)
                .WithMany(r => r.RuleAllowances)
                .HasForeignKey(e => e.RuleId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Allowance)
                .WithMany()
                .HasForeignKey(e => e.AllowanceId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // Configure ComputedRuleTag
        modelBuilder.Entity<ComputedRuleTag>(entity =>
        {
            entity.HasKey(e => new { e.RuleId, e.TagId });
            
            entity.HasOne(e => e.Rule)
                .WithMany(r => r.RuleTags)
                .HasForeignKey(e => e.RuleId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Tag)
                .WithMany()
                .HasForeignKey(e => e.TagId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
