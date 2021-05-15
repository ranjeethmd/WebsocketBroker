using ebsocketBroker.Core.Default.Interfaces;
using ebsocketBroker.Core.Default.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using WebsocketBroker.Abstractions;
using WebsocketBroker.Core.Default;

namespace WebsocketBroker.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void Bootstrap(this IServiceCollection services)
        {
            services.AddSingleton<ITcpClientManager, TcpClientManager>();
            services.AddSingleton<IRequestHandler, RequestHandler>();

            services.AddSingleton<IServer>( provider => {
                var logger = provider.GetRequiredService<ILogger<TcpServer>>();
                var config = provider.GetRequiredService<IConfiguration>();

                var port = config.GetValue<int>("Port");

                return new TcpServer(logger, IPAddress.Any, 80);              
            });
            services.AddSingleton<IBrokerManager, MessageBroker>();
            services.AddSingleton<IConnectionManagement, PollingConnectionCheckStratergy>();
            services.AddSingleton<IFrameHandler, FrameHandler>();
            services.AddSingleton<IRequestHandler, RequestHandler>();
            services.AddSingleton<IResponseHandler, ResponseHandler>();
            services.AddSingleton<ISubscriptionManager, SubscriptionManager>();


            services.AddSingleton<ITopicFactory>(provider => {
                var config = provider.GetRequiredService<IConfiguration>();
                return new TopicFactory(Environment.ExpandEnvironmentVariables(config["LedgerPath"]));            
            });
        }

    }
}
