using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Services.OrdersService;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly OrderStore _store;
    private readonly IAuthorizationService _auth;

    public OrdersController(OrderStore store, IAuthorizationService auth)
    {
        _store = store;
        _auth  = auth;
    }

    // -----------------------------------------------------------------------
    // GET /api/orders
    // No resource instance yet — check each order individually.
    // Returns only the orders this caller is allowed to view.
    // (In production with large datasets, use PlanResources instead to get
    //  a SQL filter expression rather than loading everything first.)
    // -----------------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var visible = new List<Order>();
        foreach (var order in _store.All())
        {
            var result = await _auth.AuthorizeAsync(User, (Shared.CerbosAuth.ICerbosResource)order, "Cerbos:order:view");
            if (result.Succeeded) visible.Add(order);
        }
        return Ok(visible);
    }

    // -----------------------------------------------------------------------
    // GET /api/orders/{id}
    // Imperative check: load the resource first, then ask Cerbos.
    // This is required for owner/status-based checks — you need the data.
    // -----------------------------------------------------------------------
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = _store.Find(id);
        if (order is null) return NotFound();

        var result = await _auth.AuthorizeAsync(User, (Shared.CerbosAuth.ICerbosResource)order, "Cerbos:order:view");
        return result.Succeeded ? Ok(order) : Forbid();
    }

    // -----------------------------------------------------------------------
    // POST /api/orders
    // Declarative check via [Authorize(Policy=...)] — no resource instance yet.
    // Policy just checks the role, not resource attributes.
    // -----------------------------------------------------------------------
    [HttpPost]
    [Authorize(Policy = "Cerbos:order:create")]
    public IActionResult Create([FromBody] CreateOrderRequest req)
    {
        var userId = UserId();
        var dept   = UserDept();

        var order = new Order
        {
            Title           = req.Title,
            Amount          = req.Amount,
            Status          = OrderStatus.Draft,
            CreatedByUserId = userId,
            Department      = dept
        };

        _store.Add(order);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    // -----------------------------------------------------------------------
    // PATCH /api/orders/{id}/edit
    // Imperative check — policy: owner only, status must be "draft"
    // Both conditions are evaluated by Cerbos, not here.
    // -----------------------------------------------------------------------
    [HttpPatch("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, [FromBody] EditOrderRequest req)
    {
        var order = _store.Find(id);
        if (order is null) return NotFound();

        var result = await _auth.AuthorizeAsync(User, (Shared.CerbosAuth.ICerbosResource)order, "Cerbos:order:edit");
        if (!result.Succeeded) return Forbid();

        if (req.Title  is not null) order.Title  = req.Title;
        if (req.Amount is not null) order.Amount = req.Amount.Value;
        _store.Replace(order);

        return Ok(order);
    }

    // -----------------------------------------------------------------------
    // PATCH /api/orders/{id}/cancel
    // Policy: owner only, status must be "draft" or "pending"
    // -----------------------------------------------------------------------
    [HttpPatch("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var order = _store.Find(id);
        if (order is null) return NotFound();

        var result = await _auth.AuthorizeAsync(User, (Shared.CerbosAuth.ICerbosResource)order, "Cerbos:order:cancel");
        if (!result.Succeeded) return Forbid();

        order.Status = OrderStatus.Cancelled;
        _store.Replace(order);
        return Ok(order);
    }

    // -----------------------------------------------------------------------
    // PATCH /api/orders/{id}/approve
    // Policy: dept_manager (derived role) or admin only.
    // A regular user gets 403 even if they own the order.
    // -----------------------------------------------------------------------
    [HttpPatch("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var order = _store.Find(id);
        if (order is null) return NotFound();

        var result = await _auth.AuthorizeAsync(User, (Shared.CerbosAuth.ICerbosResource)order, "Cerbos:order:approve");
        if (!result.Succeeded) return Forbid();

        order.Status = OrderStatus.Approved;
        _store.Replace(order);
        return Ok(order);
    }

    // --- Helpers ---
    private string UserId() =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? throw new InvalidOperationException("No user ID claim.");

    private string UserDept() =>
        User.FindFirst("department")?.Value ?? "unknown";
}

public record CreateOrderRequest(string Title, decimal Amount);
public record EditOrderRequest(string? Title, decimal? Amount);
