﻿using System;
using ZSN.Utils.Core.DI;
using ZSN.Utils.Core.Helpers;
using ICanPay.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ZSN.Utils.Core.Pay
{
    public static class PayExtensions
    {
        public static IServiceCollection RegisterPayMerchant<TGateway, TMerchant>(this IServiceCollection services,
            TMerchant merchant)
            where TGateway : GatewayBase, new()
            where TMerchant : class, IMerchant
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (merchant == null)
                throw new ArgumentNullException(nameof(merchant));
            services.AddICanPay(m =>
            {
                var gateways = ServiceLocator.GetInstance<IGateways>() ?? new Gateways();
                var gateway = (TGateway)Activator.CreateInstance(typeof(TGateway), (object)merchant);
                gateways.Add(gateway);

                return gateways;
            });
            return services;
        }

        public static IApplicationBuilder UsePay(this IApplicationBuilder app)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            return app.UseICanPay();
        }
    }
}
