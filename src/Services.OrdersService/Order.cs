using Shared.CerbosAuth;

namespace Services.OrdersService;

public enum OrderStatus { Draft, Pending, Approved, Cancelled }

public class Order : ICerbosResource
{
    public Guid   Id               { get; set; } = Guid.NewGuid();
    public string Title            { get; set; } = string.Empty;
    public decimal Amount          { get; set; }
    public OrderStatus Status      { get; set; } = OrderStatus.Draft;
    public string CreatedByUserId  { get; set; } = string.Empty;
    public string Department       { get; set; } = string.Empty;

    // --- ICerbosResource ---
    // These map to resource.attr.* in the YAML policy

    string ICerbosResource.ResourceId => Id.ToString();
    string ICerbosResource.OwnerId    => CreatedByUserId;
    string ICerbosResource.Department => Department;

    Dictionary<string, object> ICerbosResource.ExtraAttributes => new()
    {
        ["status"] = Status.ToString().ToLower()  // policy: resource.attr.status == 'draft'
    };
}
