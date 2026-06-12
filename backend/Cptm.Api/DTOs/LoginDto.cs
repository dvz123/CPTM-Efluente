using System.ComponentModel.DataAnnotations;

namespace Cptm.Api.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "E-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória.")]
    [MinLength(6, ErrorMessage = "Senha deve ter no mínimo 6 caracteres.")]
    [MaxLength(100)]
    public string Senha { get; set; } = string.Empty;
}

public class LoginResponse
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string HashOffline { get; set; } = string.Empty; // Para validação offline no IndexedDB
    public DateTime ExpiraEm { get; set; }
}
