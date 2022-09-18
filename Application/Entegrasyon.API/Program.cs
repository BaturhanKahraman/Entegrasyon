using System.Data.Common;
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Extensions;
using Shared.User;
using System.Text;
using System.Text.Json.Serialization;
using Entegrasyon.Entity.Dtos.Users;
using Shared.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(x =>
{
    //x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
});
builder.Services.AddLogging();
builder.Services.Configure<ApiBehaviorOptions>(o=>o.SuppressModelStateInvalidFilter=true);
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior",true);

builder.Services.AddCors(options =>
    options.AddPolicy("myclient",policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddDbContext<IntegrationDbContext>(x =>
{
    x.UseNpgsql("Server=db;Port=5432;Database=IntegrationDb2;User Id=Baturhan;Password=649471;Pooling=true;Maximum Pool Size=1024;ConnectionIdleLifetime=120;Include Error Detail=true;");
});
builder.Services.AddDbContext<HeadDbContext>(x =>
{
    x.UseNpgsql(builder.Configuration.GetConnectionString("Main"));
});



builder.Services.AddSharedSettings();
builder.Services.AddUserServices<ApplicationUser, RootLogin,RootRole,RootClaim,IntegrationDbContext>();
builder.Services.AddScoped<ILogDal, EfLogDal>();
builder.Services.AddScoped<ApplicationLogManager>();
builder.Services.Configure<Shared.Security.Jwt.TokenOptions>(builder.Configuration.GetSection("JwtTokenOptions"));

builder.Services.AddScoped<AuthManager>();
builder.Services.AddScoped<IApplicationUserDal,EfApplicationUserDal>();
builder.Services.AddScoped<ApplicationUserManager>();
builder.Services.AddScoped<IBranchOfficeDal,EfBranchOfficeDal>();
builder.Services.AddScoped<BranchOfficeManager>();
builder.Services.AddScoped<ApplicationRoleManager>();
builder.Services.AddScoped<CategoryManager>();
builder.Services.AddScoped<ICategoryDal, EfCategoryDal>();

builder.Services.AddScoped<IValidator<BranchOffice>, BranchValidator>();
builder.Services.AddScoped<IValidator<AddRoleDto>,AddRoleDtoValidator>();
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
builder.Services.AddHttpClient();
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

app.UseMiddleware<MigrateDatabaseMiddleware>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.AddJwtBlacklistMiddleware();
app.MapControllers();

app.Run();
