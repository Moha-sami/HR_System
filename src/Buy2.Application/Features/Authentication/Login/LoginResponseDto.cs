namespace Buy2.Application.Features.Authentication.Login;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
