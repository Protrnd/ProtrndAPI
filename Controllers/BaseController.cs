using Microsoft.AspNetCore.Mvc;
using ProtrndWebAPI.Services;

namespace ProtrndWebAPI.Controllers
{
    public class BaseController : Controller
    {
        public readonly NotificationService? _notificationService;
        public readonly RegistrationService? _regService;
        public readonly ProfileService? _profileService;
        public readonly SearchService? _searchService;
        public readonly PostsService? _postsService;
        public readonly IUserService? _userService;
        public readonly TagsService? _tagsService;
        public readonly TokenClaims? _profileClaims;
        public readonly PaymentService? _paymentService;
        public readonly ChatService? _chatService;

        public BaseController(IServiceProvider serviceProvider)
        {
            _regService = serviceProvider.GetService<RegistrationService>();
            _userService = serviceProvider.GetService<IUserService>();
            _notificationService = serviceProvider.GetService<NotificationService>();
            _postsService = serviceProvider.GetService<PostsService>();
            _profileService = serviceProvider.GetService<ProfileService>();
            if (_userService != null) _profileClaims = _userService.GetProfileTokenClaims();
            _searchService = serviceProvider.GetService<SearchService>();
            _tagsService = serviceProvider.GetService<TagsService>();
            _chatService = serviceProvider.GetService<ChatService>();
            _paymentService = serviceProvider.GetService<PaymentService>();
        }
    }
}
