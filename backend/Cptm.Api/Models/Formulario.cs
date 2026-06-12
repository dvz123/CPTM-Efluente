namespace Cptm.Api.Models;

public class Formulario
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty; // "efluente", "arvore", "fauna", etc.
    public int OperadorId { get; set; }
    public string CamposJson { get; set; } = "{}"; // Dados dinâmicos serializado como JSON
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Status { get; set; } = "ativo"; // "ativo", "excluido"
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? ExcluidoEm { get; set; }
    public int? ExcluidoPorId { get; set; }
}
