extern alias FlowData;
extern alias FlowWeb;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using FlowData::Microsoft.Azure.ProcessSimple.Data.Entities;
using FlowWeb::Microsoft.Azure.ProcessSimple.Web.Components;
using FlowWeb::Microsoft.Azure.ProcessSimple.Web.Extensions;

namespace SimpleEchoBot.Controllers
{
    public class TeamsFlowbotConnectorController : ApiController
    {
        private LocalTeamsFlowbotManager TeamsFlowbotManager { get; set; }

        public TeamsFlowbotConnectorController()
        {
            this.TeamsFlowbotManager = this.GetTeamsFlowbotManager();
        }

        /// <summary>
        /// Create a new notification.
        /// </summary>
        [HttpPost]
        public async Task<HttpResponseMessage> PostUserNotification()
        {
            var optionsRequestData = await this.Request.Content
                .ReadAsJsonAsync<BotNotificationWithLinkRequest<UserBotRecipient>>(this.Configuration)
                .ConfigureAwait(continueOnCapturedContext: false);

            await this.TeamsFlowbotManager.SendNotification(optionsRequestData);
            return this.Request.CreateResponse(statusCode: HttpStatusCode.OK);
        }

        /// <summary>
        /// Create a new notification.
        /// </summary>
        [HttpPost]
        public async Task<HttpResponseMessage> PostChannelNotification()
        {
            var optionsRequestData = await this.Request.Content
                .ReadAsJsonAsync<BotNotificationWithLinkRequest<ChannelBotRecipient>>(this.Configuration)
                .ConfigureAwait(continueOnCapturedContext: false);

            await this.TeamsFlowbotManager.SendNotification(optionsRequestData);
            return this.Request.CreateResponse(statusCode: HttpStatusCode.OK);
        }

        /// <summary>
        /// Create and subscribe to a new message with options.
        /// </summary>
        /// <param name="recipientType">The type of the recipient.</param>
        [HttpPost]
        public async Task<HttpResponseMessage> PostAndWaitForMessageWithOptions(string recipientType)
        {
            TeamsFlowbotRecipientType teamsFlowbotRecipientType = recipientType.ParseWithDefault(defaultValue: TeamsFlowbotRecipientType.NotSpecified);


            var optionsRequestDataConnectorSubscription = await this.Request.Content
                .ReadAsJsonAsync<ConnectorSubscriptionInput<BotMessageWithOptionsRequest<UserBotRecipient>>>(this.Configuration)
                .ConfigureAwait(continueOnCapturedContext: false);

            await this.TeamsFlowbotManager.SendMessageWithOptions(optionsRequestDataConnectorSubscription.Body, optionsRequestDataConnectorSubscription.NotificationUrl);
            return this.Request.CreateResponse(statusCode: HttpStatusCode.OK);
        }

        /// <summary>
        /// Create a new message with options.
        /// </summary>
        /// <param name="recipientType">The type of the recipient.</param>
        [HttpPost]
        public async Task<HttpResponseMessage> PostMessageWithOptions(string recipientType)
        {
            TeamsFlowbotRecipientType teamsFlowbotRecipientType = recipientType.ParseWithDefault(defaultValue: TeamsFlowbotRecipientType.NotSpecified);

            var optionsRequestData = await this.Request.Content
                .ReadAsJsonAsync<BotMessageWithOptionsRequest<UserBotRecipient>>(this.Configuration)
                .ConfigureAwait(continueOnCapturedContext: false);

            await this.TeamsFlowbotManager.SendMessageWithOptions(optionsRequestData);
            return this.Request.CreateResponse(statusCode: HttpStatusCode.OK);
        }

        /// <summary>
        /// Unsubscribe from a message with options.
        /// </summary>
        /// <param name="recipientType">The type of the recipient.</param>
        /// <param name="subscriptionId">The id of the flow subscription to mark as completed.</param>
        [HttpDelete]
        public HttpResponseMessage DeleteMessageWithOptions(string recipientType, string subscriptionId)
        {
            // Flow does not track subscriptions for options messages, all the tracking happens (is this right?) in LogicApps -
            // so this method can just return success
            return this.Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpGet]
        public HttpResponseMessage GetMetadata(string actionType, string recipientType, string metadataType)
        {
            // the following will parse the enums case-insensitively
            TeamsFlowbotActionType teamsFlowbotActionType = actionType.ParseWithDefault(defaultValue: TeamsFlowbotActionType.NotSpecified);
            TeamsFlowbotRecipientType teamsFlowbotRecipientType = recipientType.ParseWithDefault(defaultValue: TeamsFlowbotRecipientType.NotSpecified);

            var connectorMetadataType = metadataType.ParseWithDefault(defaultValue: ConnectorMetadataType.NotSpecified);
            var metadata = TeamsFlowbotManager.GetMetadata(teamsFlowbotActionType, teamsFlowbotRecipientType, connectorMetadataType);
            return this.Request.CreateResponse(statusCode: HttpStatusCode.OK, value: metadata);
        }

        // GET: api/Connector
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/Connector/5
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/Connector
        public void Post([FromBody]string value)
        {
        }

        // PUT: api/Connector/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/Connector/5
        public void Delete(int id)
        {
        }
    }
}