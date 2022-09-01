using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using MainDatabase.Context;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContext<IntegrationDbContext>(opt =>
{
    //   opt.UseInMemoryDatabase("Demo");
    //opt.UseNpgsql("Server=127.0.0.1;Port=5432;Database=DemoDBB;User Id=postgres;Password=649471;");
    opt.UseNpgsql("Server=db;Port=5432;Database=DemoDBB;User Id=postgres;Password=example;");
});
builder.Services.AddDbContext<HeadDbContext>();

builder.Services.AddStackExchangeRedisCache(opt =>
    {
        opt.Configuration = "redis";
        opt.InstanceName = "DemoInstance";
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if(app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
