using Microsoft.AspNetCore.Mvc;
using Talabat.APIs.Errors;
using Talabat.APIs.Helper;
using Talabat.Core;
using Talabat.Core.Repositories;
using Talabat.Core.Services;
using Talabat.Repository;
using Talabat.Service;

namespace Talabat.APIs.Extensions
{
    public  static class ApllicationServicesExtentions
    {
        public  static  IServiceCollection AddAppExtentionServices( this IServiceCollection Services)
        {
            Services.AddScoped<IOrderService, OrderService>();
            Services.AddScoped<IUnitOfWork, UnitOfWork>();
            Services.AddScoped<IPaymentService, PaymentService>();
            Services.AddScoped<IPaymentIntentService, StripePaymentIntentService>();
           //Unitofwork solve //Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            Services.AddAutoMapper(typeof(MappingProfiles));

            Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = actionContext =>
                {
                    var errors = actionContext.ModelState
                        .Where(p => p.Value?.Errors.Count > 0)
                        .SelectMany(p => p.Value!.Errors)
                        .Select(E => E.ErrorMessage).ToArray();

                    var ValidationErrorResponse = new ApiValidationErrorResponse()
                    {
                        Errors = errors
                    };

                    return new BadRequestObjectResult(ValidationErrorResponse);
                };
            });

            Services.AddScoped(typeof(IBasketRepository), typeof(BasketRepository));

            return Services;

        }
    }
}
