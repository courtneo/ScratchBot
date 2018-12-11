extern alias FlowData;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Web.Http;
using System.Web.Http.Routing;
using System.Net.Http;
using FlowData::Microsoft.Azure.ProcessSimple.Data.Configuration;

namespace SimpleEchoBot
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // ProcessSimpleConfiguration.Instance.Initialize().Wait();

            // Json settings
            config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Newtonsoft.Json.Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
            };

            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "TeamsFlowbotConnectorPostNotification",
                routeTemplate: "apis/flowbot/notification",
                defaults: new { id = RouteParameter.Optional, controller = "TeamsFlowbotConnector", action = "PostNotification" },
                constraints: new { httpMethod = new HttpMethodConstraint(HttpMethod.Post) }
            );

            config.Routes.MapHttpRoute(
                name: "TeamsFlowbotConnectorPostMessageWithOptions",
                routeTemplate: "apis/flowbot/messagewithoptions",
                defaults: new { id = RouteParameter.Optional, controller = "TeamsFlowbotConnector", action = "PostMessageWithOptions" },
                constraints: new { httpMethod = new HttpMethodConstraint(HttpMethod.Post) }
            );

            config.Routes.MapHttpRoute(
                name: "TeamsFlowbotConnectorSubscribe",
                routeTemplate: "apis/flowbot/messagewithoptions/$subscriptions",
                defaults: new { id = RouteParameter.Optional, controller = "TeamsFlowbotConnector", action = "PostAndWaitForMessageWithOptions" },
                constraints: new { httpMethod = new HttpMethodConstraint(HttpMethod.Post) }
            );

            config.Routes.MapHttpRoute(
                name: "TeamsFlowbotConnectorUnsubscribe",
                routeTemplate: "apis/flowbot/messagewithoptions/$subscriptions/{subscriptionId}",
                defaults: new { id = RouteParameter.Optional, controller = "TeamsFlowbotConnector", action = "DeleteMessageWithOptions" },
                constraints: new { httpMethod = new HttpMethodConstraint(HttpMethod.Delete) }
            );

            config.Routes.MapHttpRoute(
                name: "TeamsFlowbotConnectorGetMetadata",
                routeTemplate: "apis/flowbot/{actionType}/$metadata.json/{metadataType}",
                defaults: new { id = RouteParameter.Optional, controller = "TeamsFlowbotConnector", action = "GetMetadata" },
                constraints: new { httpMethod = new HttpMethodConstraint(HttpMethod.Get) }
            );

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
