using System.Text;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.MapperProfiles;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using MainDatabase.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Extensions;
using Shared.Security.Jwt;
using Shared.User.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddLogging();

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior",true);

builder.Services.AddCors(options =>
    options.AddPolicy("myclient",policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddDbContext<IntegrationDbContext>(x =>
{
    x.UseNpgsql("Server=db;Port=5432;Database=Demo3;User Id=Baturhan;Password=649471;");
});
builder.Services.AddDbContext<HeadDbContext>(x =>
{
    x.UseNpgsql(builder.Configuration.GetConnectionString("Main"));
});
builder.Services.AddSharedSettings();
builder.Services.Configure<TokenOptions>(builder.Configuration.GetSection("JwtTokenOptions"));

builder.Services.AddScoped<IUserManager<ApplicationUser>,UserManager<ApplicationUser,IntegrationDbContext>>();

builder.Services.AddScoped<AuthManager>();
builder.Services.AddScoped<ApplicationUserManager>();
builder.Services.AddAutoMapper(x =>
{
    x.AddProfile<UserProfile>();
});

builder.Services.AddStackExchangeRedisCache(opt =>
    {
        opt.Configuration = "redis";
        opt.InstanceName = "DemoInstance";
    });
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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
var app = builder.Build();
app.AddCustomExceptionHandlerMiddleware();
// Configure the HTTP request pipeline.
if(app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("myclient");

await using var scope = app.Services.CreateAsyncScope();

await app.Services.CreateScope().ServiceProvider.GetService<IntegrationDbContext>().Database.MigrateAsync();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.AddJwtBlacklistMiddleware();
app.MapControllers();

app.Run();
