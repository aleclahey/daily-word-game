/*
 * Program:         Project2_Wordle
 * File:            Program.cs
 * Date:            March 13, 2025
 * Author:          Jazz Leibur, 0690432, Section 2 & Alec Lahey, 1056146, Section 2
 * Description:     Program to run DailyWordleService and connect to WCF DailyWord server.
 */

using System.ServiceModel;
using WordleGameServer.Services;
using WordServer.Services;
using System.ServiceModel.Channels;

namespace WordleGameServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddGrpc();

            // Register the WCF DailyWord client
            builder.Services.AddSingleton<IDailyWordService>(services =>
            {
                // Create WCF client channel
                var binding = new BasicHttpBinding();
                var endpoint = new EndpointAddress("http://localhost:8080/DailyWordService");
                var channelFactory = new ChannelFactory<IDailyWordService>(binding, endpoint);
                return channelFactory.CreateChannel();
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.MapGrpcService<DailyWordleService>();
            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

            app.Run();
        }
    }
}