﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BLL.DTO;
using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.AccountModels;

namespace WebApi.Controllers
{
    [Authorize(Roles = "Admin")] // TODO: Check filter.
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IUserService userService;

        public AdminController(IUserService userService)
        {
            this.userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllUsers()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var managers = await userService.GetAllManagersAsync();
            var workers = await userService.GetAllWorkersAsync();

            var users = new List<UserDTO>();
            users.AddRange(managers);
            users.AddRange(workers);

            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetUser(string id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await userService.GetUserAsync(id);
            if (user != null)
            {
                return Ok(user);
            }
            else
            {
                ModelState.AddModelError("id", "User has not found");
                return BadRequest(ModelState);
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateRole(string Id, [FromBody]RoleChangeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (Id == null || model.Role == null)
            {
                ModelState.AddModelError("", "Invalid userId or roleName");
                return BadRequest(ModelState);
            }
            var result = await userService.UpdateUserRoleAsync(Id, model.Role);
            if (result.Succeeded)
            {
                return Ok();
            }
            else
            {
                ModelState.AddModelError(result.Property, result.Message);
                return BadRequest(ModelState);
            }
        }
    }
}