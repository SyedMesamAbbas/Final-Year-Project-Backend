using HouseofTutorAPI.Models;
using HouseofTutorAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var jwtSettings = builder.Configuration.GetSection("Jwt");

// JWT
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    jwtSettings["Key"] ??
                    throw new InvalidOperationException("JWT Key is missing.")
                )
            )
        };
    });

// FILE UPLOAD
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024;
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// KESTREL
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 100 * 1024 * 1024;
    options.Limits.MinRequestBodyDataRate = null;
    options.Limits.MinResponseDataRate = null;
});

builder.WebHost.UseUrls("http://0.0.0.0:5000");
// CONTROLLERS
builder.Services.AddControllers();

// SWAGGER
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DATABASE
builder.Services.AddDbContext<HouseofTutorContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("dbcs")
    );
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactNative", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// SERVICES
builder.Services.AddHttpClient<GeoService>();
builder.Services.AddHostedService<TutorRequestQueueService>();

// BUILD
var app = builder.Build();

app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

app.UseCors("AllowReactNative");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();














//using HouseofTutorAPI.Models;
//using Microsoft.EntityFrameworkCore;

//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.IdentityModel.Tokens;
//using System.Text;

//var builder = WebApplication.CreateBuilder(args);

//var jwtSettings = builder.Configuration.GetSection("Jwt");

//// -------------------------
//// JWT Authentication
//// -------------------------
//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
//    {
//        options.RequireHttpsMetadata = false;
//        options.SaveToken = true;

//        options.TokenValidationParameters = new TokenValidationParameters
//        {
//            ValidateIssuer = true,
//            ValidateAudience = true,
//            ValidateLifetime = true,
//            ValidateIssuerSigningKey = true,

//            ValidIssuer = jwtSettings["Issuer"],
//            ValidAudience = jwtSettings["Audience"],
//            IssuerSigningKey = new SymmetricSecurityKey(
//                Encoding.UTF8.GetBytes(jwtSettings["Key"])
//            )
//        };
//    });

//// -------------------------
//// 1️⃣ Add services to the container
//// -------------------------
//builder.Services.AddControllers();

//// Swagger
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//// -------------------------
//// 2️⃣ Add DbContext
//// -------------------------
//builder.Services.AddDbContext<HouseofTutorContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("dbcs"))
//);

//builder.Services.AddHttpClient<GeoService>();

//// -------------------------
//// 3️⃣ CORS (React Native)
//// -------------------------
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowReactNative",
//        policy =>
//        {
//            policy.AllowAnyOrigin()
//                  .AllowAnyMethod()
//                  .AllowAnyHeader();
//        });
//});

//// Load appsettings
//builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

//// -------------------------
//// 4️⃣ Build app
//// -------------------------
//var app = builder.Build();

//// -------------------------
//// 5️⃣ Middleware pipeline
//// -------------------------
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();

//// ✅ Use CORS
//app.UseCors("AllowReactNative");

//app.UseAuthentication();
//app.UseAuthorization();

//app.MapControllers();

//// -------------------------
//app.Run();




























////using HouseofTutorAPI.Models;
////using HouseofTutorAPI.Services;
////using Microsoft.EntityFrameworkCore;

////using Microsoft.AspNetCore.Authentication.JwtBearer;
////using Microsoft.IdentityModel.Tokens;
////using System.Text;

////var builder = WebApplication.CreateBuilder(args);
////var jwtSettings = builder.Configuration.GetSection("Jwt");

////builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
////    .AddJwtBearer(options =>
////    {
////        options.RequireHttpsMetadata = false;
////        options.SaveToken = true;

////        options.TokenValidationParameters = new TokenValidationParameters
////        {
////            ValidateIssuer = true,
////            ValidateAudience = true,
////            ValidateLifetime = true,
////            ValidateIssuerSigningKey = true,

////            ValidIssuer = jwtSettings["Issuer"],
////            ValidAudience = jwtSettings["Audience"],
////            IssuerSigningKey = new SymmetricSecurityKey(
////                Encoding.UTF8.GetBytes(jwtSettings["Key"])
////            )
////        };
////    });

////// -------------------------
////// 1️⃣ Add services to the container
////// -------------------------
////builder.Services.AddControllers();

////// Swagger
////builder.Services.AddEndpointsApiExplorer();
////builder.Services.AddSwaggerGen();

////// -------------------------
////// 2️⃣ Add DbContext
////// -------------------------
////builder.Services.AddDbContext<HouseofTutorContext>(options =>
////    options.UseSqlServer(builder.Configuration.GetConnectionString("dbcs"))
////);

////// -------------------------
////// 3️⃣ ✅ ADD CORS (IMPORTANT)
////// -------------------------
////builder.Services.AddCors(options =>
////{
////    options.AddPolicy("AllowReactNative",
////        policy =>
////        {
////            policy.AllowAnyOrigin()   // allow mobile app
////                  .AllowAnyMethod()
////                  .AllowAnyHeader();
////        });
////});
////builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
////// -------------------------
////// 4️⃣ Google Geocoding Service
////// -------------------------
////builder.Services.AddHttpClient<GeocodingService>();
////builder.Services.AddHttpClient<GeoService>();

////// -------------------------
////// 5️⃣ Build app
////// -------------------------
////var app = builder.Build();

////// -------------------------
////// 6️⃣ Middleware pipeline
////// -------------------------
////if (app.Environment.IsDevelopment())
////{
////    app.UseSwagger();
////    app.UseSwaggerUI();
////}

////app.UseHttpsRedirection();

////// ✅ USE CORS HERE
////app.UseCors("AllowReactNative");
////app.UseAuthentication();
////app.UseAuthorization();

////app.MapControllers();

////// -------------------------
////app.Run();