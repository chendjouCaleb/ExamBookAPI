using System;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Models;
using ExamBook.Identity.Services;
using ExamBook.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ExamBook.Identity
{
    public static class IdentityConfiguration
    {
        public static IServiceCollection AddApplicationIdentity(this IServiceCollection services)
        {
            services
                .AddTransient<UserService>()
                .AddTransient<UserCodeService>()
                .AddTransient<AuthenticationService>()
            .AddIdentity<User, Role>(options =>
                {
                    options.Password.RequireDigit = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireLowercase = false;
                }).AddEntityFrameworkStores<ApplicationIdentityDbContext>()
                .AddDefaultTokenProviders();

            
            

            return services;
        }
    }
}