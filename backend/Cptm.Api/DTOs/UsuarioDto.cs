namespace Cptm.Api.DTOs;

public class UsuarioRequest
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public string Perfil { get; set; } = "operador";
}

public class UsuarioResponse
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class AtualizarPerfilUsuarioRequest
{
    public string Perfil { get; set; } = "operador";
}

public class RedefinirSenhaUsuarioRequest
{
    public string NovaSenha { get; set; } = string.Empty;
}
