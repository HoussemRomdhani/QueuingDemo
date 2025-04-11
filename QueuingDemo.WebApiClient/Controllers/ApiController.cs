using Microsoft.AspNetCore.Mvc;

namespace QueuingDemo.WebApiClient.Controllers;

[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
    public IActionResult Get() => Ok("Hi");
}
