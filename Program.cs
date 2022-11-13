global using ProtrndWebAPI.Services.UserSevice;
global using ProtrndWebAPI.Models;
global using ProtrndWebAPI.Models.Posts;
global using ProtrndWebAPI.Models.Response;
global using ProtrndWebAPI.Models.User;
using ProtrndWebAPI.Services;
using ProtrndWebAPI.Settings;
using ProtrndWebAPI.Services.Network;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Swashbuckle.AspNetCore.Filters;
using System.Text;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;
using Microsoft.AspNetCore.Authentication.Cookies;
using CRONJOBTesting.JobFactory;
using CRONJOBTesting.Models;
using CRONJOBTesting.Schedular;
using Quartz.Impl;
using Quartz.Spi;
using Quartz;
using CRONJOBTesting.Jobs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(p => p.AddPolicy(Constants.CORS, builder =>
{
    builder.SetIsOriginAllowed(host => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
}));

builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.Configure<DBSettings>(builder.Configuration.GetSection("DBConnection"));
builder.Services.AddSingleton<RegistrationService>();
builder.Services.AddSingleton<PostsService>();
builder.Services.AddSingleton<ProfileService>();
builder.Services.AddSingleton<CategoriesService>();
builder.Services.AddSingleton<SearchService>();
builder.Services.AddSingleton<TagsService>();
builder.Services.AddSingleton<NotificationService>();
builder.Services.AddSingleton<PaymentService>();
builder.Services.AddSingleton<IJobFactory, JobFactory>();
builder.Services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
builder.Services.AddSingleton<PromotionPaymentJob>();
builder.Services.AddSingleton(new JobMetadata(Guid.NewGuid(), typeof(PromotionPaymentJob), "Promotion Payment Job", "0/10 * * * * ?"));
builder.Services.AddHostedService<MySchedular>();
builder.Services.AddAntiforgery(o => o.SuppressXFrameOptionsHeader = true);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Authorize user using Bearer scheme (\"bearer {token}\")",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = HeaderNames.Authorization,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });
    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "JWT_OR_COOKIE";
    options.DefaultChallengeScheme = "JWT_OR_COOKIE";
}).AddCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetSection("AppSettings:Token").Value)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
})
    .AddPolicyScheme("JWT_OR_COOKIE", "JWT_OR_COOKIE", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            string? authorization = context.Request.Cookies[".AspNetCore.Cookies"];
            if (!string.IsNullOrEmpty(authorization) && authorization != null)
                return CookieAuthenticationDefaults.AuthenticationScheme;
            return JwtBearerDefaults.AuthenticationScheme;
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<ErrorHandlerMiddleware>();

app.UseAuthentication();

app.UseRouting();

app.UseStaticFiles();

app.UseCors(Constants.CORS);

app.UseCookiePolicy();

app.UseAuthorization();

app.MapControllers();

app.MapDefaultControllerRoute();

app.Run();
