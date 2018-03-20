using System;
using Autofac;
using System.Web.Http;
using System.Configuration;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.Azure;

namespace Bot_Application1
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            // Implement an in memory storage for the states
            // Can be replaced by a SQL Storage or Cosmos DB.
            // See https://docs.microsoft.com/en-us/bot-framework/dotnet/bot-builder-dotnet-state
            Conversation.UpdateContainer(
            builder =>
            {
                var store = new InMemoryDataStore();
                builder.Register(c => store)
                          .Keyed<IBotDataStore<BotData>>(AzureModule.Key_DataStore)
                          .AsSelf()
                          .SingleInstance();
            });
        }
    }
}
