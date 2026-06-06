using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoWash.Domain.Entities
{
  public class Customer
  {
    public int CustomerID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int TierID { get; set; } = 1;
    public bool IsLocked { get; set; } = false;
    public DateTime? SuspendedUntil { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public string Tier => TierID == 1 ? "Member" : "Admin";

    [NotMapped]
    public string Role => TierID == 1 ? "Member" : "Admin";
  }
}
