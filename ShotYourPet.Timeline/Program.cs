using Microsoft.EntityFrameworkCore;
using ShotYourPet.Database;
using ShotYourPet.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<TimelineDbContext>(
    opts => opts.UseNpgsql(builder.Configuration.GetConnectionString("ShotYourPet"),
        x => x.MigrationsAssembly("ShotYourPet.Migrations")));

builder.Services.AddCors(cors => cors
    .AddPolicy("ShotYourPet", policy => policy
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()));

builder.Services.AddHostedService<MessageService>();

var app = builder.Build();

app.UseCors("ShotYourPet");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<TimelineDbContext>();
    context.Database.Migrate();
}

app.Run();