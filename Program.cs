using System.Globalization;
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


builder.Services.AddHttpClient<CoinGeckoService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICoinGeckoService, CoinGeckoService>();
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

app.MapGet("/coins", async (AppDbContext db, ICoinGeckoService coinService) =>
{
    await coinService.AtualizarCotacoesAsync();
   var moedas = await db.Moedas.Where(m => m.Ativo == true).Select(m => new MoedaResponseDto
   {
       Simbolo = m.Simbolo,
       Nome = m.Nome,
       PrecoBrl  = m.Cotacoes!.OrderByDescending(c => c.DataHora)
       .Select(c => c.PrecoBrl)
       .FirstOrDefault()
   }).ToListAsync();

   return Results.Ok(moedas);
});

// cultura usada para formatar valores monetarios (R$) nos logs de operacao
var ptBR = CultureInfo.GetCultureInfo("pt-BR");

var transacoes = app.MapGroup("/transacoes").RequireAuthorization().WithTags("Transacoes");
var ordens = app.MapGroup("/ordens").RequireAuthorization().WithTags("Ordens");
var carteiras = app.MapGroup("/carteira").RequireAuthorization().WithTags("Carteira");
var user = app.MapGroup("/me").RequireAuthorization().WithTags("Usuario");

user.MapGet("/perfil", async (AppDbContext db, ClaimsPrincipal user) =>
{
    var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

    if(string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var perfil = await db.Usuarios.Where( u => u.Id == userId).Select(c => new UserResponseDto
    {
        Nome = c.Nome,
        Email = c.Email
    }).ToListAsync();
    return Results.Ok(perfil);
});

user.MapPatch("/perfil", async (PerfilUserRequestDto request, AppDbContext db, ClaimsPrincipal user) =>
{
    var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == userId);

    if (usuario == null)
        return Results.NotFound();

    if (!string.IsNullOrWhiteSpace(request.Nome))
        usuario.Nome = request.Nome;

    if (!string.IsNullOrWhiteSpace(request.Email))
    {
        bool emailEmUso = await db.Usuarios.AnyAsync(u => u.Email == request.Email && u.Id != userId);
        if (emailEmUso)
            return Results.BadRequest("Email já está em uso");

        usuario.Email = request.Email;
    }

    await db.SaveChangesAsync();
    return Results.Ok();
});

user.MapPut("/perfil/email", async (PerfilUserRequestDto request, AppDbContext db, ClaimsPrincipal user) =>
{
    var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(request.Email))
        return Results.BadRequest("Email é obrigatório");

    var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == userId);

    if (usuario == null)
        return Results.NotFound();

    bool emailEmUso = await db.Usuarios.AnyAsync(u => u.Email == request.Email && u.Id != userId);
    if (emailEmUso)
        return Results.BadRequest("Email já está em uso");

    usuario.Email = request.Email;

    await db.SaveChangesAsync();
    return Results.Ok();
});

user.MapDelete("/perfil", async (AppDbContext db, ClaimsPrincipal user) =>
{
    var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == userId);

    if (usuario == null)
        return Results.NotFound();

    usuario.Ativo = false;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

transacoes.MapPost("/", async (TransacaoFiatRegisterDto request, AppDbContext db, ClaimsPrincipal user) =>
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

    db.LogOperacoes.Add(new LogOperacao
    {
        Id = Guid.NewGuid(),
        UsuarioId = userId,
        Evento = "TransacaoFiat",
        Descricao = $"Depósito de {request.Valor.ToString("C", ptBR)}",
        DataHora = DateTime.UtcNow
    });

    await db.SaveChangesAsync();
    return Results.Created();

});

transacoes.MapGet("/", async (AppDbContext db, ClaimsPrincipal user) =>
{
    var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var carteira = await db.Carteiras.FirstOrDefaultAsync(c => c.UsuarioId == userId);

    if (carteira == null)
        return Results.NotFound("Carteira não encontrada");

    var minhasTransacoes = await db.Transacoes
        .Where(t => t.CarteiraId == carteira.Id)
        .OrderByDescending(t => t.DataHora)
        .Select(t => new TransacaoFiatResponseDto
        {
            Id = t.Id,
            Tipo = t.Tipo,
            Valor = t.Valor,
            Status = t.Status,
            DataHora = t.DataHora
        })
        .ToListAsync();

    return Results.Ok(minhasTransacoes);
});

transacoes.MapPatch("/{id:guid}/pagamento", async (Guid id, AppDbContext db, ClaimsPrincipal user, PagamentoTransacaoRequestDto request) =>
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

ordens.MapPost("/compra/corretora/{id:guid}", async (Guid id, OrdemRequestDto request, AppDbContext db, ClaimsPrincipal user) =>
{
    var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var carteira = await db.Carteiras.FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == userId);

    if (carteira == null)
        return Results.NotFound("Carteira não encontrada");

    var cotacao = await db.Cotacoes
        .Where(c => c.MoedaId == request.MoedaId)
        .OrderByDescending(c => c.DataHora)
        .FirstOrDefaultAsync();

    if (cotacao == null)
        return Results.BadRequest("Cotação não encontrada para a moeda solicitada");

    var custoTotal = request.Quantidade * cotacao.PrecoBrl;

    if (carteira.SaldoBrl < custoTotal)
        return Results.BadRequest("Saldo insuficiente");

    carteira.SaldoBrl -= custoTotal;

    var saldoCripto = await db.SaldoCriptos
        .FirstOrDefaultAsync(s => s.CarteiraId == carteira.Id && s.MoedaId == request.MoedaId);

    if (saldoCripto == null)
    {
        db.SaldoCriptos.Add(new SaldoCripto
        {
            CarteiraId = carteira.Id,
            MoedaId = request.MoedaId,
            Quantidade = request.Quantidade
        });
    }
    else
    {
        saldoCripto.Quantidade += request.Quantidade;
    }

    db.Ordens.Add(new Ordem
    {
        Id = Guid.NewGuid(),
        CarteiraId = carteira.Id,
        MoedaId = request.MoedaId,
        Tipo = "Compra",
        Quantidade = request.Quantidade,
        PrecoUnitarioBrl = cotacao.PrecoBrl,
        Status = "aprovada",
        DataHora = DateTime.UtcNow
    });

    var moeda = await db.Moedas.FirstOrDefaultAsync(m => m.Id == request.MoedaId);

    db.LogOperacoes.Add(new LogOperacao
    {
        Id = Guid.NewGuid(),
        UsuarioId = userId,
        Evento = "CompraCorretora",
        Descricao = $"Compra de {request.Quantidade.ToString(CultureInfo.InvariantCulture)} {moeda?.Simbolo} por {custoTotal.ToString("C", ptBR)}",
        DataHora = DateTime.UtcNow
    });

    await db.SaveChangesAsync();
    return Results.Created();
});

ordens.MapGet("/", async (AppDbContext db) =>
{
    var ordens = await db.Ordens
        .Where(o => o.Status == "postada" && o.Tipo == "Venda")
        .Select(o => new OrdemResponseDto
        {
            Id = o.Id,
            CarteiraId = o.CarteiraId,
            MoedaId = o.MoedaId,
            Quantidade = o.Quantidade,
            PrecoUnitarioBrl = o.PrecoUnitarioBrl,
            DataHora = o.DataHora
        })
        .ToListAsync();

    return Results.Ok(ordens);
});

ordens.MapPost("/compra/{ordemId:guid}", async (Guid ordemId, AppDbContext db, ClaimsPrincipal user) =>
{
    var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var carteira = await db.Carteiras.FirstOrDefaultAsync(c => c.UsuarioId == userId);

    if (carteira == null)
        return Results.NotFound("Carteira não encontrada");

    var ordem = await db.Ordens.FirstOrDefaultAsync(o => o.Id == ordemId && o.Status == "postada" && o.Tipo == "Venda");

    if (ordem == null)
        return Results.NotFound("Ordem não encontrada ou não disponível");

    if (ordem.CarteiraId == carteira.Id)
        return Results.BadRequest("Não é possível comprar sua própria ordem");

    var custoTotal = ordem.Quantidade * ordem.PrecoUnitarioBrl;

    if (carteira.SaldoBrl < custoTotal)
        return Results.BadRequest("Saldo insuficiente");

    carteira.SaldoBrl -= custoTotal;
    ordem.CompradorCarteiraId = carteira.Id;
    ordem.Status = "em transacao";

    var moeda = await db.Moedas.FirstOrDefaultAsync(m => m.Id == ordem.MoedaId);

    db.LogOperacoes.Add(new LogOperacao
    {
        Id = Guid.NewGuid(),
        UsuarioId = userId,
        Evento = "CompraP2P",
        Descricao = $"Solicitação de compra P2P de {ordem.Quantidade.ToString(CultureInfo.InvariantCulture)} {moeda?.Simbolo}",
        DataHora = DateTime.UtcNow
    });

    await db.SaveChangesAsync();
    return Results.Ok();
});

ordens.MapPatch("/{ordemId:guid}/aprovar", async (Guid ordemId, AppDbContext db, ClaimsPrincipal user) =>
{
    var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var carteiraVendedor = await db.Carteiras.FirstOrDefaultAsync(c => c.UsuarioId == userId);

    if (carteiraVendedor == null)
        return Results.NotFound("Carteira não encontrada");

    var ordem = await db.Ordens.FirstOrDefaultAsync(o => o.Id == ordemId && o.CarteiraId == carteiraVendedor.Id && o.Status == "em transacao");

    if (ordem == null)
        return Results.NotFound("Ordem não encontrada ou não pertence a você");

    await db.Database.ExecuteSqlRawAsync("CALL sp_aprovar_ordem({0})", ordemId);

    db.LogOperacoes.Add(new LogOperacao
    {
        Id = Guid.NewGuid(),
        UsuarioId = userId,
        Evento = "OrdemAprovada",
        Descricao = $"Ordem {ordem.Id} aprovada",
        DataHora = DateTime.UtcNow
    });

    await db.SaveChangesAsync();

    return Results.Ok();
});

ordens.MapPatch("/{ordemId:guid}/cancelar", async (Guid ordemId, AppDbContext db, ClaimsPrincipal user) =>
{
    var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var carteiraVendedor = await db.Carteiras.FirstOrDefaultAsync(c => c.UsuarioId == userId);

    if (carteiraVendedor == null)
        return Results.NotFound("Carteira não encontrada");

    var ordem = await db.Ordens.FirstOrDefaultAsync(o => o.Id == ordemId && o.CarteiraId == carteiraVendedor.Id && (o.Status == "postada" || o.Status == "em transacao"));

    if (ordem == null)
        return Results.NotFound("Ordem não encontrada ou não pode ser cancelada");

    var saldoCriptoVendedor = await db.SaldoCriptos
        .FirstOrDefaultAsync(s => s.CarteiraId == carteiraVendedor.Id && s.MoedaId == ordem.MoedaId);

    if (saldoCriptoVendedor == null)
    {
        db.SaldoCriptos.Add(new SaldoCripto
        {
            CarteiraId = carteiraVendedor.Id,
            MoedaId = ordem.MoedaId,
            Quantidade = ordem.Quantidade
        });
    }
    else
    {
        saldoCriptoVendedor.Quantidade += ordem.Quantidade;
    }

    if (ordem.Status == "em transacao" && ordem.CompradorCarteiraId != null)
    {
        var carteiraComprador = await db.Carteiras.FirstOrDefaultAsync(c => c.Id == ordem.CompradorCarteiraId);
        if (carteiraComprador != null)
            carteiraComprador.SaldoBrl += ordem.Quantidade * ordem.PrecoUnitarioBrl;
    }

    ordem.Status = "cancelada";

    db.LogOperacoes.Add(new LogOperacao
    {
        Id = Guid.NewGuid(),
        UsuarioId = userId,
        Evento = "OrdemCancelada",
        Descricao = $"Ordem {ordem.Id} cancelada",
        DataHora = DateTime.UtcNow
    });

    await db.SaveChangesAsync();
    return Results.Ok();
});

ordens.MapDelete("/{ordemId:guid}", async (Guid ordemId, AppDbContext db, ClaimsPrincipal user) =>
{
    var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var carteiraVendedor = await db.Carteiras.FirstOrDefaultAsync(c => c.UsuarioId == userId);

    if (carteiraVendedor == null)
        return Results.NotFound("Carteira não encontrada");

    var ordem = await db.Ordens.FirstOrDefaultAsync(o => o.Id == ordemId && o.CarteiraId == carteiraVendedor.Id && o.Status == "postada");

    if (ordem == null)
        return Results.NotFound("Ordem não encontrada ou não pode ser excluída");

    var saldoCripto = await db.SaldoCriptos.FirstOrDefaultAsync(s => s.CarteiraId == carteiraVendedor.Id && s.MoedaId == ordem.MoedaId);

    if (saldoCripto == null)
        db.SaldoCriptos.Add(new SaldoCripto { CarteiraId = carteiraVendedor.Id, MoedaId = ordem.MoedaId, Quantidade = ordem.Quantidade });
    else
        saldoCripto.Quantidade += ordem.Quantidade;

    db.Ordens.Remove(ordem);

    await db.SaveChangesAsync();
    return Results.NoContent();
});

ordens.MapPut("/{ordemId:guid}/editar", async (Guid ordemId, OrdemEditRequestDto request, AppDbContext db, ClaimsPrincipal user) =>
{
    var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    if (request.Quantidade <= 0)
        return Results.BadRequest("Quantidade deve ser maior que zero");

    var carteiraVendedor = await db.Carteiras.FirstOrDefaultAsync(c => c.UsuarioId == userId);

    if (carteiraVendedor == null)
        return Results.NotFound("Carteira não encontrada");

    var ordem = await db.Ordens.FirstOrDefaultAsync(o => o.Id == ordemId && o.CarteiraId == carteiraVendedor.Id && o.Status == "postada");

    if (ordem == null)
        return Results.NotFound("Ordem não encontrada ou não pode ser editada");

    var saldoCripto = await db.SaldoCriptos.FirstOrDefaultAsync(s => s.CarteiraId == carteiraVendedor.Id && s.MoedaId == ordem.MoedaId);

    if (saldoCripto == null)
        return Results.BadRequest("Saldo cripto não encontrado");

    var diferenca = request.Quantidade - ordem.Quantidade;

    if (diferenca > 0 && saldoCripto.Quantidade < diferenca)
        return Results.BadRequest("Saldo de cripto insuficiente para aumentar a quantidade");

    saldoCripto.Quantidade -= diferenca;

    var cotacao = await db.Cotacoes
        .Where(c => c.MoedaId == ordem.MoedaId)
        .OrderByDescending(c => c.DataHora)
        .FirstOrDefaultAsync();

    if (cotacao == null)
        return Results.BadRequest("Cotação não encontrada");

    ordem.Quantidade = request.Quantidade;
    ordem.PrecoUnitarioBrl = cotacao.PrecoBrl;

    await db.SaveChangesAsync();
    return Results.Ok();
});

ordens.MapPost("/venda/{id:guid}", async (Guid id, OrdemRequestDto request, AppDbContext db, ClaimsPrincipal user) =>
{
    var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

    if(string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var carteira = await db.Carteiras.FirstOrDefaultAsync( c => c.Id == id && c.UsuarioId == userId);

    if(carteira == null)
        return Results.NotFound("Carteira não encontrada");

    var cotacao = await db.Cotacoes
    .Where(c => c.MoedaId == request.MoedaId)
    .OrderByDescending(c => c.DataHora)
    .FirstOrDefaultAsync();

    if(cotacao == null)
        return Results.BadRequest("Cotacao não encontrada");

    var saldoCripto = await db.SaldoCriptos.FirstOrDefaultAsync(s => s.CarteiraId == carteira.Id && s.MoedaId == request.MoedaId);
    if(saldoCripto == null)
        return Results.BadRequest();
    
    
    if (saldoCripto.Quantidade < request.Quantidade)
        return Results.BadRequest("Saldo de cripto insuficiente");

    saldoCripto.Quantidade -= request.Quantidade;

    db.Ordens.Add(new Ordem
    {
        Id = Guid.NewGuid(),
        CarteiraId = carteira.Id,
        MoedaId = request.MoedaId,
        Tipo = "Venda",
        Quantidade = request.Quantidade,
        PrecoUnitarioBrl = cotacao.PrecoBrl,
        Status = "postada",
        DataHora = DateTime.UtcNow
    });

    var moeda = await db.Moedas.FirstOrDefaultAsync(m => m.Id == request.MoedaId);

    db.LogOperacoes.Add(new LogOperacao
    {
        Id = Guid.NewGuid(),
        UsuarioId = userId,
        Evento = "OrdemVenda",
        Descricao = $"Venda de {request.Quantidade.ToString(CultureInfo.InvariantCulture)} {moeda?.Simbolo} postada",
        DataHora = DateTime.UtcNow
    });

    await db.SaveChangesAsync();
    return Results.Created();

});


ordens.MapGet("/minhas", async (AppDbContext db, ClaimsPrincipal user) =>
{
    var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var carteira = await db.Carteiras.FirstOrDefaultAsync(c => c.UsuarioId == userId);

    if (carteira == null)
        return Results.NotFound("Carteira não encontrada");

    var minhasOrdens = await db.Ordens
        .Where(o => o.CarteiraId == carteira.Id || o.CompradorCarteiraId == carteira.Id)
        .OrderByDescending(o => o.DataHora)
        .Select(o => new OrdemMinhaResponseDto
        {
            Id = o.Id,
            MoedaId = o.MoedaId,
            Tipo = o.Tipo,
            Quantidade = o.Quantidade,
            PrecoUnitarioBrl = o.PrecoUnitarioBrl,
            Status = o.Status,
            // comprador P2P fica no CompradorCarteiraId; nos demais casos o papel segue o Tipo da ordem
            Papel = o.CompradorCarteiraId == carteira.Id ? "Comprador"
                    : (o.Tipo == "Venda" ? "Vendedor" : "Comprador"),
            DataHora = o.DataHora
        })
        .ToListAsync();

    return Results.Ok(minhasOrdens);
});

carteiras.MapGet("/", async (AppDbContext db, ClaimsPrincipal user) =>
{
    var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var carteira = await db.Carteiras.FirstOrDefaultAsync(c => c.UsuarioId == userId);

    if (carteira == null)
        return Results.NotFound("Carteira não encontrada");

    var saldos = await db.SaldoCriptos
        .Where(s => s.CarteiraId == carteira.Id)
        .Select(s => new SaldoCriptoResponseDto
        {
            MoedaId = s.MoedaId,
            Simbolo = s.Moeda!.Simbolo,
            Nome = s.Moeda.Nome,
            Quantidade = s.Quantidade
        })
        .ToListAsync();

    var response = new CarteiraSaldoResponseDto
    {
        SaldoBrl = carteira.SaldoBrl,
        Saldos = saldos
    };

    return Results.Ok(response);
});

app.UseAuthentication();
app.UseAuthorization();
app.Run();
