using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Talabat.Core.Entities.Identity;
using Talabat.Core.Services;
using Talabat.Repository.Identity;
using Talabat.Service;

namespace Talabat.APIs.Extensions
{
    public  static class IdentityServicesExtension
    {
        public static IServiceCollection AddIdentityServices(this IServiceCollection services,IConfiguration configuration)
        {
            services.AddScoped<ITokenService, TokenService>();
            services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<AppIdentityDbContext>()
            .AddDefaultTokenProviders();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme=JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme=JwtBearerDefaults.AuthenticationScheme;
            }
                )
                 
                .AddJwtBearer(options =>
                 {
                     var issuer = configuration["JWT:ValidIssuer"] ?? throw new InvalidOperationException("JWT:ValidIssuer is missing");
                     var audience = configuration["JWT:ValidAudience"] ?? throw new InvalidOperationException("JWT:ValidAudience is missing");
                     var key = configuration["JWT:Key"] ?? throw new InvalidOperationException("JWT:Key is missing");

                     options.TokenValidationParameters = new TokenValidationParameters
                     {
                         ValidateIssuer = true,
                         ValidIssuer = issuer,

                         ValidateAudience = true,
                         ValidAudience = audience,

                         ValidateLifetime = true,

                         ValidateIssuerSigningKey = true,
                         IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
                     };
                 });


            return services;

        }
    }
}
