# Objetos de Banco de Dados

## Stored Procedure — `sp_aprovar_ordem`

**Por que usar:**
A aprovação de uma ordem P2P envolve três operações em tabelas diferentes ao mesmo tempo:
1. Atualizar o status da `Ordem` para `aprovada`
2. Creditar `SaldoBrl` na carteira do vendedor
3. Creditar `SaldoCripto` na carteira do comprador

Se qualquer etapa falhar no meio, o banco fica em estado inconsistente. A Stored Procedure encapsula tudo em uma transação atômica no PostgreSQL — ou tudo ocorre, ou nada ocorre.

```sql
CREATE OR REPLACE PROCEDURE sp_aprovar_ordem(p_ordem_id UUID)
LANGUAGE plpgsql
AS $$
DECLARE
    v_carteira_vendedor UUID;
    v_carteira_comprador UUID;
    v_moeda_id INT;
    v_quantidade DECIMAL;
    v_preco DECIMAL;
BEGIN
    SELECT "CarteiraId", "CompradorCarteiraId", "MoedaId", "Quantidade", "PrecoUnitarioBrl"
    INTO v_carteira_vendedor, v_carteira_comprador, v_moeda_id, v_quantidade, v_preco
    FROM "Ordens"
    WHERE "Id" = p_ordem_id AND "Status" = 'em transacao';

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Ordem não encontrada ou não está em transação';
    END IF;

    -- Credita vendedor
    UPDATE "Carteiras"
    SET "SaldoBrl" = "SaldoBrl" + (v_quantidade * v_preco)
    WHERE "Id" = v_carteira_vendedor;

    -- Credita comprador (SaldoCripto)
    INSERT INTO "SaldoCriptos" ("CarteiraId", "MoedaId", "Quantidade")
    VALUES (v_carteira_comprador, v_moeda_id, v_quantidade)
    ON CONFLICT ("CarteiraId", "MoedaId")
    DO UPDATE SET "Quantidade" = "SaldoCriptos"."Quantidade" + EXCLUDED."Quantidade";

    -- Aprova a ordem
    UPDATE "Ordens"
    SET "Status" = 'aprovada'
    WHERE "Id" = p_ordem_id;

    COMMIT;
END;
$$;
```

**Como chamar pela API (C#):**
```csharp
await db.Database.ExecuteSqlRawAsync("CALL sp_aprovar_ordem({0})", ordemId);
```

---

## Trigger — `trg_log_status_ordem`

**Por que usar:**
Toda vez que o status de uma `Ordem` muda, queremos registrar automaticamente no `LogOperacao` — sem depender da aplicação para fazer isso manualmente. O Trigger dispara direto no banco após qualquer `UPDATE` na tabela `Ordens`.

```sql
CREATE OR REPLACE FUNCTION fn_registrar_log_ordem()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF OLD."Status" <> NEW."Status" THEN
        INSERT INTO "LogOperacao" ("Id", "UsuarioId", "Evento", "Descricao", "DataHora")
        SELECT
            gen_random_uuid(),
            u."Id",
            'OrdemStatus',
            'Ordem ' || NEW."Id" || ' alterada de ' || OLD."Status" || ' para ' || NEW."Status",
            NOW()
        FROM "Carteiras" c
        JOIN "Usuarios" u ON u."Id" = c."UsuarioId"
        WHERE c."Id" = NEW."CarteiraId";
    END IF;

    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_log_status_ordem
AFTER UPDATE ON "Ordens"
FOR EACH ROW
EXECUTE FUNCTION fn_registrar_log_ordem();
```

---

## View — `vw_book_ordens`

**Por que usar:**
O comprador precisa visualizar as ordens disponíveis com informações legíveis — nome da moeda, preço e quantidade. Em vez de fazer JOIN na aplicação toda vez, a View encapsula essa consulta no banco.

```sql
CREATE OR REPLACE VIEW vw_book_ordens AS
SELECT
    o."Id",
    o."CarteiraId",
    m."Nome" AS "Moeda",
    m."Simbolo",
    o."Quantidade",
    o."PrecoUnitarioBrl",
    o."Quantidade" * o."PrecoUnitarioBrl" AS "ValorTotal",
    o."DataHora"
FROM "Ordens" o
JOIN "Moedas" m ON m."Id" = o."MoedaId"
WHERE o."Status" = 'postada' AND o."Tipo" = 'Venda';
```

**Como usar pela API (C#):**
```csharp
var ordens = await db.Database
    .SqlQueryRaw<OrdemResponseDto>("SELECT * FROM vw_book_ordens")
    .ToListAsync();
```

---

## Function — `fn_registrar_log_ordem`

`fn_registrar_log_ordem` é a function utilizada pelo trigger acima, cumprindo o requisito.
Funções no PostgreSQL são obrigatórias para triggers — o trigger chama a function, a function executa a lógica.
