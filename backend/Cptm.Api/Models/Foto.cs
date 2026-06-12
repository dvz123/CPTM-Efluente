namespace Cptm.Api.Models;

public class Foto
{
    public int Id { get; set; }
    public int FormularioId { get; set; }
    public string NomeArquivo { get; set; } = string.Empty;
    public string CaminhoArquivo { get; set; } = string.Empty;
    public long TamanhoBytes { get; set; }
    public string ContentType { get; set; } = "image/jpeg";
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
