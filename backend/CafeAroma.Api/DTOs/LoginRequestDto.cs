namespace CafeAroma.Api.DTOs;

public class LoginRequestDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
}