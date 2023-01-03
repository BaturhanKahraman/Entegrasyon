using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Shared.Extensions;
using System.Text;
using Entegrasyon.Business.Extensions;
using Shared.FileStorage;
using Shared.Logger.Serilog;
using Entegrasyon.Business.Concrete;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior",true);
builder.Services.AddControllers();
builder.Services.AddLogging();
builder.Services.AddConfigurations(builder.Configuration);
builder.Services.AddApplicationDependencies();
builder.Services.AddFileStorageCore();

builder.Services.AddLocalFileStorage(builder.Configuration.GetSection("LocalFileStorageOptions"));


builder.Services.AddCors(options =>
    options.AddPolicy("myclient",policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddStackExchangeRedisCache(opt =>
    {
        opt.Configuration = "redis";
        opt.InstanceName = "DemoInstance";
    });
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCustomDbContext();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(x =>
{
    x.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtTokenOptions:Issuer"],
        ValidAudience = builder.Configuration["JwtTokenOptions:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtTokenOptions:SecurityKey"]))
    };
});
builder.Services.AddAuthorization();
builder.Services.AddHttpClient();
builder.AddSerilogWithLoggerProvider(builder.Configuration);
var app = builder.Build();
app.Lifetime.ApplicationStarted.Register(async () =>
{
    await using var serviceScope = app.Services.CreateAsyncScope();
    var lifeTimeHandler = serviceScope.ServiceProvider.GetRequiredService<ApplicationLifetimeManager>();
    if(lifeTimeHandler != null)
        await lifeTimeHandler.ApplyStartActions();
    await serviceScope.DisposeAsync();
});
app.AddCustomExceptionHandlerMiddleware();
app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles();
app.UseCors("myclient");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
//app.AddJwtBlacklistMiddleware();
app.MapControllers();

app.Run();
