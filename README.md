# Cerbos POC — .NET Microservice

Demonstrates Cerbos authorization in a single .NET microservice with:
- RBAC (role-based rules)
- ABAC (attribute-based conditions: ownership, status)
- Derived roles (owner, dept_manager — computed at runtime)

---

## Structure

```
CerbosPoc.sln
├── cerbos.yaml                          # Cerbos PDP config
├── policies/
│   ├── derived_roles/common_roles.yaml  # owner, dept_manager — defined once, used everywhere
│   └── resource_policies/order.yaml     # all rules for the "order" resource
└── src/
    ├── Shared.CerbosAuth/               # shared library — copy to any new microservice
    │   ├── ICerbosResource.cs           # interface your domain models implement
    │   ├── CerbosAuthorizationHandler.cs# calls the PDP, single place in the codebase
    │   ├── CerbosPolicyProvider.cs      # parses "Cerbos:order:approve" policy names
    │   └── ServiceCollectionExtensions  # services.AddCerbos(config) — one line setup
    └── Services.OrdersService/
        ├── Order.cs                     # domain model, implements ICerbosResource
        ├── OrderStore.cs                # in-memory store with seed data
        ├── OrdersController.cs          # API endpoints, all auth patterns shown here
        ├── FakeAuthMiddleware.cs        # DEV ONLY: inject user via query string
        ├── FakeAuthSchemeHandler.cs     # makes ASP.NET Core accept FakeAuth identity
        └── FakeAuthSwaggerFilter.cs     # adds userId/roles/department to Swagger UI
```

---

## Running

### 1. Start the Cerbos PDP

Download the binary for your OS from https://github.com/cerbos/cerbos/releases

```bash
# From the repo root
./cerbos server --config=cerbos.yaml
```

Verify it's running:
```bash
curl http://localhost:3592/_cerbos/health
# {"healthy":true}
```

Validate your policies compile:
```bash
./cerbos compile ./policies
```

### 2. Start the Orders service

```bash
cd src/Services.OrdersService
dotnet run
```

Swagger UI: http://localhost:5001/swagger

---

## Seed Data

| ID (prefix) | Title             | Owner       | Dept        | Status   |
|-------------|-------------------|-------------|-------------|----------|
| aaaaaaaa    | Office Supplies   | user-alice  | engineering | draft    |
| bbbbbbbb    | Server Hardware   | user-bob    | engineering | pending  |
| cccccccc    | Cloud Licenses    | user-bob    | engineering | approved |
| dddddddd    | Marketing Banners | user-carol  | marketing   | draft    |

---

## Test Scenarios

Every scenario is a curl command. Swap `localhost:5001` for Swagger if you prefer.

### ALLOW: User views their own order
```bash
curl "http://localhost:5001/api/orders/aaaaaaaa-0000-0000-0000-000000000001?userId=user-alice&roles=user&department=engineering"
# 200 OK
```

### DENY: User tries to view someone else's order
```bash
curl "http://localhost:5001/api/orders/bbbbbbbb-0000-0000-0000-000000000002?userId=user-alice&roles=user&department=engineering"
# 403 Forbidden
```

### ALLOW: Owner edits their own draft order
```bash
curl -X PATCH "http://localhost:5001/api/orders/aaaaaaaa-0000-0000-0000-000000000001/edit?userId=user-alice&roles=user&department=engineering" \
  -H "Content-Type: application/json" \
  -d '{"title": "Updated Supplies"}'
# 200 OK
```

### DENY: Owner tries to edit an already-approved order (wrong status)
```bash
curl -X PATCH "http://localhost:5001/api/orders/cccccccc-0000-0000-0000-000000000003/edit?userId=user-bob&roles=user&department=engineering" \
  -H "Content-Type: application/json" \
  -d '{"title": "Try to edit"}'
# 403 Forbidden — policy: edit only allowed when status == 'draft'
```

### ALLOW: Owner cancels their pending order
```bash
curl -X PATCH "http://localhost:5001/api/orders/bbbbbbbb-0000-0000-0000-000000000002/cancel?userId=user-bob&roles=user&department=engineering"
# 200 OK
```

### DENY: User tries to cancel someone else's order
```bash
curl -X PATCH "http://localhost:5001/api/orders/aaaaaaaa-0000-0000-0000-000000000001/cancel?userId=user-bob&roles=user&department=engineering"
# 403 Forbidden
```

### ALLOW: Dept manager approves an order in their department
```bash
curl -X PATCH "http://localhost:5001/api/orders/bbbbbbbb-0000-0000-0000-000000000002/approve?userId=mgr-dave&roles=manager&department=engineering"
# 200 OK — mgr-dave is a manager in engineering, order is also engineering
```

### DENY: Dept manager tries to approve an order in a different department
```bash
curl -X PATCH "http://localhost:5001/api/orders/dddddddd-0000-0000-0000-000000000004/approve?userId=mgr-dave&roles=manager&department=engineering"
# 403 Forbidden — order-4 is in marketing, mgr-dave is in engineering
```

### ALLOW: Admin approves anything regardless of department
```bash
curl -X PATCH "http://localhost:5001/api/orders/dddddddd-0000-0000-0000-000000000004/approve?userId=admin-eve&roles=admin&department=any"
# 200 OK
```

### ALLOW: List — user sees only their own orders
```bash
curl "http://localhost:5001/api/orders?userId=user-alice&roles=user&department=engineering"
# Returns only order-1 (alice's order). order-2, 3, 4 are filtered out.
```

### ALLOW: List — admin sees all orders
```bash
curl "http://localhost:5001/api/orders?userId=admin-eve&roles=admin&department=any"
# Returns all 4 orders
```

---

## Live Policy Change (no restart needed)

While the service is running, open `policies/resource_policies/order.yaml` and change
the `edit` rule to also allow managers:

```yaml
- actions: ["edit"]
  effect: EFFECT_ALLOW
  derivedRoles: ["owner", "dept_manager"]   # add dept_manager here
  condition:
    match:
      expr: "resource.attr.status == 'draft'"
```

Save the file. The PDP picks it up immediately (`watchForChanges: true`).
Now test — a manager in the same dept can edit draft orders without touching any .NET code.

---

## Adding a Second Microservice

1. Add `<ProjectReference>` to `Shared.CerbosAuth` in the new service's `.csproj`
2. Call `services.AddCerbos(configuration)` in `Program.cs`
3. Implement `ICerbosResource` on the new domain model
4. Add a new YAML file under `policies/resource_policies/`
5. Use `await _auth.AuthorizeAsync(User, resource, "Cerbos:newresource:action")` in controllers

The shared library, the PDP, and the policies folder are all reused as-is.

---

## Future: Moving to Docker / Kubernetes

**Code changes required: zero.**

Only `appsettings.Production.json` changes:

| Deployment       | PdpAddress                                        | UseTls |
|------------------|---------------------------------------------------|--------|
| Local binary     | `http://localhost:3593`                           | false  |
| Docker           | `http://cerbos:3593` (container name)             | true   |
| K8s sidecar      | `http://localhost:3593` (same pod)                | true   |
| K8s service      | `http://cerbos-svc.default.svc.cluster.local:3593`| true   |
