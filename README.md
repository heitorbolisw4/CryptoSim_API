# Crypto — API de Corretora de Criptomoedas

API REST desenvolvida em **ASP.NET Core (.NET 10)** para gerenciamento de uma corretora de criptomoedas. O sistema permite cadastro de usuários, criação de carteiras e controle de saldos em BRL e criptomoedas.

---

## Tecnologias

| Tecnologia | Versão | Uso |
|---|---|---|
| .NET | 10.0 | Framework principal |
| ASP.NET Core | 10.0 | API REST |
| Entity Framework Core | 9.0.12 | ORM |
| PostgreSQL + Npgsql | 9.0.1 | Banco de dados |
| BCrypt.Net-Next | 4.2.0 | Hash de senhas |
| Swashbuckle (Swagger) | 9.0.1 | Documentação da API |

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/download/) (v14+)

---

## Configuração

### 1. Clone o repositório

```bash
git clone <url-do-repositório>
cd Crypto
```

### 2. Configure a string de conexão com o banco

Use User Secrets para não expor credenciais no repositório:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=crypto_db;Username=seu_usuario;Password=sua_senha"
```

### 3. Aplique as migrations

```bash
dotnet ef database update
```

### 4. Execute o projeto

```bash
dotnet run
```

A API estará disponível em `http://localhost:5196`.  
O Swagger UI estará em `http://localhost:5196/swagger` (apenas em Development).

---

## Estrutura do Projeto

```
Crypto/
├── Data/
│   └── AppDbContext.cs          # DbContext e configuração do modelo
├── DTO/
│   └── UserRegisterRequestDto.cs
├── Entities/
│   ├── Usuario.cs
│   ├── Carteira.cs
│   ├── Moeda.cs
│   └── SaldoCripto.cs
├── Migrations/
├── Artefatos/
│   ├── DER - CORRETORA CRIPTO.pdf
│   └── problema.pdf
├── Program.cs
└── Crypto.csproj
```

---

## Modelo de Dados

```
Usuario ──────────── Carteira
                         │
                    SaldoCripto ──── Moeda
```

- **Usuario** — dados do usuário (nome, CPF, e-mail, senha hasheada)
- **Carteira** — saldo em BRL de cada usuário (1:1 com Usuario)
- **Moeda** — criptomoedas disponíveis na plataforma (símbolo único)
- **SaldoCripto** — quantidade de cada moeda em uma carteira (N:1 com Carteira)

---

## Comandos úteis

```bash
# Executar com hot reload
dotnet watch run

# Criar nova migration
dotnet ef migrations add NomeDaMigration

# Aplicar migrations
dotnet ef database update

# Reverter última migration
dotnet ef database update NomeDaMigrationAnterior

# Build
dotnet build
```

---

## Artefatos

- `Artefatos/DER - CORRETORA CRIPTO.pdf` — Diagrama Entidade-Relacionamento
- `Artefatos/problema.pdf` — Especificação dos requisitos
# CryptoSim_API
