using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

                return new TcpServer(logger, IPAddress.Parse("127.0.0.1"), 80);              
            });
            services.AddSingleton<IBrokerManager, MessageBroker>();
            services.AddSingleton<IConnectionManagement, PollingConnectionCheckStratergy>();
            services.AddSingleton<IFrameHandler, FrameHandler>();
            services.AddSingleton<IRequestHandler, RequestHandler>();
            services.AddSingleton<IResponseHandler, ResponseHandler>();
        }

    }
}
