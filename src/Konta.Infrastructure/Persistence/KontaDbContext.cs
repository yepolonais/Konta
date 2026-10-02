using Konta.Domain.Categories;
using Konta.Domain.Accounts;
using Konta.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Konta.Infrastructure.Persistence;

public sealed class KontaDbContext(DbContextOptions<KontaDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Account> Accounts => Set<Account>();

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
    }
}