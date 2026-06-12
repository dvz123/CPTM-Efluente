using System.Security.Claims;
using System.Security.Cryptography;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Cptm.Api.DTOs;
using Cptm.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Oracle.ManagedDataAccess.Client;
using Swashbuckle.AspNetCore.Annotations;

var builder = WebApplication.CreateBuilder(args);

// ====== JWT Authentication ======
var jwtKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey não configurada.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CptmAmbiental";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CPTM Ambiental API",
        Version = "v1",
        Description = "API de gestão de formulários, usuários e referências.",
    });
});

// ====== CORS (Restrito ao frontend) ======
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:4173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ====== Serviços ======
builder.Services.AddSingleton<JwtService>();
builder.Services.AddAntiforgery();

var app = builder.Build();

// ====== Swagger / OpenAPI ======
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "CPTM Ambiental API v1");
    options.RoutePrefix = "swagger";
});

// ====== Middleware (ordem importa) ======
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

// Limitar tamanho do request body (proteção contra upload abusivo)
app.Use(async (context, next) =>
{
    // Segurança: Header para prevenir XSS, clickjacking
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

var connectionString = builder.Configuration.GetConnectionString("OracleConnection")
    ?? throw new InvalidOperationException("Connection string OracleConnection não encontrada.");

static string TraduzirErroOracle(OracleException ex)
{
    var mensagem = ex.Message ?? string.Empty;

    if (ex.Number == 1536 || mensagem.Contains("ORA-01536", StringComparison.OrdinalIgnoreCase))
        return "O banco de dados está sem espaço disponível no momento para gravar as fotos. O formulário pode permanecer salvo localmente para reenvio posterior.";

    if (ex.Number == 1861 || ex.Number == 1843 ||
        mensagem.Contains("ORA-01861", StringComparison.OrdinalIgnoreCase) ||
        mensagem.Contains("ORA-01843", StringComparison.OrdinalIgnoreCase))
        return "Foi identificada uma data inválida em um dos campos do formulário. Revise a data e tente novamente.";

    if (ex.Number == 1722 || mensagem.Contains("ORA-01722", StringComparison.OrdinalIgnoreCase))
        return "Foi identificado um valor numérico inválido no formulário. Revise os campos numéricos e tente novamente.";

    if (ex.Number == 12899 || mensagem.Contains("ORA-12899", StringComparison.OrdinalIgnoreCase))
        return "Um ou mais campos ultrapassaram o tamanho permitido. Revise os textos informados e tente novamente.";

    if (ex.Number == 1400 || mensagem.Contains("ORA-01400", StringComparison.OrdinalIgnoreCase))
        return "Um campo obrigatório para gravação no banco não foi preenchido corretamente. Revise o formulário e tente novamente.";

    if (mensagem.Contains("ORA-00001", StringComparison.OrdinalIgnoreCase))
        return "Já existe um registro com a mesma chave primária. Gere uma nova chave e tente novamente.";

    if (mensagem.Contains("ORA-02291", StringComparison.OrdinalIgnoreCase) ||
        mensagem.Contains("ORA-02292", StringComparison.OrdinalIgnoreCase))
        return "Os dados informados possuem referência inválida para o banco. Revise os campos de identificação e tente novamente.";

    if (mensagem.Contains("ORA-00932", StringComparison.OrdinalIgnoreCase))
        return "Foi identificado tipo de dado incompatível em um dos campos enviados. Revise os dados e tente novamente.";

    Console.WriteLine($"[ERRO ORACLE] Number={ex.Number}; Message={mensagem}");
    return $"Ocorreu um erro interno ao salvar os dados no banco. [Oracle:{ex.Number}]";
}

// ============================================================
// ENDPOINTS
// ============================================================

// ====== Health Check ======
app.MapGet("/", () => Results.Ok(new { status = "API CPTM Ambiental rodando", versao = "2.0" }))
   .WithName("HealthCheck")
   .WithTags("Health")
   .WithMetadata(new SwaggerOperationAttribute
   {
       Summary = "Verifica se a API está ativa.",
       Description = "Health check da API CPTM Ambiental."
   });

// ====== AUTH: Login ======
app.MapPost("/api/auth/login", async (LoginRequest request) =>
{
    // Validação de input
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Senha))
        return Results.BadRequest(new { mensagem = "E-mail e senha são obrigatórios." });

    // Sanitiza o e-mail (previne XSS)
    var emailLimpo = request.Email.Trim().ToLowerInvariant();
    if (emailLimpo.Length > 200)
        return Results.BadRequest(new { mensagem = "E-mail inválido." });

    using var conn = new OracleConnection(connectionString);
    await conn.OpenAsync();

    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT ID, NOME, EMAIL, SENHA_HASH, PERFIL FROM USUARIOS WHERE LOWER(EMAIL) = :email AND ATIVO = 1";
    cmd.Parameters.Add(new OracleParameter("email", emailLimpo));

    using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
        return Results.Json(new { mensagem = "E-mail ou senha inválidos." }, statusCode: 401); // Mensagem útil para frontend

    var id = Convert.ToInt32(reader["ID"]);
    var nome = reader["NOME"]?.ToString() ?? "";
    var email = reader["EMAIL"]?.ToString() ?? "";
    var senhaHash = reader["SENHA_HASH"]?.ToString() ?? "";
    var perfil = reader["PERFIL"]?.ToString() ?? "operador";

    // Verificar senha com SHA256 (em produção usar BCrypt)
    var senhaHashInput = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Senha))).ToLowerInvariant();
    if (senhaHash != senhaHashInput)
        return Results.Json(new { mensagem = "E-mail ou senha inválidos." }, statusCode: 401);

    // Gerar JWT
    var jwtService = app.Services.GetRequiredService<JwtService>();
    var (jwtToken, expiraEm) = jwtService.GerarToken(id, email, perfil, nome);

    // Registrar sessão no Oracle
    using var cmdSessao = conn.CreateCommand();
    cmdSessao.CommandText = "INSERT INTO SESSOES (USUARIO_ID, TOKEN, CRIADO_EM, EXPIRA_EM) VALUES (:p_usuario_id, :p_token, SYSTIMESTAMP, :p_expira_em)";
    cmdSessao.Parameters.Add(new OracleParameter("p_usuario_id", id));
    cmdSessao.Parameters.Add(new OracleParameter("p_token", jwtToken));
    cmdSessao.Parameters.Add(new OracleParameter("p_expira_em", expiraEm));
    await cmdSessao.ExecuteNonQueryAsync();

    return Results.Ok(new LoginResponse
    {
        Id = id,
        Nome = nome,
        Email = email,
        Perfil = perfil,
        Token = jwtToken,
        HashOffline = senhaHashInput, // Para validação offline no IndexedDB
        ExpiraEm = expiraEm
    });
})
   .WithName("Login")
   .WithTags("Autenticação")
   .WithMetadata(new SwaggerOperationAttribute
   {
       Summary = "Autentica o usuário e retorna um token JWT.",
       Description = "Recebe email e senha, e emite token para acesso às rotas protegidas."
   });

// ====== FORMULÁRIOS: Criar (Operador + Supervisor) — grava na PT_EFLUENTE ======
app.MapPost("/api/formularios", [Authorize] async (HttpContext ctx) =>
{
    var userIdClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var userEmail = ctx.User.FindFirst(ClaimTypes.Email)?.Value ?? "";
    if (string.IsNullOrEmpty(userIdClaim))
        return Results.Unauthorized();

    var form = await ctx.Request.ReadFormAsync();
    var dadosJson = form["dados"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(dadosJson))
        return Results.BadRequest(new { mensagem = "Dados do formulário são obrigatórios." });

    if (dadosJson.Length > 50_000)
        return Results.BadRequest(new { mensagem = "Payload excede o tamanho máximo permitido." });

    FormularioRequest? req;
    try
    {
        req = JsonSerializer.Deserialize<FormularioRequest>(dadosJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
    catch
    {
        return Results.BadRequest(new { mensagem = "JSON de dados inválido." });
    }

    if (req == null)
        return Results.BadRequest(new { mensagem = "Dados do formulário inválidos." });

    // Validar fotos: mínimo 4, se arvore: 6
    var fotos = form.Files.Where(f => f.Name == "fotos").ToList();
    var tipo = (req.Natureza ?? "efluente").ToLowerInvariant();
    var minFotos = tipo == "arvore" ? 6 : 4;
    if (fotos.Count < minFotos)
        return Results.BadRequest(new { mensagem = $"Tipo '{tipo}' exige no mínimo {minFotos} fotos. Enviadas: {fotos.Count}" });

    // Validar extensões de fotos (OWASP: prevenir upload malicioso)
    var extensoesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
    var maxTamanhoFoto = 10 * 1024 * 1024;
    foreach (var foto in fotos)
    {
        var ext = Path.GetExtension(foto.FileName).ToLowerInvariant();
        if (!extensoesPermitidas.Contains(ext))
            return Results.BadRequest(new { mensagem = $"Extensão '{ext}' não permitida. Use: jpg, jpeg, png, webp." });
        if (foto.Length > maxTamanhoFoto)
            return Results.BadRequest(new { mensagem = "Foto excede 10MB." });
        if (foto.Length == 0)
            return Results.BadRequest(new { mensagem = "Foto com tamanho zero não é permitida." });
    }

    // Gerar PK — "EFL-{timestamp}-{random}"
    var pk = req.ChavePrimaria;
    if (string.IsNullOrWhiteSpace(pk))
        pk = $"EFL-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8]}";

    static object ParseNullableNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DBNull.Value;

        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : DBNull.Value;
    }

    using var conn = new OracleConnection(connectionString);
    await conn.OpenAsync();
    await using var tx = conn.BeginTransaction();

    try
    {
        // Validar unicidade da PK de negócio para registros ativos
        using (var cmdCheckPk = conn.CreateCommand())
        {
            cmdCheckPk.Transaction = tx;
            cmdCheckPk.CommandText = @"
                SELECT COUNT(*)
                  FROM PT_EFLUENTE
                 WHERE PK_CD_MEIO_AMBIENTE_CPTM = :pk
                   AND (TX_STATUS_DO_REGISTRO_NO_BD IS NULL OR TX_STATUS_DO_REGISTRO_NO_BD != 'excluido')";
            cmdCheckPk.Parameters.Add(new OracleParameter("pk", pk));

            var existePk = Convert.ToInt32(await cmdCheckPk.ExecuteScalarAsync()) > 0;
            if (existePk)
            {
                await tx.RollbackAsync();
                return Results.BadRequest(new { mensagem = "Já existe um registro ativo com a mesma chave primária." });
            }
        }

        // ── INSERT na PT_EFLUENTE ──
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
        INSERT INTO PT_EFLUENTE (
            PK_CD_MEIO_AMBIENTE_CPTM,
            TX_NATUREZA_DO_PGA,
            TX_TIPO_DE_FORMULARIO,
            DT_DATA_EMISSAO_FORMULARIO,
            NR_NUMERO_DE_FORMULARIO,
            TX_AUTOR_PF_DO_FORMULARIO,
            TX_SIGLA_DEPTO_MEIO_AMBIENTE,
            TX_STATUS_DO_DESVIO_AMBIENTAL,
            TX_NOME_PJ_DA_CONTRATADA,
            TX_NR_CONTRATO_CONTRATADA,
            TX_NM_LOCAL_ESCOPO_CONTRATUAL,
            TX_NOME_PJ_EXECUTORA,
            TX_NOME_PJ_DA_SUPERVISORA,
            TX_NM_RESPONSAVEL_CADASTRO,
            TX_RP_RESPONSAVEL_CADASTRO,
            TX_DRT_RESPONSAVEL_CADASTRO,
            TX_NM_AREA_GESTORA_CPTM,
            TX_ID_AREA_GESTORA_CPTM,
            TX_SIGLA_AREA_GESTORA_CPTM,
            TX_NR_ELEMENTO_MONITORAMENTO,
            TX_NM_ELEMENTO_MONITORAMENTO,
            TX_MUNICIPIO,
            TX_LINHA_CPTM,
            TX_ESTACAO_CPTM,
            TX_VIA_CPTM,
            TX_TRECHO_E_SENTIDO_CPTM,
            TX_KM_POSTE,
            NR_LAT_GRAU_DECIMAL_WGS84,
            NR_LONG_GRAU_DECIMAL_WGS84,
            GEOM,
            TX_TIPO_ATIVIDADE_LISTADA,
            TX_TIPO_ATIVIDADE_N_LISTADA,
            TX_TIPO_DRA_LISTADO,
            TX_ID_DRA,
            DT_VALIDADE_DRA,
            TX_TIPO_ATIVIDADE_CPTM,
            TX_NM_LOCAL_ATIV,
            TX_NM_LOCAL_ATIV_COMPLEMENTO,
            TX_ORIGEM_EFLUENTE,
            TX_FONTE_GERADORA,
            TX_TIPO_DESTINACAO,
            NR_QUANTIDADE_L,
            TX_TIPO_VEICULO,
            TX_ID_VEICULO,
            TX_ID_GUIA_REMESSA,
            NR_DISTANCIA_DA_VIA_M,
            TX_OFERECE_RISCO_SISTEMA_CPTM,
            TX_OBS_CADASTRAMENTO,
            DT_DATA_DO_CADASTRAMENTO,
            HR_HORA_DO_CADASTRAMENTO,
            TX_AUTOR_PF_DO_CADASTRO,
            TX_STATUS_DO_REGISTRO_NO_BD,
            TX_NOME_FOTO_01,
            TX_NOME_FOTO_02,
            TX_NOME_FOTO_03,
            TX_NOME_FOTO_04
        ) VALUES (
            :pk, :natureza, :tipoDoc, TO_DATE(:dataForm, 'YYYY-MM-DD'), :numero,
            :autor, :sigla, :statusDesvio, :contratada, :numContrato, :localEscopo, :empresaExecutora,
            :supervisor, :responsavel, :rp, :drt, :nomeAreaGestora, :idAreaGestora, :siglaAreaGestora,
            :elemNum, :elemNome, :municipio, :linha, :estacao,
            :via, :trecho, :km,
            :lat, :lng,
            SDO_GEOMETRY(2001, 4326, SDO_POINT_TYPE(:lng2, :lat2, NULL), NULL, NULL),
            :tipoAtiv, :ativNaoList, :tipoDra, :codDra,
            TO_DATE(:dataValDra, 'YYYY-MM-DD'),
            :tipoAtivCptm, :nomeEdif, :edifCompl, :origemEfl,
            :fonteGer, :destinacao, :qtdLitros,
            :veicTipo, :veicPlaca, :guia, :distancia,
            :commodities, :obs,
            TO_DATE(:dataCad, 'YYYY-MM-DD'), :horaCad,
            :autorCad, 'Ativo',
            :foto01, :foto02, :foto03, :foto04
        )";

        cmd.Parameters.Add(new OracleParameter("pk", pk));
        cmd.Parameters.Add(new OracleParameter("natureza", (object?)req.Natureza ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("tipoDoc", (object?)req.TipoDocumento ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("dataForm", (object?)req.Data ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("numero", ParseNullableNumber(req.Numero)));
        cmd.Parameters.Add(new OracleParameter("autor", (object?)req.Autor ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("sigla", (object?)req.SiglaMeioAmbiente ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("statusDesvio", (object?)(req.StatusDesvioAmbiental ?? "Não Regularizado") ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("contratada", (object?)req.Contratada ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("numContrato", (object?)req.NumContrato ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("localEscopo", (object?)(req.LocalEscopoContratual ?? req.Empresa) ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("empresaExecutora", (object?)(req.EmpresaExecutora ?? req.Contratada) ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("supervisor", (object?)req.Supervisor ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("responsavel", (object?)(req.Rt ?? req.Autor) ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("rp", (object?)req.RegistroProfissional ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("drt", (object?)(req.DocumentoRt ?? req.RegistroProfissional) ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("nomeAreaGestora", (object?)req.NomeAreaGestoraCptm ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("idAreaGestora", (object?)req.IdAreaGestoraCptm ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("siglaAreaGestora", (object?)req.SiglaAreaGestoraCptm ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("elemNum", (object?)req.ElementoNumero ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("elemNome", (object?)req.ElementoNome ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("municipio", (object?)req.Municipio ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("linha", (object?)req.LinhaCptm ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("estacao", (object?)req.Estacao ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("via", (object?)req.Via ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("trecho", (object?)req.TrechoSentido ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("km", (object?)req.KmPoste ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("lat", req.Latitude.HasValue ? (object)req.Latitude.Value : DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("lng", req.Longitude.HasValue ? (object)req.Longitude.Value : DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("lng2", req.Longitude.HasValue ? (object)req.Longitude.Value : DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("lat2", req.Latitude.HasValue ? (object)req.Latitude.Value : DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("tipoAtiv", (object?)req.TipoAtividade ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("ativNaoList", (object?)req.AtividadeNaoListada ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("tipoDra", (object?)req.TipoDra ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("codDra", (object?)req.CodigoDra ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("dataValDra", (object?)req.DataValidadeDra ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("tipoAtivCptm", (object?)req.TipoAtividadeCptm ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("nomeEdif", (object?)req.NomeEdificacao ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("edifCompl", (object?)req.EdificacaoComplemento ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("origemEfl", (object?)req.OrigemEfluente ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("fonteGer", (object?)(req.FonteGeradora ?? req.QtdComplemento) ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("destinacao", (object?)req.Destinacao ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("qtdLitros", req.QuantidadeLitros.HasValue ? (object)req.QuantidadeLitros.Value : DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("veicTipo", (object?)req.VeiculoTipo ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("veicPlaca", (object?)req.VeiculoPlaca ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("guia", (object?)(req.CodigoGuiaRemessa ?? req.IdGuiaRemessa ?? req.GuiaRemocao) ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("distancia", req.DistanciaVia.HasValue ? (object)req.DistanciaVia.Value : DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("commodities", (object?)(req.OfereceRiscoSistemaCptm ?? req.Commodities) ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("obs", (object?)(req.ObservacoesGerais ?? req.Observacoes) ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("dataCad", req.DataColeta ?? DateTime.UtcNow.ToString("yyyy-MM-dd")));
        cmd.Parameters.Add(new OracleParameter("horaCad", (object?)req.HoraColeta ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("autorCad", (object?)userEmail ?? DBNull.Value));

        // Nomes das fotos nas colunas TX_NOME_FOTO_01..04
        cmd.Parameters.Add(new OracleParameter("foto01", fotos.Count > 0 ? (object)fotos[0].FileName : DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("foto02", fotos.Count > 1 ? (object)fotos[1].FileName : DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("foto03", fotos.Count > 2 ? (object)fotos[2].FileName : DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("foto04", fotos.Count > 3 ? (object)fotos[3].FileName : DBNull.Value));

        await cmd.ExecuteNonQueryAsync();

        // ── INSERT fotos como BLOB na RT_EFLUENTE ──
        for (int i = 0; i < fotos.Count; i++)
        {
            var foto = fotos[i];
            using var ms = new MemoryStream();
            await foto.CopyToAsync(ms);
            var bytes = ms.ToArray();

            using var cmdFoto = conn.CreateCommand();
            cmdFoto.Transaction = tx;
            cmdFoto.CommandText = @"
            INSERT INTO RT_EFLUENTE (REL_OBJECTID, CONTENT_TYPE, ATT_NAME, DATA_SIZE, DATA)
            VALUES (:relId, :ct, :attName, :dataSize, :blobData)";
            cmdFoto.Parameters.Add(new OracleParameter("relId", pk));
            cmdFoto.Parameters.Add(new OracleParameter("ct", foto.ContentType));
            cmdFoto.Parameters.Add(new OracleParameter("attName", foto.FileName));
            cmdFoto.Parameters.Add(new OracleParameter("dataSize", bytes.Length));
            cmdFoto.Parameters.Add(new OracleParameter("blobData", OracleDbType.Blob) { Value = bytes });
            await cmdFoto.ExecuteNonQueryAsync();
        }

        // ── INSERT na FORMULARIOS_AUDITORIA ──
        using var cmdAuditoria = conn.CreateCommand();
        cmdAuditoria.Transaction = tx;
        cmdAuditoria.CommandText = @"
            INSERT INTO FORMULARIOS_AUDITORIA (
                FORMULARIO_PK, ACAO, USUARIO_ID, USUARIO_NOME, USUARIO_EMAIL, 
                IP_ORIGEM, USER_AGENT, PAYLOAD_DEPOIS
            ) VALUES (
                :pk, 'CRIACAO', :userId, :userNome, :userEmail, 
                :ip, :userAgent, :payload
            )";
        
        cmdAuditoria.Parameters.Add(new OracleParameter("pk", pk));
        cmdAuditoria.Parameters.Add(new OracleParameter("userId", userIdClaim));
        cmdAuditoria.Parameters.Add(new OracleParameter("userNome", ctx.User.FindFirst(ClaimTypes.Name)?.Value ?? ""));
        cmdAuditoria.Parameters.Add(new OracleParameter("userEmail", userEmail));
        cmdAuditoria.Parameters.Add(new OracleParameter("ip", ctx.Connection.RemoteIpAddress?.ToString() ?? ""));
        cmdAuditoria.Parameters.Add(new OracleParameter("userAgent", ctx.Request.Headers["User-Agent"].ToString()));
        
        var payloadClob = new OracleParameter("payload", OracleDbType.Clob);
        payloadClob.Value = dadosJson;
        cmdAuditoria.Parameters.Add(payloadClob);

        await cmdAuditoria.ExecuteNonQueryAsync();

        await tx.CommitAsync();
    }
    catch (OracleException ex)
    {
        await tx.RollbackAsync();
        return Results.BadRequest(new { mensagem = TraduzirErroOracle(ex) });
    }
    catch (Exception)
    {
        await tx.RollbackAsync();
        return Results.Problem("Falha inesperada ao salvar o formulário no banco de dados.");
    }

    return Results.Created($"/api/formularios/{pk}", new { pk, mensagem = "Formulário salvo com sucesso." });
})
   .WithName("CriarFormulario")
   .WithTags("Formulários")
   .WithMetadata(new SwaggerOperationAttribute
   {
       Summary = "Cria um novo formulário com fotos.",
       Description = "Recebe multipart/form-data com campo 'dados' (JSON do formulário) e arquivos 'fotos'."
   });

// ====== FORMULÁRIOS: Listar (Autenticado) — lê da PT_EFLUENTE ======
app.MapGet("/api/formularios", [Authorize] async (HttpContext ctx, string? tipo, string? dataInicio, string? dataFim) =>
{
    var perfil = ctx.User.FindFirst(ClaimTypes.Role)?.Value ?? "operador";
    var userEmail = ctx.User.FindFirst(ClaimTypes.Email)?.Value ?? "";

    using var conn = new OracleConnection(connectionString);
    await conn.OpenAsync();

    var sql = new StringBuilder(@"
        SELECT p.PK_CD_MEIO_AMBIENTE_CPTM,
               p.TX_NATUREZA_DO_PGA,
               p.TX_MUNICIPIO,
               p.TX_LINHA_CPTM,
               p.TX_ESTACAO_CPTM,
               p.TX_AUTOR_PF_DO_CADASTRO,
               p.NR_LAT_GRAU_DECIMAL_WGS84,
               p.NR_LONG_GRAU_DECIMAL_WGS84,
               p.TX_STATUS_DO_REGISTRO_NO_BD,
               p.DT_DATA_DO_CADASTRAMENTO,
               p.HR_HORA_DO_CADASTRAMENTO,
               p.TX_NOME_PJ_DA_CONTRATADA,
               p.TX_ORIGEM_EFLUENTE,
               p.NR_QUANTIDADE_L,
               (SELECT COUNT(*) FROM RT_EFLUENTE r WHERE r.REL_OBJECTID = p.PK_CD_MEIO_AMBIENTE_CPTM) AS QTD_FOTOS
        FROM PT_EFLUENTE p
        WHERE (p.TX_STATUS_DO_REGISTRO_NO_BD IS NULL OR p.TX_STATUS_DO_REGISTRO_NO_BD != 'excluido')");

    var parametros = new List<OracleParameter>();

    // Operador só vê os próprios registros
    if (perfil != "supervisor")
    {
        sql.Append(" AND LOWER(p.TX_AUTOR_PF_DO_CADASTRO) = :email");
        parametros.Add(new OracleParameter("email", userEmail.ToLowerInvariant()));
    }

    if (!string.IsNullOrWhiteSpace(tipo))
    {
        sql.Append(" AND LOWER(p.TX_NATUREZA_DO_PGA) = :tipo");
        parametros.Add(new OracleParameter("tipo", tipo.Trim().ToLowerInvariant()));
    }

    sql.Append(" ORDER BY p.DT_DATA_DO_CADASTRAMENTO DESC NULLS LAST");

    using var cmd = conn.CreateCommand();
    cmd.CommandText = sql.ToString();
    foreach (var p in parametros) cmd.Parameters.Add(p);

    var resultados = new List<FormularioResponse>();
    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        resultados.Add(new FormularioResponse
        {
            Pk = reader["PK_CD_MEIO_AMBIENTE_CPTM"]?.ToString(),
            Natureza = reader["TX_NATUREZA_DO_PGA"]?.ToString(),
            Municipio = reader["TX_MUNICIPIO"]?.ToString(),
            LinhaCptm = reader["TX_LINHA_CPTM"]?.ToString(),
            Estacao = reader["TX_ESTACAO_CPTM"]?.ToString(),
            Autor = reader["TX_AUTOR_PF_DO_CADASTRO"]?.ToString(),
            Latitude = reader["NR_LAT_GRAU_DECIMAL_WGS84"] == DBNull.Value ? null : Convert.ToDouble(reader["NR_LAT_GRAU_DECIMAL_WGS84"]),
            Longitude = reader["NR_LONG_GRAU_DECIMAL_WGS84"] == DBNull.Value ? null : Convert.ToDouble(reader["NR_LONG_GRAU_DECIMAL_WGS84"]),
            Status = reader["TX_STATUS_DO_REGISTRO_NO_BD"]?.ToString() ?? "ativo",
            DataCadastro = reader["DT_DATA_DO_CADASTRAMENTO"] == DBNull.Value ? null : 
                (reader["HR_HORA_DO_CADASTRAMENTO"] != DBNull.Value && !string.IsNullOrWhiteSpace(reader["HR_HORA_DO_CADASTRAMENTO"].ToString())
                    ? DateTime.Parse(Convert.ToDateTime(reader["DT_DATA_DO_CADASTRAMENTO"]).ToString("yyyy-MM-dd") + "T" + reader["HR_HORA_DO_CADASTRAMENTO"].ToString() + ":00")
                    : Convert.ToDateTime(reader["DT_DATA_DO_CADASTRAMENTO"])),
            QtdFotos = Convert.ToInt32(reader["QTD_FOTOS"]),
            Contratada = reader["TX_NOME_PJ_DA_CONTRATADA"]?.ToString(),
            OrigemEfluente = reader["TX_ORIGEM_EFLUENTE"]?.ToString(),
            QuantidadeLitros = reader["NR_QUANTIDADE_L"] == DBNull.Value ? null : Convert.ToDouble(reader["NR_QUANTIDADE_L"])
        });
    }

    return Results.Ok(resultados);
})
   .WithName("ListarFormularios")
   .WithTags("Formulários")
   .WithMetadata(new SwaggerOperationAttribute
   {
       Summary = "Lista formulários cadastrados.",
       Description = "Retorna formulários acessíveis pelo usuário autenticado, com filtros opcionais de tipo e período."
   });

// ====== FORMULÁRIOS: Obter Fotos por PK ======
app.MapGet("/api/formularios/{pk}/fotos", [Authorize] async (string pk) =>
{
    if (string.IsNullOrWhiteSpace(pk))
        return Results.BadRequest(new { mensagem = "PK inválida." });

    using var conn = new OracleConnection(connectionString);
    await conn.OpenAsync();

    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT ATT_NAME, CONTENT_TYPE, DATA FROM RT_EFLUENTE WHERE REL_OBJECTID = :pk";
    cmd.Parameters.Add(new OracleParameter("pk", pk));

    var fotos = new List<object>();
    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var bytes = (byte[])reader["DATA"];
        var base64 = Convert.ToBase64String(bytes);
        var contentType = reader["CONTENT_TYPE"]?.ToString() ?? "image/jpeg";
        
        fotos.Add(new
        {
            nome = reader["ATT_NAME"]?.ToString(),
            tipo = contentType,
            base64 = $"data:{contentType};base64,{base64}"
        });
    }

    return Results.Ok(fotos);
})
   .WithName("ObterFotosFormulario")
   .WithTags("Formulários")
   .WithMetadata(new SwaggerOperationAttribute
   {
       Summary = "Obtém as fotos de um formulário.",
       Description = "Retorna a lista de imagens codificadas em base64 para o formulário informado."
   });

// ====== FORMULÁRIOS: Obter por PK (Edição Supervisor) ======
app.MapGet("/api/formularios/{pk}", [Authorize(Roles = "supervisor")] async (string pk) =>
{
    if (string.IsNullOrWhiteSpace(pk))
        return Results.BadRequest(new { mensagem = "PK inválida." });

    using var conn = new OracleConnection(connectionString);
    await conn.OpenAsync();

    using var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        SELECT 
            PK_CD_MEIO_AMBIENTE_CPTM, TX_NATUREZA_DO_PGA, TX_TIPO_DE_FORMULARIO, DT_DATA_EMISSAO_FORMULARIO,
            NR_NUMERO_DE_FORMULARIO, TX_AUTOR_PF_DO_FORMULARIO, TX_SIGLA_DEPTO_MEIO_AMBIENTE, TX_STATUS_DO_DESVIO_AMBIENTAL,
            TX_NOME_PJ_DA_CONTRATADA, TX_NR_CONTRATO_CONTRATADA, TX_NM_LOCAL_ESCOPO_CONTRATUAL, TX_NOME_PJ_EXECUTORA,
            TX_NOME_PJ_DA_SUPERVISORA, TX_NM_RESPONSAVEL_CADASTRO, TX_RP_RESPONSAVEL_CADASTRO, TX_DRT_RESPONSAVEL_CADASTRO,
            TX_NM_AREA_GESTORA_CPTM, TX_ID_AREA_GESTORA_CPTM, TX_SIGLA_AREA_GESTORA_CPTM, TX_NR_ELEMENTO_MONITORAMENTO,
            TX_NM_ELEMENTO_MONITORAMENTO, TX_MUNICIPIO, TX_LINHA_CPTM, TX_ESTACAO_CPTM, TX_VIA_CPTM, TX_TRECHO_E_SENTIDO_CPTM,
            TX_KM_POSTE, NR_LAT_GRAU_DECIMAL_WGS84, NR_LONG_GRAU_DECIMAL_WGS84, TX_TIPO_ATIVIDADE_LISTADA,
            TX_TIPO_ATIVIDADE_N_LISTADA, TX_TIPO_DRA_LISTADO, TX_ID_DRA, DT_VALIDADE_DRA, TX_TIPO_ATIVIDADE_CPTM,
            TX_NM_LOCAL_ATIV, TX_NM_LOCAL_ATIV_COMPLEMENTO, TX_ORIGEM_EFLUENTE, TX_FONTE_GERADORA, TX_TIPO_DESTINACAO,
            NR_QUANTIDADE_L, TX_TIPO_VEICULO, TX_ID_VEICULO, TX_ID_GUIA_REMESSA, NR_DISTANCIA_DA_VIA_M,
            TX_OFERECE_RISCO_SISTEMA_CPTM, TX_OBS_CADASTRAMENTO, DT_DATA_DO_CADASTRAMENTO, HR_HORA_DO_CADASTRAMENTO,
            TX_AUTOR_PF_DO_CADASTRO
        FROM PT_EFLUENTE 
        WHERE PK_CD_MEIO_AMBIENTE_CPTM = :pk AND (TX_STATUS_DO_REGISTRO_NO_BD IS NULL OR TX_STATUS_DO_REGISTRO_NO_BD != 'excluido')";
    
    cmd.Parameters.Add(new OracleParameter("pk", pk));

    using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
        return Results.NotFound(new { mensagem = "Formulário não encontrado." });

    var form = new FormularioRequest
    {
        ChavePrimaria = reader["PK_CD_MEIO_AMBIENTE_CPTM"]?.ToString(),
        Natureza = reader["TX_NATUREZA_DO_PGA"]?.ToString(),
        TipoDocumento = reader["TX_TIPO_DE_FORMULARIO"]?.ToString(),
        Data = reader["DT_DATA_EMISSAO_FORMULARIO"] == DBNull.Value ? null : Convert.ToDateTime(reader["DT_DATA_EMISSAO_FORMULARIO"]).ToString("yyyy-MM-dd"),
        Numero = reader["NR_NUMERO_DE_FORMULARIO"]?.ToString(),
        Autor = reader["TX_AUTOR_PF_DO_FORMULARIO"]?.ToString(),
        SiglaMeioAmbiente = reader["TX_SIGLA_DEPTO_MEIO_AMBIENTE"]?.ToString(),
        StatusDesvioAmbiental = reader["TX_STATUS_DO_DESVIO_AMBIENTAL"]?.ToString(),
        Contratada = reader["TX_NOME_PJ_DA_CONTRATADA"]?.ToString(),
        NumContrato = reader["TX_NR_CONTRATO_CONTRATADA"]?.ToString(),
        Empresa = reader["TX_NM_LOCAL_ESCOPO_CONTRATUAL"]?.ToString(),
        EmpresaExecutora = reader["TX_NOME_PJ_EXECUTORA"]?.ToString(),
        Supervisor = reader["TX_NOME_PJ_DA_SUPERVISORA"]?.ToString(),
        Rt = reader["TX_NM_RESPONSAVEL_CADASTRO"]?.ToString(),
        RegistroProfissional = reader["TX_RP_RESPONSAVEL_CADASTRO"]?.ToString(),
        DocumentoRt = reader["TX_DRT_RESPONSAVEL_CADASTRO"]?.ToString(),
        NomeAreaGestoraCptm = reader["TX_NM_AREA_GESTORA_CPTM"]?.ToString(),
        IdAreaGestoraCptm = reader["TX_ID_AREA_GESTORA_CPTM"]?.ToString(),
        SiglaAreaGestoraCptm = reader["TX_SIGLA_AREA_GESTORA_CPTM"]?.ToString(),
        ElementoNumero = reader["TX_NR_ELEMENTO_MONITORAMENTO"]?.ToString(),
        ElementoNome = reader["TX_NM_ELEMENTO_MONITORAMENTO"]?.ToString(),
        Municipio = reader["TX_MUNICIPIO"]?.ToString(),
        LinhaCptm = reader["TX_LINHA_CPTM"]?.ToString(),
        Estacao = reader["TX_ESTACAO_CPTM"]?.ToString(),
        Via = reader["TX_VIA_CPTM"]?.ToString(),
        TrechoSentido = reader["TX_TRECHO_E_SENTIDO_CPTM"]?.ToString(),
        KmPoste = reader["TX_KM_POSTE"]?.ToString(),
        Latitude = reader["NR_LAT_GRAU_DECIMAL_WGS84"] == DBNull.Value ? null : Convert.ToDouble(reader["NR_LAT_GRAU_DECIMAL_WGS84"]),
        Longitude = reader["NR_LONG_GRAU_DECIMAL_WGS84"] == DBNull.Value ? null : Convert.ToDouble(reader["NR_LONG_GRAU_DECIMAL_WGS84"]),
        TipoAtividade = reader["TX_TIPO_ATIVIDADE_LISTADA"]?.ToString(),
        AtividadeNaoListada = reader["TX_TIPO_ATIVIDADE_N_LISTADA"]?.ToString(),
        TipoDra = reader["TX_TIPO_DRA_LISTADO"]?.ToString(),
        CodigoDra = reader["TX_ID_DRA"]?.ToString(),
        DataValidadeDra = reader["DT_VALIDADE_DRA"] == DBNull.Value ? null : Convert.ToDateTime(reader["DT_VALIDADE_DRA"]).ToString("yyyy-MM-dd"),
        TipoAtividadeCptm = reader["TX_TIPO_ATIVIDADE_CPTM"]?.ToString(),
        NomeEdificacao = reader["TX_NM_LOCAL_ATIV"]?.ToString(),
        EdificacaoComplemento = reader["TX_NM_LOCAL_ATIV_COMPLEMENTO"]?.ToString(),
        OrigemEfluente = reader["TX_ORIGEM_EFLUENTE"]?.ToString(),
        FonteGeradora = reader["TX_FONTE_GERADORA"]?.ToString(),
        Destinacao = reader["TX_TIPO_DESTINACAO"]?.ToString(),
        QuantidadeLitros = reader["NR_QUANTIDADE_L"] == DBNull.Value ? null : Convert.ToDouble(reader["NR_QUANTIDADE_L"]),
        VeiculoTipo = reader["TX_TIPO_VEICULO"]?.ToString(),
        VeiculoPlaca = reader["TX_ID_VEICULO"]?.ToString(),
        CodigoGuiaRemessa = reader["TX_ID_GUIA_REMESSA"]?.ToString(),
        DistanciaVia = reader["NR_DISTANCIA_DA_VIA_M"] == DBNull.Value ? null : Convert.ToDouble(reader["NR_DISTANCIA_DA_VIA_M"]),
        OfereceRiscoSistemaCptm = reader["TX_OFERECE_RISCO_SISTEMA_CPTM"]?.ToString(),
        ObservacoesGerais = reader["TX_OBS_CADASTRAMENTO"]?.ToString(),
        DataColeta = reader["DT_DATA_DO_CADASTRAMENTO"] == DBNull.Value ? null : Convert.ToDateTime(reader["DT_DATA_DO_CADASTRAMENTO"]).ToString("yyyy-MM-dd"),
        HoraColeta = reader["HR_HORA_DO_CADASTRAMENTO"]?.ToString()
    };

    return Results.Ok(form);
})
   .WithName("ObterFormularioPorPk")
   .WithTags("Formulários")
   .WithMetadata(new SwaggerOperationAttribute
   {
       Summary = "Obtém um formulário por sua chave primária.",
       Description = "Retorna os dados completos do formulário para edição pelo supervisor."
   });

// ====== FORMULÁRIOS: Atualizar (APENAS Supervisor) ======
app.MapPut("/api/formularios/{pk}", [Authorize(Roles = "supervisor")] async (string pk, HttpContext ctx) =>
{
    if (string.IsNullOrWhiteSpace(pk))
        return Results.BadRequest(new { mensagem = "PK inválida." });

    var form = await ctx.Request.ReadFormAsync();
    var dadosJson = form["dados"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(dadosJson))
        return Results.BadRequest(new { mensagem = "Dados do formulário são obrigatórios." });

    FormularioRequest? req;
    try
    {
        req = JsonSerializer.Deserialize<FormularioRequest>(dadosJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERRO JSON] {ex.Message}");
        return Results.BadRequest(new { mensagem = "JSON de dados inválido." });
    }

    if (req == null)
        return Results.BadRequest(new { mensagem = "Dados do formulário inválidos." });

    static object ParseNullableNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return DBNull.Value;
        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : DBNull.Value;
    }

    using var conn = new OracleConnection(connectionString);
    await conn.OpenAsync();
    await using var tx = conn.BeginTransaction();

    try
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            UPDATE PT_EFLUENTE SET
                TX_NATUREZA_DO_PGA = :natureza,
                TX_TIPO_DE_FORMULARIO = :tipoDoc,
                DT_DATA_EMISSAO_FORMULARIO = TO_DATE(:dataForm, 'YYYY-MM-DD'),
                NR_NUMERO_DE_FORMULARIO = :numero,
                TX_AUTOR_PF_DO_FORMULARIO = :autor,
                TX_SIGLA_DEPTO_MEIO_AMBIENTE = :sigla,
                TX_STATUS_DO_DESVIO_AMBIENTAL = :statusDesvio,
                TX_NOME_PJ_DA_CONTRATADA = :contratada,
                TX_NR_CONTRATO_CONTRATADA = :numContrato,
                TX_NM_LOCAL_ESCOPO_CONTRATUAL = :localEscopo,
                TX_NOME_PJ_EXECUTORA = :empresaExecutora,
                TX_NOME_PJ_DA_SUPERVISORA = :supervisor,
                TX_NM_RESPONSAVEL_CADASTRO = :responsavel,
                TX_RP_RESPONSAVEL_CADASTRO = :rp,
                TX_DRT_RESPONSAVEL_CADASTRO = :drt,
                TX_NM_AREA_GESTORA_CPTM = :nomeAreaGestora,
                TX_ID_AREA_GESTORA_CPTM = :idAreaGestora,
                TX_SIGLA_AREA_GESTORA_CPTM = :siglaAreaGestora,
                TX_NR_ELEMENTO_MONITORAMENTO = :elemNum,
                TX_NM_ELEMENTO_MONITORAMENTO = :elemNome,
                TX_MUNICIPIO = :municipio,
                TX_LINHA_CPTM = :linha,
                TX_ESTACAO_CPTM = :estacao,
                TX_VIA_CPTM = :via,
                TX_TRECHO_E_SENTIDO_CPTM = :trecho,
                TX_KM_POSTE = :km,
                NR_LAT_GRAU_DECIMAL_WGS84 = :lat,
                NR_LONG_GRAU_DECIMAL_WGS84 = :lng,
                GEOM = SDO_GEOMETRY(2001, 4326, SDO_POINT_TYPE(:lng2, :lat2, NULL), NULL, NULL),
                TX_TIPO_ATIVIDADE_LISTADA = :tipoAtiv,
                TX_TIPO_ATIVIDADE_N_LISTADA = :ativNaoList,
                TX_TIPO_DRA_LISTADO = :tipoDra,
                TX_ID_DRA = :codDra,
                DT_VALIDADE_DRA = TO_DATE(:dataValDra, 'YYYY-MM-DD'),
                TX_TIPO_ATIVIDADE_CPTM = :tipoAtivCptm,
                TX_NM_LOCAL_ATIV = :nomeEdif,
                TX_NM_LOCAL_ATIV_COMPLEMENTO = :edifCompl,
                TX_ORIGEM_EFLUENTE = :origemEfl,
                TX_FONTE_GERADORA = :fonteGer,
                TX_TIPO_DESTINACAO = :destinacao,
                NR_QUANTIDADE_L = :qtdLitros,
                TX_TIPO_VEICULO = :veicTipo,
                TX_ID_VEICULO = :veicPlaca,
                TX_ID_GUIA_REMESSA = :guia,
                NR_DISTANCIA_DA_VIA_M = :distancia,
                TX_OFERECE_RISCO_SISTEMA_CPTM = :commodities,
                TX_OBS_CADASTRAMENTO = :obs,
                DT_DATA_DO_CADASTRAMENTO = TO_DATE(:dataCad, 'YYYY-MM-DD'),
                HR_HORA_DO_CADASTRAMENTO = :horaCad
            WHERE PK_CD_MEIO_AMBIENTE_CPTM = :pk AND (TX_STATUS_DO_REGISTRO_NO_BD IS NULL OR TX_STATUS_DO_REGISTRO_NO_BD != 'excluido')";

        cmd.Parameters.Add(new OracleParameter("natureza", (object?)req.Natureza ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("tipoDoc", (object?)req.TipoDocumento ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("dataForm", (object?)req.Data ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("numero", ParseNullableNumber(req.Numero)));
        cmd.Parameters.Add(new OracleParameter("autor", (object?)req.Autor ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("sigla", (object?)req.SiglaMeioAmbiente ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("statusDesvio", (object?)(req.StatusDesvioAmbiental ?? "Não Regularizado") ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("contratada", (object?)req.Contratada ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("numContrato", (object?)req.NumContrato ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("localEscopo", (object?)(req.LocalEscopoContratual ?? req.Empresa) ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("empresaExecutora", (object?)(req.EmpresaExecutora ?? req.Contratada) ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("supervisor", (object?)req.Supervisor ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("responsavel", (object?)(req.Rt ?? req.Autor) ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("rp", (object?)req.RegistroProfissional ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("drt", (object?)(req.DocumentoRt ?? req.RegistroProfissional) ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("nomeAreaGestora", (object?)req.NomeAreaGestoraCptm ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("idAreaGestora", (object?)req.IdAreaGestoraCptm ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("siglaAreaGestora", (object?)req.SiglaAreaGestoraCptm ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("elemNum", (object?)req.ElementoNumero ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("elemNome", (object?)req.ElementoNome ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("municipio", (object?)req.Municipio ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("linha", (object?)req.LinhaCptm ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("estacao", (object?)req.Estacao ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("via", (object?)req.Via ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("trecho", (object?)req.TrechoSentido ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("km", (object?)req.KmPoste ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("lat", req.Latitude.HasValue ? (object)req.Latitude.Value : DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("lng", req.Longitude.HasValue ? (object)req.Longitude.Value : DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("lng2", req.Longitude.HasValue ? (object)req.Longitude.Value : DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("lat2", req.Latitude.HasValue ? (object)req.Latitude.Value : DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("tipoAtiv", (object?)req.TipoAtividade ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("ativNaoList", (object?)req.AtividadeNaoListada ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("tipoDra", (object?)req.TipoDra ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("codDra", (object?)req.CodigoDra ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("dataValDra", (object?)req.DataValidadeDra ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("tipoAtivCptm", (object?)req.TipoAtividadeCptm ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("nomeEdif", (object?)req.NomeEdificacao ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("edifCompl", (object?)req.EdificacaoComplemento ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("origemEfl", (object?)req.OrigemEfluente ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("fonteGer", (object?)(req.FonteGeradora ?? req.QtdComplemento) ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("destinacao", (object?)req.Destinacao ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("qtdLitros", req.QuantidadeLitros.HasValue ? (object)req.QuantidadeLitros.Value : DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("veicTipo", (object?)req.VeiculoTipo ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("veicPlaca", (object?)req.VeiculoPlaca ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("guia", (object?)(req.CodigoGuiaRemessa ?? req.IdGuiaRemessa ?? req.GuiaRemocao) ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("distancia", req.DistanciaVia.HasValue ? (object)req.DistanciaVia.Value : DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("commodities", (object?)(req.OfereceRiscoSistemaCptm ?? req.Commodities) ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("obs", (object?)(req.ObservacoesGerais ?? req.Observacoes) ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("dataCad", req.DataColeta ?? DateTime.UtcNow.ToString("yyyy-MM-dd")));
        cmd.Parameters.Add(new OracleParameter("horaCad", (object?)req.HoraColeta ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("pk", pk));

        var rows = await cmd.ExecuteNonQueryAsync();
        if (rows == 0)
        {
            await tx.RollbackAsync();
            return Results.NotFound(new { mensagem = "Formulário não encontrado ou já excluído." });
        }

        // Atualizar fotos se enviadas
        var fotos = form.Files.Where(f => f.Name == "fotos").ToList();
        if (fotos.Any())
        {
            // Remove fotos antigas
            using var cmdDelFotos = conn.CreateCommand();
            cmdDelFotos.Transaction = tx;
            cmdDelFotos.CommandText = "DELETE FROM RT_EFLUENTE WHERE REL_OBJECTID = :pk";
            cmdDelFotos.Parameters.Add(new OracleParameter("pk", pk));
            await cmdDelFotos.ExecuteNonQueryAsync();

            // Insere novas
            for (int i = 0; i < fotos.Count; i++)
            {
                var foto = fotos[i];
                using var ms = new MemoryStream();
                await foto.CopyToAsync(ms);
                var bytes = ms.ToArray();

                using var cmdFoto = conn.CreateCommand();
                cmdFoto.Transaction = tx;
                cmdFoto.CommandText = @"
                INSERT INTO RT_EFLUENTE (REL_OBJECTID, CONTENT_TYPE, ATT_NAME, DATA_SIZE, DATA)
                VALUES (:relId, :ct, :attName, :dataSize, :blobData)";
                cmdFoto.Parameters.Add(new OracleParameter("relId", pk));
                cmdFoto.Parameters.Add(new OracleParameter("ct", foto.ContentType));
                cmdFoto.Parameters.Add(new OracleParameter("attName", foto.FileName));
                cmdFoto.Parameters.Add(new OracleParameter("dataSize", bytes.Length));
                cmdFoto.Parameters.Add(new OracleParameter("blobData", OracleDbType.Blob) { Value = bytes });
                await cmdFoto.ExecuteNonQueryAsync();
            }
        }

        // ── INSERT na FORMULARIOS_AUDITORIA ──
        using var cmdAuditoria = conn.CreateCommand();
        cmdAuditoria.Transaction = tx;
        cmdAuditoria.CommandText = @"
            INSERT INTO FORMULARIOS_AUDITORIA (
                FORMULARIO_PK, ACAO, USUARIO_ID, USUARIO_NOME, USUARIO_EMAIL, 
                IP_ORIGEM, USER_AGENT, PAYLOAD_DEPOIS
            ) VALUES (
                :pk, 'EDICAO', :userId, :userNome, :userEmail, 
                :ip, :userAgent, :payload
            )";
        
        cmdAuditoria.Parameters.Add(new OracleParameter("pk", pk));
        cmdAuditoria.Parameters.Add(new OracleParameter("userId", ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""));
        cmdAuditoria.Parameters.Add(new OracleParameter("userNome", ctx.User.FindFirst(ClaimTypes.Name)?.Value ?? ""));
        cmdAuditoria.Parameters.Add(new OracleParameter("userEmail", ctx.User.FindFirst(ClaimTypes.Email)?.Value ?? ""));
        cmdAuditoria.Parameters.Add(new OracleParameter("ip", ctx.Connection.RemoteIpAddress?.ToString() ?? ""));
        cmdAuditoria.Parameters.Add(new OracleParameter("userAgent", ctx.Request.Headers["User-Agent"].ToString()));
        
        var payloadClob = new OracleParameter("payload", OracleDbType.Clob);
        payloadClob.Value = dadosJson;
        cmdAuditoria.Parameters.Add(payloadClob);

        await cmdAuditoria.ExecuteNonQueryAsync();

        await tx.CommitAsync();
        return Results.Ok(new { mensagem = "Formulário atualizado com sucesso." });
    }
    catch (OracleException ex)
    {
        await tx.RollbackAsync();
        return Results.BadRequest(new { mensagem = TraduzirErroOracle(ex) });
    }
    catch (Exception)
    {
        await tx.RollbackAsync();
        return Results.Problem("Falha inesperada ao atualizar o formulário.");
    }
})
   .WithName("AtualizarFormulario")
   .WithTags("Formulários")
   .WithMetadata(new SwaggerOperationAttribute
   {
       Summary = "Atualiza um formulário existente.",
       Description = "Recebe multipart/form-data com campo 'dados' JSON para atualizar o formulário informado."
   });

// ====== FORMULÁRIOS: Excluir (APENAS Supervisor) — soft delete na PT_EFLUENTE ======
app.MapDelete("/api/formularios/{pk}", [Authorize(Roles = "supervisor")] async (string pk, HttpContext ctx) =>
{
    if (string.IsNullOrWhiteSpace(pk) || pk.Length > 255)
        return Results.BadRequest(new { mensagem = "PK inválida." });

    using var conn = new OracleConnection(connectionString);
    await conn.OpenAsync();
    await using var tx = conn.BeginTransaction();

    try
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            UPDATE PT_EFLUENTE SET TX_STATUS_DO_REGISTRO_NO_BD = 'excluido'
            WHERE PK_CD_MEIO_AMBIENTE_CPTM = :pk AND (TX_STATUS_DO_REGISTRO_NO_BD IS NULL OR TX_STATUS_DO_REGISTRO_NO_BD != 'excluido')";
        cmd.Parameters.Add(new OracleParameter("pk", pk));

        var rows = await cmd.ExecuteNonQueryAsync();
        
        if (rows > 0)
        {
            // ── INSERT na FORMULARIOS_AUDITORIA ──
            using var cmdAuditoria = conn.CreateCommand();
            cmdAuditoria.Transaction = tx;
            cmdAuditoria.CommandText = @"
                INSERT INTO FORMULARIOS_AUDITORIA (
                    FORMULARIO_PK, ACAO, USUARIO_ID, USUARIO_NOME, USUARIO_EMAIL, 
                    IP_ORIGEM, USER_AGENT
                ) VALUES (
                    :pk, 'EXCLUSAO', :userId, :userNome, :userEmail, 
                    :ip, :userAgent
                )";
            
            cmdAuditoria.Parameters.Add(new OracleParameter("pk", pk));
            cmdAuditoria.Parameters.Add(new OracleParameter("userId", ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""));
            cmdAuditoria.Parameters.Add(new OracleParameter("userNome", ctx.User.FindFirst(ClaimTypes.Name)?.Value ?? ""));
            cmdAuditoria.Parameters.Add(new OracleParameter("userEmail", ctx.User.FindFirst(ClaimTypes.Email)?.Value ?? ""));
            cmdAuditoria.Parameters.Add(new OracleParameter("ip", ctx.Connection.RemoteIpAddress?.ToString() ?? ""));
            cmdAuditoria.Parameters.Add(new OracleParameter("userAgent", ctx.Request.Headers["User-Agent"].ToString()));

            await cmdAuditoria.ExecuteNonQueryAsync();
            await tx.CommitAsync();
            
            return Results.Ok(new { mensagem = "Registro excluído com sucesso." });
        }
        
        await tx.RollbackAsync();
        return Results.NotFound(new { mensagem = "Registro não encontrado ou já excluído." });
    }
    catch (Exception)
    {
        await tx.RollbackAsync();
        return Results.Problem("Falha inesperada ao excluir o formulário.");
    }
})
   .WithName("ExcluirFormulario")
   .WithTags("Formulários")
   .WithMetadata(new SwaggerOperationAttribute
   {
       Summary = "Exclui (soft delete) um formulário.",
       Description = "Marca o formulário como excluído sem removê-lo fisicamente da base."
   });

// ====== REFERÊNCIAS: Listas para cache no front ======
app.MapGet("/api/referencia/linhas", [Authorize] async () =>
{
    // Dados de referência das linhas CPTM
    return Results.Ok(new[]
    {
        new { Id = 10, Nome = "Linha 10 - Turquesa", Cor = "#008DA5" },
        new { Id = 11, Nome = "Linha 11 - Coral", Cor = "#F68B1F" },
        new { Id = 12, Nome = "Linha 12 - Safira", Cor = "#083E8D" },
        new { Id = 13, Nome = "Linha 13 - Jade", Cor = "#00843D" },
        new { Id = 99, Nome = "Sem linha associada", Cor = "#6B7280" }
    });
})
   .WithName("ListarLinhasReferencia")
   .WithTags("Referências")
   .WithMetadata(new SwaggerOperationAttribute
   {
       Summary = "Lista as linhas de referência usadas no frontend.",
       Description = "Retorna uma tabela de linhas CPTM para preenchimento de formulários de referência."
   });

app.MapGet("/api/referencia/estacoes", [Authorize] async () =>
{
    return Results.Ok(new[]
    {
        new { Id = 15, Nome = "Brás", LinhaId = 10 },
        new { Id = 16, Nome = "Juventus-Mooca", LinhaId = 10 },
        new { Id = 17, Nome = "Ipiranga", LinhaId = 10 },
        new { Id = 18, Nome = "Tamanduateí", LinhaId = 10 },
        new { Id = 19, Nome = "São Caetano do Sul-Prefeito Walter Braido", LinhaId = 10 },
        new { Id = 20, Nome = "Utinga", LinhaId = 10 },
        new { Id = 21, Nome = "Prefeito Saladino", LinhaId = 10 },
        new { Id = 22, Nome = "Santo André", LinhaId = 10 },
        new { Id = 23, Nome = "Capuava", LinhaId = 10 },
        new { Id = 24, Nome = "Mauá", LinhaId = 10 },
        new { Id = 25, Nome = "Guapituba", LinhaId = 10 },
        new { Id = 26, Nome = "Ribeirão Pires", LinhaId = 10 },
        new { Id = 27, Nome = "Rio Grande da Serra", LinhaId = 10 },
        new { Id = 28, Nome = "Luz", LinhaId = 11 },
        new { Id = 29, Nome = "Brás", LinhaId = 11 },
        new { Id = 30, Nome = "Tatuapé", LinhaId = 11 },
        new { Id = 31, Nome = "Corinthians-Itaquera", LinhaId = 11 },
        new { Id = 32, Nome = "Dom Bosco", LinhaId = 11 },
        new { Id = 33, Nome = "José Bonifácio", LinhaId = 11 },
        new { Id = 34, Nome = "Guaianases", LinhaId = 11 },
        new { Id = 35, Nome = "Antonio Gianetti Neto", LinhaId = 11 },
        new { Id = 36, Nome = "Ferraz de Vasconcelos", LinhaId = 11 },
        new { Id = 37, Nome = "Poá", LinhaId = 11 },
        new { Id = 38, Nome = "Calmon Viana", LinhaId = 11 },
        new { Id = 39, Nome = "Suzano", LinhaId = 11 },
        new { Id = 40, Nome = "Jundiapeba", LinhaId = 11 },
        new { Id = 41, Nome = "Braz Cubas", LinhaId = 11 },
        new { Id = 42, Nome = "Mogi das Cruzes", LinhaId = 11 },
        new { Id = 43, Nome = "Estudantes", LinhaId = 11 },
        new { Id = 44, Nome = "Brás", LinhaId = 12 },
        new { Id = 45, Nome = "Tatuapé", LinhaId = 12 },
        new { Id = 46, Nome = "Engenheiro Goulart", LinhaId = 12 },
        new { Id = 47, Nome = "USP Leste", LinhaId = 12 },
        new { Id = 48, Nome = "Comendador Ermelino", LinhaId = 12 },
        new { Id = 49, Nome = "São Miguel Paulista", LinhaId = 12 },
        new { Id = 50, Nome = "Jardim Helena-Vila Mara", LinhaId = 12 },
        new { Id = 51, Nome = "Itaim Paulista", LinhaId = 12 },
        new { Id = 52, Nome = "Jardim Romano", LinhaId = 12 },
        new { Id = 53, Nome = "Engenheiro Manoel Feio", LinhaId = 12 },
        new { Id = 54, Nome = "Itaquaquecetuba", LinhaId = 12 },
        new { Id = 55, Nome = "Aracaré", LinhaId = 12 },
        new { Id = 56, Nome = "Calmon Viana", LinhaId = 12 },
        new { Id = 57, Nome = "Engenheiro Goulart", LinhaId = 13 },
        new { Id = 58, Nome = "Guarulhos-Cecap", LinhaId = 13 },
        new { Id = 59, Nome = "Aeroporto-Guarulhos", LinhaId = 13 }
    });
})
   .WithName("ListarEstacoesReferencia")
   .WithTags("Referências")
   .WithMetadata(new SwaggerOperationAttribute
   {
       Summary = "Lista as estações de referência.",
       Description = "Retorna estações CPTM agrupadas por linha para uso em formulários."
   });

app.MapGet("/api/referencia/naturezas", [Authorize] async () =>
{
    return Results.Ok(new[]
    {
        new { Id = 1, Nome = "Efluente", Tipo = "efluente" },
        new { Id = 2, Nome = "Vegetação / Árvore Isolada", Tipo = "arvore" },
        new { Id = 3, Nome = "Fauna", Tipo = "fauna" },
        new { Id = 4, Nome = "Erosão / Movimento de Massa", Tipo = "erosao" },
        new { Id = 5, Nome = "Resíduos Sólidos", Tipo = "residuo" }
    });
})
   .WithName("ListarNaturezasReferencia")
   .WithTags("Referências")
   .WithMetadata(new SwaggerOperationAttribute
   {
       Summary = "Lista naturezas de formulário.",
       Description = "Retorna categorias de natureza para o formulário."
   });

app.MapGet("/api/referencia/formulario-operador", [Authorize] () =>
{
    return Results.Ok(new
    {
        SiglasMeioAmbiente = new[] { "GEA", "GEA.DEAE", "GEA.DEAO", "Não se aplica(m)", "Inexistente(s)", "Indefinido(a)(s)" },
        StatusDesvioAmbiental = new[] { "Não Regularizado", "Regularizado", "Não se aplica(m)", "Inexistente(s)", "Indefinido(a)(s)", "Não avaliado(a)(s)" },
        TiposAtividade = new[] { "Estação de Tratamento de Efluente", "Transporte", "Outro(a)(s)", "Indefinido(a)(s)", "Inexistente(s)", "Não se aplica(m)", "Não avaliado(a)(s)" },
        TiposDra = new[] { "Cadastro Técnico Federal (IBAMA) - CTF/IBAMA", "Certificado de Dispensa de Licença - CDL", "Certificado de Movimentação de Resíduos de Interesse Ambiental - CADRI", "Declaração de Movimentação de Resíduos - DMR", "Ficha de Informações de Segurança de Produtos Químicos - FISPQ", "Licença de Operação - LO", "Manifesto de Transporte de Resíduos - MTR", "Outro(a)(s)", "Indefinido(a)(s)", "Inexistente(s)", "Não se aplica(m)", "Não avaliado(a)(s)" },
        TiposAtividadeCptm = new[] { "Empreendimento/Obra", "Manutenção", "Operação", "Outro(a)(s)", "Indefinido(a)(s)", "Inexistente(s)", "Não se aplica(m)", "Não avaliado(a)(s)" },
        NomesEdificacao = new[] { "Abrigo", "Base de manutenção", "Cabine Primária", "Cabine Seccionadora", "Estação", "Lavador de TUE", "Oficina", "Pátio", "Prédio administrativo", "Prédio de apoio", "Sala técnica", "Subestação", "Outro(a)(s)", "Indefinido(a)(s)", "Inexistente(s)", "Não se aplica(m)", "Não avaliado(a)(s)" },
        OrigensEfluente = new[] { "Doméstico/Sanitário", "Fundação", "Industrial", "Outro(a)(s)", "Indefinido(a)(s)", "Inexistente(s)", "Não se aplica(m)", "Não avaliado(a)(s)" },
        ComplementosLocal = new[] { "Atividade de obra", "Banheiro químico", "Banheiros/vestiários/refeitórios", "Fossa séptica", "Lavagem de trens/peças", "Manutenção ETE", "Valas de manutenção", "Outro(a)(s)", "Indefinido(a)(s)", "Inexistente(s)", "Não se aplica(m)", "Não avaliado(a)(s)" },
        Destinacoes = new[] { "Esgotamento e transporte", "Interligação em rede coletora", "Lançamento em galeria de águas pluviais", "Reinfiltração", "Tratamento em ETE", "Outro(a)(s)", "Indefinido(a)(s)", "Inexistente(s)", "Não se aplica(m)", "Não avaliado(a)(s)" },
        TiposVeiculo = new[] { "Caminhão", "Outro(a)(s)", "Indefinido(a)(s)", "Inexistente(s)", "Não se aplica(m)", "Não avaliado(a)(s)" },
        Municipios = new[] { "Arujá", "Barueri", "Biritiba-Mirim", "Caieiras", "Cajamar", "Campo Limpo Paulista", "Carapicuíba", "Cotia", "Diadema", "Embu", "Embu-Guaçu", "Ferraz de Vasconcelos", "Franco da Rocha", "Francisco Morato", "Guararema", "Guarulhos", "Itapecerica da Serra", "Itapevi", "Itaquaquecetuba", "Jandira", "Mauá", "Osasco", "Ribeirão Pires", "Rio Grande da Serra", "Santo André", "São Paulo" }
    });
})
   .WithName("ListarFormularioOperadorReferencia")
   .WithTags("Referências")
   .WithMetadata(new SwaggerOperationAttribute
   {
       Summary = "Lista opções de referência usadas no formulário do operador.",
       Description = "Fornece valores predefinidos para campos de seleção no formulário de cadastro."
   });

// ====== USUÁRIOS: Cadastrar (Apenas Supervisor) ======
app.MapPost("/api/usuarios", [Authorize(Roles = "supervisor")] async (UsuarioRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Nome) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Senha))
        return Results.BadRequest(new { mensagem = "Nome, e-mail e senha são obrigatórios." });

    var emailLimpo = request.Email.Trim().ToLowerInvariant();
    if (emailLimpo.Length > 200)
        return Results.BadRequest(new { mensagem = "E-mail inválido." });

    var perfil = request.Perfil?.ToLowerInvariant();
    if (perfil != "operador" && perfil != "supervisor")
        perfil = "operador";

    using var conn = new OracleConnection(connectionString);
    await conn.OpenAsync();

    // Verificar se e-mail já existe
    using var cmdCheck = conn.CreateCommand();
    cmdCheck.CommandText = "SELECT COUNT(*) FROM USUARIOS WHERE LOWER(EMAIL) = :email AND ATIVO = 1";
    cmdCheck.Parameters.Add(new OracleParameter("email", emailLimpo));
    var count = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync());
    if (count > 0)
        return Results.BadRequest(new { mensagem = "E-mail já cadastrado." });

    var senhaHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Senha))).ToLowerInvariant();

    using var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        INSERT INTO USUARIOS (NOME, EMAIL, SENHA_HASH, PERFIL, ATIVO, CRIADO_EM)
        VALUES (:nome, :email, :senha, :perfil, 1, LOCALTIMESTAMP)
        RETURNING ID INTO :id";
    cmd.Parameters.Add(new OracleParameter("nome", request.Nome.Trim()));
    cmd.Parameters.Add(new OracleParameter("email", emailLimpo));
    cmd.Parameters.Add(new OracleParameter("senha", senhaHash));
    cmd.Parameters.Add(new OracleParameter("perfil", perfil));
    var outParam = new OracleParameter("id", OracleDbType.Int32)
    {
        Direction = System.Data.ParameterDirection.Output
    };
    cmd.Parameters.Add(outParam);

    await cmd.ExecuteNonQueryAsync();

    int id = 0;
    var rawId = outParam.Value;
    if (rawId is Oracle.ManagedDataAccess.Types.OracleDecimal oracleDecimal)
    {
        id = oracleDecimal.ToInt32();
    }
    else if (rawId != null && rawId != DBNull.Value)
    {
        id = Convert.ToInt32(rawId);
    }

    return Results.Created($"/api/usuarios/{id}", new { id, mensagem = "Usuário cadastrado com sucesso." });
});

// ====== USUÁRIOS: Listar (Apenas Supervisor) ======
app.MapGet("/api/usuarios", [Authorize(Roles = "supervisor")] async () =>
{
    using var conn = new OracleConnection(connectionString);
    await conn.OpenAsync();

    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT ID, NOME, EMAIL, PERFIL, ATIVO, CRIADO_EM FROM USUARIOS WHERE ATIVO = 1 ORDER BY NOME";

    var usuarios = new List<UsuarioResponse>();
    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        usuarios.Add(new UsuarioResponse
        {
            Id = Convert.ToInt32(reader["ID"]),
            Nome = reader["NOME"]?.ToString() ?? "",
            Email = reader["EMAIL"]?.ToString() ?? "",
            Perfil = reader["PERFIL"]?.ToString() ?? "",
            Ativo = reader["ATIVO"] != DBNull.Value && Convert.ToInt32(reader["ATIVO"]) == 1,
            CriadoEm = reader["CRIADO_EM"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["CRIADO_EM"])
        });
    }

    return Results.Ok(usuarios);
});

// ====== USUÁRIOS: Atualizar Perfil (Apenas Supervisor) ======
app.MapPut("/api/usuarios/{id:int}/perfil", [Authorize(Roles = "supervisor")] async (int id, AtualizarPerfilUsuarioRequest request, HttpContext ctx) =>
{
    if (id <= 0)
        return Results.BadRequest(new { mensagem = "ID inválido." });

    var perfil = request.Perfil?.Trim().ToLowerInvariant();
    if (perfil != "operador" && perfil != "supervisor")
        return Results.BadRequest(new { mensagem = "Perfil inválido. Use 'operador' ou 'supervisor'." });

    var userIdClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdClaim, out var usuarioLogadoId))
        return Results.Unauthorized();

    // Evita auto-rebaixamento do único supervisor logado
    if (usuarioLogadoId == id && perfil == "operador")
    {
        using var connCheck = new OracleConnection(connectionString);
        await connCheck.OpenAsync();

        using var cmdSupervisores = connCheck.CreateCommand();
        cmdSupervisores.CommandText = "SELECT COUNT(*) FROM USUARIOS WHERE ATIVO = 1 AND LOWER(PERFIL) = 'supervisor'";
        var totalSupervisores = Convert.ToInt32(await cmdSupervisores.ExecuteScalarAsync());

        if (totalSupervisores <= 1)
            return Results.BadRequest(new { mensagem = "Não é possível remover o último supervisor ativo." });
    }

    using var conn = new OracleConnection(connectionString);
    await conn.OpenAsync();

    using var cmd = conn.CreateCommand();
    cmd.CommandText = "UPDATE USUARIOS SET PERFIL = :perfil WHERE ID = :id AND ATIVO = 1";
    cmd.Parameters.Add(new OracleParameter("perfil", perfil));
    cmd.Parameters.Add(new OracleParameter("id", id));

    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0
        ? Results.Ok(new { mensagem = "Perfil atualizado com sucesso." })
        : Results.NotFound(new { mensagem = "Usuário não encontrado." });
});

// ====== USUÁRIOS: Redefinir Senha (Apenas Supervisor) ======
app.MapPut("/api/usuarios/{id:int}/senha", [Authorize(Roles = "supervisor")] async (int id, RedefinirSenhaUsuarioRequest request) =>
{
    if (id <= 0)
        return Results.BadRequest(new { mensagem = "ID inválido." });

    if (string.IsNullOrWhiteSpace(request.NovaSenha) || request.NovaSenha.Length < 6 || request.NovaSenha.Length > 100)
        return Results.BadRequest(new { mensagem = "A nova senha deve conter entre 6 e 100 caracteres." });

    var senhaHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.NovaSenha))).ToLowerInvariant();

    using var conn = new OracleConnection(connectionString);
    await conn.OpenAsync();

    using var cmd = conn.CreateCommand();
    cmd.CommandText = "UPDATE USUARIOS SET SENHA_HASH = :senha WHERE ID = :id AND ATIVO = 1";
    cmd.Parameters.Add(new OracleParameter("senha", senhaHash));
    cmd.Parameters.Add(new OracleParameter("id", id));

    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0
        ? Results.Ok(new { mensagem = "Senha redefinida com sucesso." })
        : Results.NotFound(new { mensagem = "Usuário não encontrado." });
});

// ====== USUÁRIOS: Excluir (Apenas Supervisor, com confirmação explícita) ======
app.MapDelete("/api/usuarios/{id:int}", [Authorize(Roles = "supervisor")] async (int id, HttpContext ctx) =>
{
    if (id <= 0)
        return Results.BadRequest(new { mensagem = "ID inválido." });

    var userIdClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdClaim, out var usuarioLogadoId))
        return Results.Unauthorized();

    if (usuarioLogadoId == id)
        return Results.BadRequest(new { mensagem = "Não é permitido bloquear o próprio usuário." });

    using var conn = new OracleConnection(connectionString);
    await conn.OpenAsync();

    // Não permitir remover o último supervisor ativo
    using var cmdPerfil = conn.CreateCommand();
    cmdPerfil.CommandText = "SELECT LOWER(PERFIL) FROM USUARIOS WHERE ID = :id AND ATIVO = 1";
    cmdPerfil.Parameters.Add(new OracleParameter("id", id));
    var perfilAlvoObj = await cmdPerfil.ExecuteScalarAsync();

    if (perfilAlvoObj == null || perfilAlvoObj == DBNull.Value)
        return Results.NotFound(new { mensagem = "Usuário não encontrado." });

    var perfilAlvo = perfilAlvoObj.ToString()?.Trim().ToLowerInvariant() ?? "operador";
    if (perfilAlvo == "supervisor")
    {
        using var cmdSupervisores = conn.CreateCommand();
        cmdSupervisores.CommandText = "SELECT COUNT(*) FROM USUARIOS WHERE ATIVO = 1 AND LOWER(PERFIL) = 'supervisor'";
        var totalSupervisores = Convert.ToInt32(await cmdSupervisores.ExecuteScalarAsync());

        if (totalSupervisores <= 1)
            return Results.BadRequest(new { mensagem = "Não é possível bloquear o último supervisor ativo." });
    }

    using var cmdDelete = conn.CreateCommand();
    cmdDelete.CommandText = "UPDATE USUARIOS SET ATIVO = 0 WHERE ID = :id AND ATIVO = 1";
    cmdDelete.Parameters.Add(new OracleParameter("id", id));

    var rows = await cmdDelete.ExecuteNonQueryAsync();
    return rows > 0
        ? Results.Ok(new { mensagem = "Usuário bloqueado com sucesso." })
        : Results.NotFound(new { mensagem = "Usuário não encontrado." });
});

// ====== Endpoint legado da Semana 1 (pode remover depois) ======
app.MapGet("/api/nomes", async () =>
{
    var nomes = new List<object>();
    using var conn = new OracleConnection(connectionString);
    await conn.OpenAsync();
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT ID, NOME FROM TESTE_SEMANA1 ORDER BY ID";
    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        nomes.Add(new
        {
            Id = reader["ID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["ID"]),
            Nome = reader["NOME"]?.ToString()
        });
    }
    return Results.Ok(nomes);
});

// ====== Seed automático: garante supervisor de teste se não existir ======
try
{
    using var seedConn = new OracleConnection(connectionString);
    await seedConn.OpenAsync();

    // 1. Criar tabela de auditoria se não existir
    try
    {
        using var checkTableCmd = seedConn.CreateCommand();
        checkTableCmd.CommandText = "SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME = 'FORMULARIOS_AUDITORIA'";
        var tableExists = Convert.ToInt32(await checkTableCmd.ExecuteScalarAsync()) > 0;

        if (!tableExists)
        {
            using var createTableCmd = seedConn.CreateCommand();
            createTableCmd.CommandText = @"
                CREATE TABLE FORMULARIOS_AUDITORIA (
                    ID NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                    FORMULARIO_PK VARCHAR2(255) NOT NULL,
                    ACAO VARCHAR2(50) NOT NULL,
                    USUARIO_ID VARCHAR2(100),
                    USUARIO_NOME VARCHAR2(200),
                    USUARIO_EMAIL VARCHAR2(200),
                    IP_ORIGEM VARCHAR2(50),
                    USER_AGENT VARCHAR2(500),
                    PAYLOAD_ANTES CLOB,
                    PAYLOAD_DEPOIS CLOB,
                    DATA_HORA TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";
            await createTableCmd.ExecuteNonQueryAsync();
            Console.WriteLine("[CPTM] Tabela FORMULARIOS_AUDITORIA criada com sucesso.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[CPTM] Aviso: não foi possível criar a tabela de auditoria: {ex.Message}");
    }

    // 2. Criar supervisor de teste
    using var seedCmd = seedConn.CreateCommand();
    seedCmd.CommandText = "SELECT COUNT(*) FROM USUARIOS WHERE LOWER(EMAIL) = 'supervisor@cptm.sp.gov.br'";
    var countSupervisor = Convert.ToInt32(await seedCmd.ExecuteScalarAsync());

    if (countSupervisor == 0)
    {
        using var insertCmd = seedConn.CreateCommand();
        insertCmd.CommandText = @"
            INSERT INTO USUARIOS (NOME, EMAIL, SENHA_HASH, PERFIL, ATIVO, CRIADO_EM)
            VALUES ('Supervisor Teste', 'supervisor@cptm.sp.gov.br',
                    'beb9b226a7aa191956e2a654dcd57d25b13658ba2eeaca5e69375a2815c384bb',
                    'supervisor', 1, SYSTIMESTAMP)";
        await insertCmd.ExecuteNonQueryAsync();
        Console.WriteLine("[CPTM] Supervisor de teste criado automaticamente.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[CPTM] Aviso: não foi possível verificar/criar supervisor de teste: {ex.Message}");
}

app.Run();
