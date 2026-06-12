using System.ComponentModel.DataAnnotations;

namespace Cptm.Api.DTOs;

/// <summary>
/// Payload recebido do frontend — campos mapeados 1:1 com PT_EFLUENTE.
/// </summary>
public class FormularioRequest
{
    // ── Identificação ──
    [MaxLength(255)]
    public string? Contratada { get; set; }
    [MaxLength(255)]
    public string? NumContrato { get; set; }
    [MaxLength(255)]
    public string? Empresa { get; set; }
    [MaxLength(255)]
    public string? LocalEscopoContratual { get; set; }
    [MaxLength(255)]
    public string? EmpresaExecutora { get; set; }
    [MaxLength(255)]
    public string? SiglaMeioAmbiente { get; set; }
    [MaxLength(255)]
    public string? StatusDesvioAmbiental { get; set; }
    [MaxLength(255)]
    public string? Supervisor { get; set; }
    [MaxLength(255)]
    public string? NomeAreaGestoraCptm { get; set; }
    [MaxLength(255)]
    public string? IdAreaGestoraCptm { get; set; }
    [MaxLength(255)]
    public string? SiglaAreaGestoraCptm { get; set; }
    [MaxLength(255)]
    public string? Autor { get; set; }
    [MaxLength(255)]
    public string? Rt { get; set; }
    [MaxLength(255)]
    public string? RegistroProfissional { get; set; }
    [MaxLength(255)]
    public string? DocumentoRt { get; set; }
    [MaxLength(255)]
    public string? Natureza { get; set; }
    [MaxLength(255)]
    public string? TipoDocumento { get; set; }
    public string? Data { get; set; }
    [MaxLength(255)]
    public string? Numero { get; set; }

    // ── Local ──
    public string? DataColeta { get; set; }
    [MaxLength(255)]
    public string? HoraColeta { get; set; }
    [MaxLength(255)]
    public string? ChavePrimaria { get; set; }
    [MaxLength(255)]
    public string? ElementoNumero { get; set; }
    [MaxLength(255)]
    public string? ElementoNome { get; set; }
    [MaxLength(255)]
    public string? Municipio { get; set; }
    [MaxLength(255)]
    public string? LocalTipo { get; set; }
    [MaxLength(255)]
    public string? LinhaCptm { get; set; }
    [MaxLength(255)]
    public string? Estacao { get; set; }
    [MaxLength(255)]
    public string? Via { get; set; }
    [MaxLength(255)]
    public string? TrechoSentido { get; set; }
    [MaxLength(20)]
    public string? KmPoste { get; set; }

    [Range(-90, 90)]
    public double? Latitude { get; set; }
    [Range(-180, 180)]
    public double? Longitude { get; set; }

    // ── Caracterização ──
    [MaxLength(255)]
    public string? TipoAtividade { get; set; }
    [MaxLength(255)]
    public string? TipoDra { get; set; }
    [MaxLength(255)]
    public string? AtividadeNaoListada { get; set; }
    [MaxLength(255)]
    public string? CodigoDra { get; set; }
    public string? DataValidadeDra { get; set; }
    [MaxLength(255)]
    public string? TipoAtividadeCptm { get; set; }
    [MaxLength(255)]
    public string? NomeEdificacao { get; set; }
    [MaxLength(255)]
    public string? OrigemEfluente { get; set; }
    [MaxLength(255)]
    public string? EdificacaoComplemento { get; set; }
    [MaxLength(255)]
    public string? Destinacao { get; set; }
    [MaxLength(255)]
    public string? QtdComplemento { get; set; }
    [MaxLength(255)]
    public string? FonteGeradora { get; set; }
    [MaxLength(255)]
    public string? Commodities { get; set; }
    [MaxLength(255)]
    public string? OfereceRiscoSistemaCptm { get; set; }
    public double? QuantidadeLitros { get; set; }
    [MaxLength(255)]
    public string? VeiculoTipo { get; set; }
    [MaxLength(255)]
    public string? VeiculoPlaca { get; set; }
    [MaxLength(255)]
    public string? GuiaRemocao { get; set; }
    [MaxLength(255)]
    public string? CodigoGuiaRemessa { get; set; }
    [MaxLength(255)]
    public string? IdGuiaRemessa { get; set; }
    public double? DistanciaVia { get; set; }
    [MaxLength(2000)]
    public string? Observacoes { get; set; }
    [MaxLength(2000)]
    public string? ObservacoesGerais { get; set; }
}

/// <summary>
/// Resposta enviada ao frontend — dados da PT_EFLUENTE.
/// </summary>
public class FormularioResponse
{
    public string Pk { get; set; } = string.Empty;
    public string? Natureza { get; set; }
    public string? Municipio { get; set; }
    public string? LinhaCptm { get; set; }
    public string? Estacao { get; set; }
    public string? Autor { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Status { get; set; }
    public DateTime? DataCadastro { get; set; }
    public int QtdFotos { get; set; }
    public string? Contratada { get; set; }
    public string? OrigemEfluente { get; set; }
    public double? QuantidadeLitros { get; set; }
}
