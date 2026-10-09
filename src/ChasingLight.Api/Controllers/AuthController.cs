using ChasingLight.Api.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ChasingLight.Api.Controllers;

[ApiController]
[Route("/api/v1/auth")]
public class AuthController: ControllerBase
{
    [HttpGet("test-error")]
    public IActionResult TestError()
    {
        throw new NotFoundException("User", 999);
    }
}