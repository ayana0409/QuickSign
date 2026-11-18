using QuickSign.HttpApi.Host.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Register layered architecture dependencies and host services
builder.Services.AddHostServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseHostPipeline();

app.Run();
