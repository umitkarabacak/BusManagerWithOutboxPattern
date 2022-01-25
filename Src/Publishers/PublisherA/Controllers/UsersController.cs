using Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PublisherA.Data.Contexts;
using PublisherA.Models;
using System.Text.Json;

namespace PublisherA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ILogger<UsersController> _logger;
        private readonly ProjectContext _dbContext;

        public UsersController(ILogger<UsersController> logger
            , ProjectContext projectContext)
        {
            _logger = logger;
            _dbContext = projectContext;
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Get()
        {
            var users = await _dbContext.Users
                .OrderByDescending(u => u.Id)
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var user = await _dbContext.Users
                .Where(u => u.Id.Equals(id))
                .FirstOrDefaultAsync();

            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> Register(UserRegisterRequest userRegisterRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            #region Same Logic ...

            var user = new User
            {
                Username = userRegisterRequest.Username,
                Password = userRegisterRequest.Password
            };

            var outboxMessage = new OutboxMessage
            {
                Payload = JsonSerializer.Serialize(userRegisterRequest),
                EventType = typeof(UserRegisterRequest).ToString()
            };

            await _dbContext.Users.AddAsync(user);
            await _dbContext.OutboxMessages.AddAsync(outboxMessage);
            await _dbContext.SaveChangesAsync();

            #endregion

            _logger.LogInformation("User Registration Completed: " + JsonSerializer.Serialize(userRegisterRequest));

            return Ok(user);
        }
    }
}
