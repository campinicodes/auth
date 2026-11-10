namespace AuthApi.Dtos;

public record RegisterRequestDto(string Email, string Password);
public record LoginRequestDto(string Email, string Password);
public record ApproveUserDto(Guid UserId, bool Approve);