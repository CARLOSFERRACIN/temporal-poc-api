using Temporal.POC.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add application services
builder.Services.AddApplicationServices();

// Add Temporal services
builder.Services.AddTemporalServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

