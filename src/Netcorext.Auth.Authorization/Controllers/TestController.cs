using Microsoft.AspNetCore.Mvc;
using Netcorext.Auth.Attributes;

namespace Netcorext.Auth.Authorization.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("[controller]")]
[Permission("TEST")]
public class TestController : ControllerBase
{
    [HttpGet("{id:long?}")]
    public Task<IActionResult> GetAsync([FromRoute] long? id)
    {
        return Task.FromResult<IActionResult>(Ok());
    }
}