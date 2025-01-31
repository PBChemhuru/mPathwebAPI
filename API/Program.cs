using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;


var builder = WebApplication.CreateBuilder(args);


// Load the JWT settings from the configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings.GetValue<string>("SecretKey");
var issuer = jwtSettings.GetValue<string>("Issuer");
var audience = jwtSettings.GetValue<string>("Audience");

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

// Add JWT Bearer authentication to the services
//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
//    {
//        options.TokenValidationParameters = new TokenValidationParameters
//        {
 //           ValidateIssuer = true,
 //           ValidateAudience = true,
 //           ValidIssuer = issuer,
  //          ValidAudience = audience,
 //          IssuerSigningKey = key
  //      };
 //   });

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// Register Swagger services
builder.Services.AddSwaggerGen();
//JSON Serializer
builder.Services.AddControllersWithViews().AddNewtonsoftJson();
builder.Services.AddAuthorization();
var app = builder.Build();
//sors
app.UseCors(x => x.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // Enable Swagger in Development environment
    app.UseSwaggerUI(); // Enable the Swagger UI page
    app.MapOpenApi();
}

app.UseHttpsRedirection();
//app.UseAuthentication();
//app.UseAuthorization();

app.MapControllers();

app.Run();
