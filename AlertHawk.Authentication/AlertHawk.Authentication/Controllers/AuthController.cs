﻿using AlertHawk.Application.Interfaces;
using AlertHawk.Authentication.Domain.Custom;
using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Infrastructure.Utils;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AlertHawk.Authentication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IConfiguration _configuration;
        private readonly IUsersMonitorGroupService _monitorGroupService;

        public AuthController(IUserService userService, IJwtTokenService jwtTokenService, IConfiguration configuration,
            IUsersMonitorGroupService monitorGroupService)
        {
            _userService = userService;
            _jwtTokenService = jwtTokenService;
            _configuration = configuration;
            _monitorGroupService = monitorGroupService;
        }

        [HttpPost("azure")]
        [SwaggerOperation(Summary = "Get User Token for mobile app")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AzureMobileAuth([FromBody] AzureAuth azureAuth)
        {
            var user = await _userService.GetByEmail(azureAuth.Email);

            if (user is null)
            {
                var newUser = new UserCreationFromAzure(azureAuth.Email, azureAuth.Email);
                await _userService.CreateFromAzure(newUser);
                user = await _userService.GetByEmail(azureAuth.Email);
                
                var demoMode = Environment.GetEnvironmentVariable("DEMO_MODE") ?? "false";

                if (string.Equals(demoMode, "true", StringComparison.InvariantCultureIgnoreCase))
                {
                    await _monitorGroupService.CreateOrUpdateAsync([
                        new UsersMonitorGroup
                        {
                            UserId = user.Id,
                            GroupMonitorId = 24
                        }
                    ]);
                }
            }

            if (azureAuth.ApiKey != _configuration.GetSection("MOBILE_API_KEY").Value)
            {
                return BadRequest(new Message("Invalid API key."));
            }

            var token = _jwtTokenService.GenerateToken(user);
            await _userService.UpdateUserToken(token, user.Username.ToLower());

            return Ok(new { token });
        }

        [HttpPost("refreshToken")]
        [SwaggerOperation(Summary = "Refresh User Token")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RefreshUserToken()
        {
            try
            {
                var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());
                var user = await _userService.GetUserByToken(jwtToken);

                if (user is null)
                {
                    return BadRequest(new Message("Invalid token."));
                }

                var token = _jwtTokenService.GenerateToken(user);
                await _userService.UpdateUserToken(token, user.Username.ToLower());

                return Ok(new { token });
            }
            catch (Exception err)
            {
                SentrySdk.CaptureException(err);
                return StatusCode(StatusCodes.Status500InternalServerError, new Message("Something went wrong."));
            }
        }

        [HttpPost("login")]
        [SwaggerOperation(Summary = "Authenticate User")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PostUserAuth([FromBody] UserAuth userAuth)
        {
            try
            {
                var enabledLoginAuth = Environment.GetEnvironmentVariable("ENABLED_LOGIN_AUTH") ?? "true";
                if (string.Equals(enabledLoginAuth, "false", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine("BadRequest");
                    return BadRequest(new Message("Login is disabled."));
                }

                var user = await _userService.Login(userAuth.username, userAuth.Password);

                if (user is null)
                {
                    return BadRequest(new Message("Invalid credentials."));
                }

                var token = _jwtTokenService.GenerateToken(user);

                await _userService.UpdateUserToken(token, user.Username.ToLower());

                return Ok(new { token });
            }
            catch (Exception err)
            {
                SentrySdk.CaptureException(err);
                return StatusCode(StatusCodes.Status500InternalServerError, new Message("Something went wrong."));
            }
        }
    }
}