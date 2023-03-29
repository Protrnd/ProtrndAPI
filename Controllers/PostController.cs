using Microsoft.AspNetCore.Mvc;
using ProtrndWebAPI.Services.Network;

namespace ProtrndWebAPI.Controllers
{
    [Route("api/post")]
    [ApiController]
    [ProTrndAuthorizationFilter]
    public class PostController : BaseController
    {
        public PostController(IServiceProvider serviceProvider) : base(serviceProvider) { }

        [HttpGet("get")]
        public async Task<ActionResult<ActionResponse>> GetPosts()
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _postsService.GetAllPostsAsync() });
        }

        [HttpGet("fetch/{page}")]
        public async Task<ActionResult<ActionResponse>> GetPostsPaginated(int page)
        {
            return Ok(new ActionResponse { Successful = true, Message = $"Posts results for page {page}", StatusCode = 200, Data = await _postsService.GetPagePostsAsync(page) });
        }

        [HttpGet("fetch/query")]
        public async Task<ActionResult<ActionResponse>> GetPostsByQuery([FromQuery] PostQuery query)
        {
            return Ok(new ActionResponse { Successful = true, Message = $"Posts results", StatusCode = 200, Data = await _postsService.GetPostQuery(query) });
        }

        [HttpGet("get/count/{word}")]
        public async Task<ActionResult<ActionResponse>> GetQueryCount(string word)
        {
            return Ok(new ActionResponse { Successful = true, Message ="Result OK", StatusCode = 200, Data = await _postsService.GetQueryCount(word) });
        }

        [HttpGet("get/promotions")]
        public async Task<ActionResult<ActionResponse>> GetPromotions()
        {
            return Ok(new ActionResponse { Successful = true, Message = ActionResponseMessage.Ok, StatusCode = 200, Data = await _postsService.GetPromotionsAsync(_profileClaims) });
        }

        [HttpGet("fetch/promotions/{page}")]
        public async Task<ActionResult<ActionResponse>> GetPromotionsPaginated(int page)
        {
            return Ok(new ActionResponse { Successful = true, Message = $"Promotions results for page {page}", StatusCode = 200, Data = await _postsService.GetPromotionsPaginatedAsync(page, _profileClaims) });
        }

        //[HttpGet("{id}/gift/profiles")]
        //public async Task<ActionResult<ActionResponse>> GetGifters(Guid id)
        //{
        //    return NotFound(new ActionResponse { StatusCode = 404, Message = ActionResponseMessage.NotFound});
        //    return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _postsService.GetGiftersAsync(id) });
        //}

        [HttpPost("add")]
        public async Task<ActionResult<ActionResponse>> AddPost([FromBody] PostDTO upload)
        {
            var post = new Post { AcceptGift = false, Location = upload.Location, UploadUrls = upload.UploadUrls, Caption = upload.Caption, ProfileId = _profileClaims.ID };
            var uploadResult = await _postsService.AddPostAsync(post);
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = uploadResult });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ActionResponse>> GetPost(Guid id)
        {
            var post = await _postsService.GetSinglePostAsync(id);
            if (post == null)
                return NotFound(new ActionResponse { StatusCode = 404, Message = ActionResponseMessage.NotFound });
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = post });
        }

        [HttpGet("{id}/posts")]
        public async Task<ActionResult<ActionResponse>> GetUserPosts(Guid id)
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _postsService.GetUserPostsAsync(id) });
        }

        [HttpGet("{id}/likes")]
        public async Task<ActionResult<ActionResponse>> GetLikes(Guid id)
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _postsService.GetPostLikesAsync(id) });
        }

        [HttpPost("like/{id}")]
        public async Task<ActionResult<ActionResponse>> AddLike(Guid id)
        {
            var post = await _postsService.GetSinglePostAsync(id);
            if (post != null)
            {
                var like = new Like { SenderId = _profileClaims.ID, Time = DateTime.Now, UploadId = id };
                var liked = await _postsService.AddLikeAsync(like);
                if (liked)
                {
                    await _notificationService.LikeNotification(_profileClaims, post.ProfileId, post.Identifier);
                    return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = true });
                }
            }
            return NotFound(new ActionResponse { Message = ActionResponseMessage.BadRequest });
        }

        [HttpGet("is-liked/{id}")]
        public async Task<ActionResult<ActionResponse>> GetIsLiked(Guid id)
        {
            return Ok(new ActionResponse { Successful = true, Message = "Result Ok", StatusCode = 200, Data = await _postsService.IsLikedAsync(new LikeDTO { SenderId = _profileClaims.ID, UploadId = id }) });
        }

        [HttpDelete("delete/like/{id}")]
        public async Task<ActionResult<ActionResponse>> RemoveLike(Guid id)
        {
            var post = await _postsService.GetSinglePostAsync(id);
            if (post != null)
            {
                var liked = await _postsService.RemoveLike(id, _profileClaims.ID);
                if (liked)
                    return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = false });
            }
            return NotFound(new ActionResponse { StatusCode = 404, Message = ActionResponseMessage.NotFound });
        }

        [HttpGet("{id}/like-count")]
        public async Task<ActionResult<ActionResponse>> GetLikesCount(Guid id)
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _postsService.GetLikesCountAsync(id) });
        }

        [HttpPost("comment")]
        public async Task<ActionResult<ActionResponse>> AddComment(CommentDTO commentDTO)
        {
            var post = await _postsService.GetSinglePostAsync(commentDTO.PostId);
            if (post != null)
            {
                var comment = new Comment { UserId = _profileClaims.ID, PostId = commentDTO.PostId, CommentContent = commentDTO.CommentContent };
                comment.Identifier = comment.Id;
                await _notificationService.CommentNotification(_profileClaims, post.ProfileId, post.Identifier);
                var commentResult = await _postsService.InsertCommentAsync(comment);
                return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = commentResult });
            }
            return NotFound(new ActionResponse { StatusCode = 404, Message = ActionResponseMessage.NotFound });
        }

        [HttpGet("{id}/comments")]
        public async Task<ActionResult<ActionResponse>> GetComments(Guid id)
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _postsService.GetCommentsAsync(id) });
        }

        [HttpDelete("delete/{id}")]
        public async Task<ActionResult<ActionResponse>> DeletePost(Guid id)
        {
            var delete = await _postsService.DeletePostAsync(id, _profileClaims.ID);
            if (!delete)
                return BadRequest(new ActionResponse { Message = ActionResponseMessage.BadRequest });
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok });
        }
    }
}
