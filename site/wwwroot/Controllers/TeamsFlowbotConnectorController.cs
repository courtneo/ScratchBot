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
        private TeamsFlowbotManager TeamsFlowbotManager { get; set; }

        public TeamsFlowbotConnectorController()
        {
            this.TeamsFlowbotManager = this.GetTeamsFlowbotManager();
        }

        /// <summary>
        /// Create a new notification.
        /// </summary>
        [HttpPost]
        public async Task<HttpResponseMessage> PostNotification()
        {
            var optionsRequestData = await this.Request.Content
                .ReadAsJsonAsync<NotificationRequest>(this.Configuration)
                .ConfigureAwait(continueOnCapturedContext: false);

            await this.TeamsFlowbotManager.SendNotification(optionsRequestData);
            return this.Request.CreateResponse(statusCode: HttpStatusCode.OK);
        }

        /// <summary>
        /// Create and subscribe to a new message with options.
        /// </summary>
        [HttpPost]
        public async Task<HttpResponseMessage> PostAndWaitForMessageWithOptions()
        {
            var optionsRequestDataConnectorSubscription = await this.Request.Content
                .ReadAsJsonAsync<ConnectorSubscriptionInput<MessageWithOptionsRequest>>(this.Configuration)
                .ConfigureAwait(continueOnCapturedContext: false);

            await this.TeamsFlowbotManager.SendMessageWithOptions(optionsRequestDataConnectorSubscription.Body, optionsRequestDataConnectorSubscription.NotificationUrl);
            return this.Request.CreateResponse(statusCode: HttpStatusCode.OK);
        }

        /// <summary>
        /// Create a new message with options.
        /// </summary>
        [HttpPost]
        public async Task<HttpResponseMessage> PostMessageWithOptions()
        {
            var optionsRequestData = await this.Request.Content
                .ReadAsJsonAsync<MessageWithOptionsRequest>(this.Configuration)
                .ConfigureAwait(continueOnCapturedContext: false);

            await this.TeamsFlowbotManager.SendMessageWithOptions(optionsRequestData);
            return this.Request.CreateResponse(statusCode: HttpStatusCode.OK);
        }

        /// <summary>
        /// Unsubscribe from a message with options.
        /// </summary>
        /// <param name="subscriptionId">The id of the flow subscription to mark as completed.</param>
        [HttpDelete]
        public object DeleteMessageWithOptions(string subscriptionId)
        {
            // Flow does not track subscriptions for options messages, all the tracking happens (is this right?) in LogicApps -
            // so this method can just return success
            return this.Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpGet]
        public object GetMetadata(string actionType, string metadataType)
        {
            // the following will parse the enums case-insensitively
            TeamsFlowbotActionType teamsFlowbotActionType = actionType.ParseWithDefault(defaultValue: TeamsFlowbotActionType.NotSpecified);
            var connectorMetadataType = metadataType.ParseWithDefault(defaultValue: ConnectorMetadataType.NotSpecified);
            var metadata = TeamsFlowbotManager.GetMetadata(teamsFlowbotActionType, connectorMetadataType);
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