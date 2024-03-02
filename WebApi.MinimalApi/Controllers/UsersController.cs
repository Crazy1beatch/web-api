using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers
{
    [Produces("application/json", "application/xml")]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        private readonly IUserRepository userRepository;
        private readonly IMapper mapper;
        private readonly LinkGenerator linkGenerator;
        private static readonly string[] value = new[] { "POST", "GET", "OPTIONS" };

        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
        {
            this.mapper = mapper;
            this.userRepository = userRepository;
            this.linkGenerator = linkGenerator;
        }

        [HttpHead("{userId}")]
        [HttpGet("{userId}", Name = nameof(GetUserById))]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = userRepository.FindById(userId);
            if (user is null)
                return NotFound();

            return Ok(mapper.Map<UserDto>(user));
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] UserCreationDto? user)
        {
            if (user is null)
                return BadRequest();
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var createdUserEntity = userRepository.Insert(mapper.Map<UserEntity>(user));
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
        }

        [HttpPut("{userId}")]
        public IActionResult PutUserUpsert([FromRoute] Guid userId, [FromBody] UpdateUserDto? updateUser)
        {
            if (updateUser is null)
                return BadRequest();
            if (userId == Guid.Empty)
                return BadRequest();
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            updateUser.Id = userId;
            userRepository.UpdateOrInsert(mapper.Map<UserEntity>(updateUser), out var isInserted);
            if (isInserted)
                return CreatedAtRoute(
                    nameof(GetUserById),
                    new { userId },
                    userId);
            return NoContent();
        }

        [HttpPatch("{userId}")]
        public IActionResult PatchUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateUserDto>? patchDoc)
        {
            var updateDto = mapper.Map<UpdateUserDto>(userRepository.FindById(userId));

            if (patchDoc is null)
                return BadRequest();
            if (userId == Guid.Empty || updateDto is null)
                return NotFound();
            patchDoc.ApplyTo(updateDto, ModelState);
            TryValidateModel(updateDto);
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            userRepository.Update(mapper.Map<UserEntity>(updateDto));
            return NoContent();
        }

        [HttpDelete("{userId}")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            if (userId == Guid.Empty || userRepository.FindById(userId) is null)
                return NotFound();
            userRepository.Delete(userId);
            return NoContent();
        }

        [HttpGet]
        public IActionResult GetAllUsers([FromQuery] int pageNumber, [FromQuery] int pageSize = 10)
        {
            pageNumber = Math.Max(pageNumber, 1);
            pageSize = Math.Min(Math.Max(pageSize, 1), 20);

            var page = userRepository.GetPage(pageNumber, pageSize);

            var prevPage = page.CurrentPage - 1;
            var nextPage = page.CurrentPage + 1;
            var paginationHeader = new
            {
                previousPageLink = page.HasPrevious
                    ? linkGenerator.GetUriByRouteValues(HttpContext, "", new { prevPage, page.PageSize })
                    : null,
                nextPageLink = page.HasNext
                    ? linkGenerator.GetUriByRouteValues(HttpContext, "", new { nextPage, page.PageSize })
                    : null,
                totalCount = page.TotalCount,
                pageSize = page.PageSize,
                currentPage = page.CurrentPage,
                totalPages = page.TotalPages
            };

            Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

            return Ok(page);
        }

        [HttpOptions]
        public IActionResult GetUserOptions()
        {
            Response.Headers.Append("Allow", value);

            return Ok();
        }
    }
}
