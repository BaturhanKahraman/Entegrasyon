using System.Text;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.MapperProfiles;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using FluentValidation;
using MainDatabase.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Extensions;
using Shared.Security.Jwt;
using Shared.User;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddLogging();
builder.Services.Configure<ApiBehaviorOptions>(o=>o.SuppressModelStateInvalidFilter=true);
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior",true);

builder.Services.AddCors(options =>
    options.AddPolicy("myclient",policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddDbContext<IntegrationDbContext>(x =>
{
    x.UseNpgsql("Server=db;Port=5432;Database=Demo2;User Id=Baturhan;Password=649471;");
});
builder.Services.AddDbContext<HeadDbContext>(x =>
{
    x.UseNpgsql(builder.Configuration.GetConnectionString("Main"));
});
builder.Services.AddSharedSettings();
builder.Services.AddUserServices<ApplicationUser, RootLogin, IntegrationDbContext>();
builder.Services.AddScoped<ILogDal, EfLogDal>();
builder.Services.AddScoped<ApplicationLogManager>();
builder.Services.Configure<Shared.Security.Jwt.TokenOptions>(builder.Configuration.GetSection("JwtTokenOptions"));

builder.Services.AddScoped<AuthManager>();
builder.Services.AddScoped<IApplicationUserDal,EfApplicationUserDal>();
builder.Services.AddScoped<ApplicationUserManager>();
builder.Services.AddScoped<IBranchOfficeDal,EfBranchOfficeDal>();
builder.Services.AddScoped<BranchOfficeManager>();


builder.Services.AddScoped<IValidator<BranchOffice>, BranchValidator>();
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
