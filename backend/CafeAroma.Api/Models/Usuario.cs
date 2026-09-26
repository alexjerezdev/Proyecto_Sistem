namespace CafeAroma.Api.Models;

public class Usuario
{
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty; // "dueno" o "encargada"
    public string ContrasenaHash { get; set; } = string.Empty;
    public bool Estado { get; set; } = true;
}