// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Account.Contracts.Events.External;

public class UserSyncBatchEvent
{
    public List<UserSyncRecord> Users { get; set; } = [];
}

public class UserSyncRecord
{
    public string Email { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public Guid? ReferralId { get; set; }
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
}
