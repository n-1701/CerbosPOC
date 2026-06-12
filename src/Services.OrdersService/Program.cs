using Services.OrdersService;
using Shared.CerbosAuth;

var builder = WebApplication.CreateBuilder(args);

// --- Authentication ---
// FakeAuthMiddleware handles auth in Development so you can test without an IdP.
// In production, replace with your real JWT bearer config:
//
//   builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//       .AddJwtBearer(o => { o.Authority = ...; o.Audience = ...; });
//
builder.Services.AddAuthentication("FakeAuth")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
               FakeAuthSchemeHandler>("FakeAuth", _ => { });

// --- Cerbos: one line wires up everything ---
builder.Services.AddCerbos(builder.Configuration);

// --- App ---
builder.Services.AddSingleton<OrderStore>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Orders POC", Version = "v1" });
    c.OperationFilter<FakeAuthSwaggerFilter>();  // adds userId/roles/department to Swagger UI
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
    app.UseMiddleware<FakeAuthMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
