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

//Add JWT Bearer authentication to the services

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
                                            {
                                                ValidateIssuer = true,
                                                ValidateAudience = true,
                                                ValidIssuer = issuer,
                                                ValidAudience = audience,
                                                IssuerSigningKey = key
                                            };
    });

//Add services to the container.

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
//sors change from anyorigin
app.UseCors(x => x.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());

app.Use(async (context, next) =>
{
    // Content Security Policy (CSP) to prevent XSS
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self';";

    // Prevent Clickjacking
    context.Response.Headers["X-Frame-Options"] = "DENY";

    // Enable XSS Protection
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

    // Prevent MIME Type Sniffing
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";

    // Enable Strict Transport Security (HSTS)
    context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";  // 1 year

    await next();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); 
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"); 
        c.RoutePrefix = string.Empty;
    });
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
