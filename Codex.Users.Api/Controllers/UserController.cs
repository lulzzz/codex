﻿using Codex.Models.Roles;
using Codex.Models.Users;
using Codex.Users.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Codex.Tenants.Framework;

namespace Codex.Users.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "TENANT_MANAGER,USER")]
        public async Task<ActionResult<User>> FindOne(string id)
        {
            string? contextUserId = HttpContext.GetUserId();
            if (!HttpContext.User.IsInRole(RoleConstant.TENANT_MANAGER) && contextUserId != id)
            {
                return Unauthorized();
            }

            var user = await _userService.FindOneAsync(id);

            return user == null ? NotFound(id) : Ok(user);
        }

        [HttpGet]
        [Authorize(Roles = RoleConstant.TENANT_MANAGER)]
        public async Task<ActionResult<IEnumerable<User>>> FindAll([FromQuery] UserCriteria userCriteria)
        {
            var users = await _userService.FindAllAsync(userCriteria);

            return Ok(users);
        }

        [HttpPost]
        [Authorize(Roles = RoleConstant.TENANT_MANAGER)]
        public async Task<ActionResult<User>> CreateUser([FromBody] UserCreator userCreator)
        {
            string? tenantId = HttpContext.GetTenant()?.Id;
            var user = await _userService.CreateAsync(tenantId!, userCreator);

            return CreatedAtAction(nameof(FindOne), new { id = user.Id }, user);
        }

        [HttpPut("{userId}")]
        [Authorize(Roles = "TENANT_MANAGER,USER")]
        public async Task<ActionResult<User>> UpdateUser([FromQuery] string userId, [FromBody] User user)
        {
            string? contextUserId = HttpContext.GetUserId();
            if (!HttpContext.User.IsInRole(RoleConstant.TENANT_MANAGER) && contextUserId != userId)
            {
                return Unauthorized();
            }

            user = user with { Id = userId };

            User? userResult;
            if (!HttpContext.User.IsInRole(RoleConstant.TENANT_MANAGER) && contextUserId == userId)
            {
                userResult = await _userService.FindOneAsync(userId);
                if (userResult == null)
                {
                    return NotFound(userId);
                }
                user = user with
                { // current user without TENANT_MANAGER Role can't update all user fields
                    PasswordHash = null,
                    ActivationCode = null,
                    ActivationValidity = null,
                    Roles = userResult.Roles,
                    Active = userResult.Active                    
                };
            }

            userResult = await _userService.UpdateAsync(user);
            if (userResult == null)
            {
                return NotFound(userId);
            }

            return AcceptedAtAction(nameof(FindOne), new { id = user.Id }, userResult);
        }

        [HttpGet("{userId}/activation")]
        public async Task<ActionResult<User>> ActivateUser([FromQuery] string userId, [FromQuery] string activationCode)
        {
            var user = await _userService.FindOneAsync(userId);
            if (user == null)
            {
                return NotFound(userId);
            }

            user = await _userService.ActivateUserAsync(user, activationCode);

            return Ok(user);
        }
    }
}
