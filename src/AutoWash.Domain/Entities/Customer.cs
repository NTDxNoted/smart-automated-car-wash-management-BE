using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoWash.Domain.Entities
{
  [Table("Customer")]
  public class Customer
  {
    public int CustomerID { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    // MAP đúng với PostgreSQL column: password
    [Column("password")]
    public string PasswordHash { get; set; } = string.Empty;

    public int TierID { get; set; } = 1;

    public bool IsLocked { get; set; } = false;

    public DateTime? SuspendedUntil { get; set; }

    public decimal TotalSpending { get; set; } = 0;

    public DateTime? LastVisit { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int NoShowCount { get; set; } = 0;

    // ❌ KHÔNG dùng Tier entity nếu bạn chưa setup DbSet
    // bỏ để tránh lỗi build

    // ================= NOT MAPPED =================
    [NotMapped]
    public string TierName =>
        TierID switch
        {
          1 => "Member",
          2 => "Silver",
          3 => "Gold",
          4 => "Platinum",
          _ => "Member"
        };

    [NotMapped]
    public string Role => "Customer";
  }
}