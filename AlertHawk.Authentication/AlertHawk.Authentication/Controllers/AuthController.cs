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

        public AuthController(IUserService userService, IJwtTokenService jwtTokenService)
        {
            _userService = userService;
            _jwtTokenService = jwtTokenService;
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
                var enabledLoginAuth = Environment.GetEnvironmentVariable("ENABLED_LOGIN_AUTH")?.ToLower() ?? "true";
                if(enabledLoginAuth == "false")
                {
                    return BadRequest(new Message("Login is disabled."));
                }
                
                var user = await _userService.Login(userAuth.Username, userAuth.Password);

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