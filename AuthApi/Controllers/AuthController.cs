using Microsoft.AspNetCore.Mvc;
using AuthApi.Models;
using AuthApi.Dtos;
using AuthApi.Data;
using Microsoft.EntityFrameworkCore;

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
        if(await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower()))
        {
            return BadRequest(new {message = "Este e-mail já está cadastrado"});
        }

        var user = new User
        {
          Email = dto.Email,
          PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
          Status = UserStatus.Pending  
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new {message = "Cadastro realizado, liberação pendente."});


    }
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new {message = "E-mail ou senha inválidos"});
        }

        if (user.Status == UserStatus.Pending)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Pendente Aprovação"});
        }

        if (user.Status == UserStatus.Rejected)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Acesso rejeitado."});
        }

        return Ok(new {message = "Login realizado.", userId = user.Id, email = user.Email});
    }
}