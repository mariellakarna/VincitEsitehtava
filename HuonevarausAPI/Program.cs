using HuonevarausAPI.Data;
using HuonevarausAPI.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Lis‰‰ tietokanta
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=huonevaraus.db"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<HuonevarausAPI.Services.ReservationService>();

var app = builder.Build();

// Testidatan lis‰ys
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.Rooms.Any())
    {
        db.Rooms.AddRange(
            new Room { Name = "Neuvotteluhuone 1" },
            new Room { Name = "Neuvotteluhuone 2" },
            new Room { Name = "Neuvotteluhuone 3" }
        );
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();