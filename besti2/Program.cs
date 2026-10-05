using besti2.Infrastructure;
using besti2.Application.Businesses;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
var app = builder.Build();

// Configure the HTTP request pipeline.
 if (app.Environment.IsDevelopment())
 {
     app.MapOpenApi();
 }

app.UseHttpsRedirection();

// Get all businesses
app.MapGet("/api/businesses", async (IBusinessService businessService, CancellationToken cancellationToken) =>
{
   return Results.Ok(await businessService.GetAllAsync(cancellationToken));
});
// Get business by id
app.MapGet("/api/businesses/{id:guid}", async (Guid id, IBusinessService businessService, CancellationToken cancellationToken) =>
{
    var business = await businessService.GetByIdAsync(id, cancellationToken);
    return business is null ? Results.NotFound() : Results.Ok(business);
});

app.Run();