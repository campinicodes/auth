using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuthApi.Data;
using AuthApi.Models;
using AuthApi.Dtos;

namespace AuthApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower()))
        {
            return BadRequest(new { message = "Este e-mail já está cadastrado." });
        }

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Status = UserStatus.Pending
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Cadastro realizado! Seu acesso está pendente de aprovação por um administrador." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "E-mail ou senha inválidos." });
        }

        if (user.Status == UserStatus.Pending)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Sua conta ainda não foi aprovada pelo administrador." });
        }

        if (user.Status == UserStatus.Rejected)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Sua solicitação de cadastro foi recusada." });
        }

        return Ok(new { message = "Login realizado com sucesso!", userId = user.Id, email = user.Email });
    }
    [HttpGet("pending-users")]
    public async Task<IActionResult> GetPendingUsers()
    {
        var pendingUsers = await _context.Users
        .Where(u => u.Status == UserStatus.Pending)
        .Select(u => new {u.Id, u.Email, u.CreatedAt })
        .ToListAsync();
        
        return Ok(pendingUsers);


    }

    [HttpPost("approve")]
    public async Task<IActionResult> ApproveUser([FromBody] ApproveUserDto dto)
    {
        var user = await _context.Users.FindAsync(dto.UserId);
        if (user == null)
        {
            return NotFound(new {message = "Usuário não encontrado"});

        }

        user.Status = dto.Approve ? UserStatus.Approved : UserStatus.Rejected;
        await _context.SaveChangesAsync();

        var action = dto.Approve ? "aprovado" : "rejeitado";
        return Ok(new {message = $"Usuário {action} com sucesso."});
    }
}