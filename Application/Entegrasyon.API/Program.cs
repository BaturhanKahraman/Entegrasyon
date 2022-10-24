using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using MainDatabase.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Extensions;
using System.Text;
using Entegrasyon.Business.Extensions;
using Shared.FileStorage;
using Shared.FileStorage.Options;
using Shared.Middlewares;

var builder = WebApplication.CreateBuilder(args);
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior",true);
builder.Services.AddControllers();
builder.Services.AddLogging();
builder.Services.Configure<ApiBehaviorOptions>(o=>o.SuppressModelStateInvalidFilter=true);
builder.Services.Configure<Shared.Security.Jwt.TokenOptions>(builder.Configuration.GetSection("JwtTokenOptions"));
builder.Services.AddApplicationDependencies();
builder.Services.AddFileStorageCore();
builder.Services.AddLocalFileStorage(x=>builder.Configuration.GetSection("LocalFileStorageOptions"));
//builder.Services.AddLocalFileStorage(new LocalFileStorageOption{RootPath = "wwwroot"});

builder.Services.AddDbContext<HeadDbContext>(x =>
{
    x.UseNpgsql(builder.Configuration.GetConnectionString("Main"));
});
builder.Services.AddCors(options =>
    options.AddPolicy("myclient",policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddDbContext<IntegrationDbContext>(x =>
{
    x.UseNpgsql("Server=db;Port=5432;Database=IntegrationDb00;User Id=Baturhan;Password=649471;Pooling=true;Maximum Pool Size=1024;ConnectionIdleLifetime=120;Include Error Detail=true;",
        npg=>
        {
            npg.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            npg.EnableRetryOnFailure(5,TimeSpan.FromSeconds(10),null);
        });
    //x.UseNpgsql("Server=localhost;Port=5432;Database=IntegrationDb111;User Id=postgres;Password=649471;Pooling=true;Maximum Pool Size=1024;ConnectionIdleLifetime=120;Include Error Detail=true;");
    x.EnableDetailedErrors();

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
builder.Services.AddHttpClient();
var app = builder.Build();

app.AddCustomExceptionHandlerMiddleware();
// Configure the HTTP request pipeline.
if(app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseMiddleware<MigrateDatabaseMiddleware>();
}

app.UseCors("myclient");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.AddJwtBlacklistMiddleware();
app.MapControllers();

app.Run();
