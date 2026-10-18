using Konta.Domain.Categories;
using Konta.Domain.Accounts;
using Konta.Domain.Transactions;
using Konta.Domain.Budgets;
using Konta.Domain.Forecasts;
using Konta.Domain.Savings;
using Konta.Domain.Transfers;
using Microsoft.EntityFrameworkCore;

namespace Konta.Infrastructure.Persistence;

public sealed class KontaDbContext(DbContextOptions<KontaDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryRule> CategoryRules => Set<CategoryRule>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<MonthlyBudget> MonthlyBudgets => Set<MonthlyBudget>();
    public DbSet<BudgetOverride> BudgetOverrides => Set<BudgetOverride>();
    public DbSet<RecurringForecast> RecurringForecasts => Set<RecurringForecast>();
    public DbSet<ForecastOccurrence> ForecastOccurrences => Set<ForecastOccurrence>();
    public DbSet<AccountBalanceReference> AccountBalanceReferences => Set<AccountBalanceReference>();
    public DbSet<AccountTransfer> AccountTransfers => Set<AccountTransfer>();
    public DbSet<SavingsGoal> SavingsGoals => Set<SavingsGoal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(transaction => transaction.Id);
            entity.Property(transaction => transaction.Label).HasMaxLength(500).IsRequired();
            entity.Property(transaction => transaction.Amount).HasPrecision(18, 2);
            entity.Property(transaction => transaction.Type).HasConversion<string>().HasMaxLength(16);
            entity.Property(transaction => transaction.Source).HasConversion<string>().HasMaxLength(16);
            entity.Property(transaction => transaction.AccountNumber).HasMaxLength(64);
            entity.Property(transaction => transaction.AccountLabel).HasMaxLength(200);
            entity.Property(transaction => transaction.ImportFingerprint).HasMaxLength(64);
            entity.HasIndex(transaction => transaction.Date);
            entity.HasIndex(transaction => new
            {
                transaction.AccountId,
                transaction.ImportFingerprint,
                transaction.ImportOccurrence
            }).IsUnique();
            entity.HasOne(transaction => transaction.Category)
                .WithMany()
                .HasForeignKey(transaction => transaction.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(transaction => transaction.Account)
                .WithMany()
                .HasForeignKey(transaction => transaction.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(account => account.Id);
            entity.Property(account => account.Name).HasMaxLength(100).IsRequired();
            entity.Property(account => account.BankAccountNumber).HasMaxLength(64).IsRequired();
            entity.Property(account => account.BankLabel).HasMaxLength(200).IsRequired();
            entity.Property(account => account.Kind).HasConversion<string>().HasMaxLength(16);
            entity.HasIndex(account => account.BankAccountNumber).IsUnique();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(category => category.Name).IsUnique();
        });

        modelBuilder.Entity<CategoryRule>(entity =>
        {
            entity.HasKey(rule => rule.Id);
            entity.Property(rule => rule.Name).HasMaxLength(100).IsRequired();
            entity.Property(rule => rule.Pattern).HasMaxLength(200).IsRequired();
            entity.Property(rule => rule.CategoryId).IsRequired();
            entity.HasIndex(rule => rule.Pattern).IsUnique();
            entity.HasIndex(rule => rule.Name).IsUnique();
        });

        modelBuilder.Entity<MonthlyBudget>(entity =>
        {
            entity.HasKey(budget => budget.Id);
            entity.Property(budget => budget.Amount).HasPrecision(18, 2);
            entity.HasIndex(budget => budget.CategoryId).IsUnique();
            entity.HasOne<Category>().WithMany().HasForeignKey(budget => budget.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BudgetOverride>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Amount).HasPrecision(18, 2);
            entity.HasIndex(item => new { item.BudgetId, item.Year, item.Month }).IsUnique();
            entity.HasOne<MonthlyBudget>().WithMany().HasForeignKey(item => item.BudgetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecurringForecast>(entity =>
        {
            entity.HasKey(forecast => forecast.Id);
            entity.Property(forecast => forecast.Name).HasMaxLength(100).IsRequired();
            entity.Property(forecast => forecast.LabelPattern).HasMaxLength(200).IsRequired();
            entity.Property(forecast => forecast.Amount).HasPrecision(18, 2);
            entity.Property(forecast => forecast.Type).HasConversion<string>().HasMaxLength(16);
            entity.Property(forecast => forecast.Frequency).HasConversion<string>().HasMaxLength(16);
            entity.HasOne<Account>().WithMany().HasForeignKey(forecast => forecast.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Category>().WithMany().HasForeignKey(forecast => forecast.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ForecastOccurrence>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ActualAmount).HasPrecision(18, 2);
            entity.HasIndex(item => new { item.ForecastId, item.ScheduledDate }).IsUnique();
            entity.HasIndex(item => item.TransactionId).IsUnique();
            entity.HasOne<RecurringForecast>().WithMany().HasForeignKey(item => item.ForecastId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Transaction>().WithMany().HasForeignKey(item => item.TransactionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AccountBalanceReference>(entity =>
        {
            entity.HasKey(reference => reference.Id);
            entity.Property(reference => reference.BalanceAfterTransaction).HasPrecision(18, 2);
            entity.HasIndex(reference => reference.AccountId).IsUnique();
            entity.HasOne<Account>().WithMany().HasForeignKey(reference => reference.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Transaction>().WithMany().HasForeignKey(reference => reference.TransactionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AccountTransfer>(entity =>
        {
            entity.HasKey(transfer => transfer.Id);
            entity.Property(transfer => transfer.Amount).HasPrecision(18, 2);
            entity.HasIndex(transfer => transfer.SourceTransactionId).IsUnique();
            entity.HasIndex(transfer => transfer.DestinationTransactionId).IsUnique();
            entity.HasOne(transfer => transfer.SourceTransaction).WithMany()
                .HasForeignKey(transfer => transfer.SourceTransactionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(transfer => transfer.DestinationTransaction).WithMany()
                .HasForeignKey(transfer => transfer.DestinationTransactionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SavingsGoal>(entity =>
        {
            entity.HasKey(goal => goal.Id);
            entity.Property(goal => goal.Name).HasMaxLength(100).IsRequired();
            entity.Property(goal => goal.TargetAmount).HasPrecision(18, 2);
            entity.Property(goal => goal.PlannedMonthlyContribution).HasPrecision(18, 2);
            entity.HasIndex(goal => goal.AccountId)
                .IsUnique()
                .HasFilter("\"IsActive\" = 1");
            entity.HasOne<Account>().WithMany().HasForeignKey(goal => goal.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}