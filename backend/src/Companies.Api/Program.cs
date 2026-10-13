var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    // In Development both :5000 and :7000 serve the API (spec + Angular dev proxy).
    app.UseHttpsRedirection();
}

app.MapControllers();

await app.RunAsync();
