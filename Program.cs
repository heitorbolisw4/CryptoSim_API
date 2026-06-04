using System.Security.Claims;
using System.Security.Cryptography.Xml;
using System.Text;
using Crypto.Data;
using Crypto.Dto;
using Crypto.DTO;
using Crypto.Entities;
using Crypto.Interface;
using Crypto.Jwt;
using Crypto.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;


var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();



builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
   options.TokenValidationParameters = new TokenValidationParameters
   {
       ValidateIssuer = true,
       ValidateAudience = true,
       ValidateLifetime = true,
       ValidateIssuerSigningKey = true,

       ValidIssuer = jwtSettings!.Issuer,
       ValidAudience = jwtSettings.Audience,
       IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),

       NameClaimType = ClaimTypes.NameIdentifier
       //RoleClaimType = ClaimTypes.Email

    };
});
builder.Services.AddAuthorization();



builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen( options =>
{
   options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
   {
       Name = "Authorization",
       Type = SecuritySchemeType.Http,
       Scheme = "Bearer",
       BearerFormat = "JWT",
       In = ParameterLocation.Header,
       Description = "Informe o JWT"
   });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        } 
    });
});

var app = builder.Build();

app.MapPost("/register", async (AppDbContext db, UserRegisterRequestDto request, IAuthService authService) =>
{
    // valida se os camos estão vazios

    if(string.IsNullOrWhiteSpace(request.Email) ||
        string.IsNullOrWhiteSpace(request.Senha) ||
        string.IsNullOrWhiteSpace(request.Nome))
        return Results.BadRequest("Todos os campos devem ser preenchidos");


    // verifica se email existe no banco

    bool emailExiste = await db.Usuarios.AnyAsync(u => u.Email == request.Email);

    if (emailExiste)
    {
        return Results.BadRequest("Email Existente");
    }

    string doHashPassw = authService.HashGeneration(request.Senha);
    // instancia o usuario
    
    Usuario user = new Usuario
    {
        
        Nome = request.Nome,
        Email = request.Email,
        SenhaHash = doHashPassw,
        DataCadastro = DateTime.UtcNow,
        Ativo = true
    };

    await db.Usuarios.AddAsync(user);
    await db.SaveChangesAsync();

    return Results.Created();



    // salva no banco
});

app.MapPost("/auth", async (AppDbContext db, UserLoginRequestDto request, IAuthService authService) =>
{
    if(string.IsNullOrWhiteSpace(request.Email)||
        string.IsNullOrWhiteSpace(request.Senha))
        return Results.BadRequest();

    //eu buso o email no banco de dados
    var user = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == request.Email);

    if( user == null)
        return Results.Unauthorized();

    string PassordFromDb = user.SenhaHash;

    bool isValid = authService.PasswordVerify(request.Senha, PassordFromDb);

    if(!isValid)
        return Results.Unauthorized();

    var token = authService.GenerateJwtToken(user);

    return Results.Ok(new {token});
});
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseAuthentication();
app.UseAuthorization();
app.Run();
