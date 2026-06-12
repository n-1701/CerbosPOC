namespace Services.OrdersService;

/// <summary>
/// Seed data covers every authorization scenario in the POC:
///
///   order-1  — owned by user-alice, dept=engineering, status=draft    → alice can edit/cancel
///   order-2  — owned by user-bob,   dept=engineering, status=pending  → alice cannot edit (wrong owner)
///   order-3  — owned by user-bob,   dept=engineering, status=approved → bob cannot cancel (wrong status)
///   order-4  — owned by user-carol, dept=marketing,   status=draft    → eng manager cannot approve (wrong dept)
/// </summary>
public class OrderStore
{
    private readonly List<Order> _orders = new()
    {
        new Order { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), Title = "Office Supplies",  Amount = 250m,    Status = OrderStatus.Draft,    CreatedByUserId = "user-alice", Department = "engineering" },
        new Order { Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002"), Title = "Server Hardware",  Amount = 15000m,  Status = OrderStatus.Pending,  CreatedByUserId = "user-bob",   Department = "engineering" },
        new Order { Id = Guid.Parse("cccccccc-0000-0000-0000-000000000003"), Title = "Cloud Licenses",   Amount = 3000m,   Status = OrderStatus.Approved, CreatedByUserId = "user-bob",   Department = "engineering" },
        new Order { Id = Guid.Parse("dddddddd-0000-0000-0000-000000000004"), Title = "Marketing Banners",Amount = 800m,    Status = OrderStatus.Draft,    CreatedByUserId = "user-carol", Department = "marketing"   },
    };

    public IEnumerable<Order> All()          => _orders;
    public Order? Find(Guid id)              => _orders.FirstOrDefault(o => o.Id == id);
    public void   Add(Order o)               => _orders.Add(o);
    public void   Replace(Order o)           { var i = _orders.FindIndex(x => x.Id == o.Id); if (i >= 0) _orders[i] = o; }
}
