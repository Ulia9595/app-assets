using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JwtTestController : ControllerBase
    {
        private readonly JwtService _jwtService;

        public JwtTestController(JwtService jwtService)
        {
            _jwtService = jwtService;
        }

        [HttpGet("generate")]
        public IActionResult GenerateToken()
        {
            string uid = Guid.NewGuid().ToString();
            string token = _jwtService.GenerateToken(23, "ulyanacoroliova@yandex.ru", uid, "admin");

            return Ok(new { token });
        }

        [HttpGet("validate")]
        public IActionResult ValidateToken([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
                return BadRequest("Token is required");

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                var claimsInfo = jwtToken.Claims.Select(c => new { c.Type, c.Value }).ToList();

                var principal = _jwtService.ValidateToken(token);

                return Ok(new
                {
                    ParsedClaims = claimsInfo,
                    IsValid = principal != null,
                    UserId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    Role = principal?.FindFirst(ClaimTypes.Role)?.Value
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}