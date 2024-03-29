﻿using Microsoft.AspNetCore.Mvc;
using ProtrndWebAPI.Services.Network;

namespace ProtrndWebAPI.Controllers
{
    [Route("api/search")]
    [ApiController]
    [ProTrndAuthorizationFilter]
    public class SearchController : BaseController
    {
        public SearchController(IServiceProvider serviceProvider) : base(serviceProvider) { }

        [HttpGet("get/{search}")]
        public async Task<ActionResult<ActionResponse>> GetSearchResults(string search)
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _searchService.GetSearchResultAsync(search) });
        }

        [HttpGet("get/posts/{name}")]
        public async Task<ActionResult<ActionResponse>> GetPosts(string name)
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _searchService.SearchPostsByNameAsync(name) });
        }

        [HttpGet("get/people/{name}")]
        public async Task<ActionResult<ActionResponse>> GetPeople(string name)
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _searchService.SearchProfilesByNameAsync(name) });
        }

        [HttpGet("get/email/{email}")]
        public async Task<ActionResult<ActionResponse>> GetProfileByEmail(string email)
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _searchService.SearchProfilesByEmailAsync(email.ToLower()) });
        }
    }
}
