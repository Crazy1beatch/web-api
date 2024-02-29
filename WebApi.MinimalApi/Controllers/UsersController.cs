using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Produces("application/json", "application/xml")]
[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;

    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        this.mapper = mapper;
        this.userRepository = userRepository;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
            return NotFound();

        return Ok(mapper.Map<UserDto>(user));
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] UserCreationDto user)
    {
        if (user is null)
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        if (!user.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError("login", "Unsupported symbols in login!");
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        var createdUserEntity = userRepository.Insert(mapper.Map<UserEntity>(user));
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }
}
