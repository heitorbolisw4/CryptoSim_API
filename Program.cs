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

    // crio a carteira

    await db.Usuarios.AddAsync(user);
    Carteira carteira = new Carteira
    {
      Id = Guid.NewGuid(),
      UsuarioId = user.Id,
      DataCriacao = DateTime.UtcNow  
    };
    await db.Carteiras.AddAsync(carteira);
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

app.MapGet("/coins", async (AppDbContext db) =>
{
   var moedas = await db.Moedas.Where(m => m.Ativo == true).Select(m => new MoedaResponseDto
   {
       Simbolo = m.Simbolo,
       Nome = m.Nome
   }).ToListAsync();

   return Results.Ok(moedas);
});
//definir um mapgroup
var users = app.MapGroup("/me").RequireAuthorization();

users.MapPost("/transacao", async (Guid id, TransacaoFiatRegisterDto request, AppDbContext db, ClaimsPrincipal user) =>
{
    if(string.IsNullOrEmpty(request.Tipo) || request.Valor == 0)
        return Results.BadRequest("Todos os campos devem ser preenchidos");

    //pego o claim
    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    // identifico o user
    if(string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    // identifica carteira
    var carteira = await db.Carteiras.FirstOrDefaultAsync(c => c.UsuarioId == userId);

    if(carteira == null)
        return Results.BadRequest();

    TransacaoFiat transacao = new TransacaoFiat
    {
        Id = Guid.NewGuid(),
        CarteiraId = carteira.Id,
        Tipo = request.Tipo,
        Valor = request.Valor,
        DataHora = DateTime.UtcNow,
        Status = "Pendente"
        

    };
    db.Transacoes.Add(transacao);
    await db.SaveChangesAsync();
    return Results.Created();

});

users.MapPatch("/transacao/pagamento", async (Guid id, AppDbContext db, ClaimsPrincipal user, PagamentoTransacaoRequestDto request) =>
{
    if(string.IsNullOrWhiteSpace(request.Status))
        return Results.BadRequest("Deve informar um status valido ex: Concluido/Cancelado");

    var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

    if(string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    //identifico a carteira
    var carteira = await db.Carteiras.FirstOrDefaultAsync(c => c.UsuarioId == userId);

    if(carteira == null)
        return Results.BadRequest();
    //identifico a transacao
    var transacao = await db.Transacoes.FirstOrDefaultAsync( t => t.Id == id && t.CarteiraId == carteira.Id);

    if(transacao == null)
        return Results.BadRequest();

    transacao.Status = request.Status;
    await db.SaveChangesAsync();

    if(transacao.Status == "Aprovado")
    {
        carteira.SaldoBrl += transacao.Valor;
    }
    await db.SaveChangesAsync();
    return Results.Ok();

    
});


app.UseAuthentication();
app.UseAuthorization();
app.Run();
