extern alias FlowData;
extern alias FlowWeb;
extern alias FlowCommon;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using Microsoft.WindowsAzure.ResourceStack.Common.Instrumentation;
using FlowData::Microsoft.Azure.ProcessSimple.Data.Entities;
using FlowWeb::Microsoft.Azure.ProcessSimple.Web.Extensions;
using FlowCommon::Microsoft.Azure.ProcessSimple.Common.Extensions;
using FlowData::Microsoft.Azure.ProcessSimple.Data.Extensions;
using FlowWeb::Microsoft.Azure.ProcessSimple.Web.Common;
using FlowData::Microsoft.Azure.ProcessSimple.Data.Configuration;
using FlowWeb::Microsoft.Azure.ProcessSimple.Web.Components;

namespace SimpleEchoBot.Controllers
{
    public class TeamsFlowbotConnectorController : ApiController
    {
        /// <summary>
        /// Post a new adaptive card to a user.
        /// </summary>
        /// <param name="recipientType">The type of the recipient.</param>
        [HttpPost]
        public async Task<HttpResponseMessage> PostAdaptiveCard(string recipientType)
        {
            this.PopulateSenderFromAuthHeader();
            string operationName = "TeamsFlowbotActionsConnectorController.PostAdaptiveCard";
            TeamsFlowbotRecipientType teamsFlowbotRecipientType = recipientType.ParseWithDefault(defaultValue: TeamsFlowbotRecipientType.NotSpecified);
            Validation.RecipientType(teamsFlowbotRecipientType);

            return teamsFlowbotRecipientType == TeamsFlowbotRecipientType.User
                ? await this
                    .GetTeamsFlowbotManager()
                    .PostMessageAsync<BotMessageRequest<UserBotRecipient>, UserBotRecipient>(operationName, this.Request, false, TeamsFlowbotActionType.AdaptiveCard)
                    .ConfigureAwait(continueOnCapturedContext: false)
                : await this
                    .GetTeamsFlowbotManager()
                    .PostMessageAsync<BotMessageRequest<ChannelBotRecipient>, ChannelBotRecipient>(operationName, this.Request, false, TeamsFlowbotActionType.AdaptiveCard)
                    .ConfigureAwait(continueOnCapturedContext: false);
        }

        /// <summary>
        /// Create a new notification.
        /// </summary>
        [HttpPost]
        public async Task<HttpResponseMessage> PostUserNotification()
        {
            this.PopulateSenderFromAuthHeader();
            string operationName = "TeamsFlowbotActionsConnectorController.NotifyUser";

            return await this
                .GetTeamsFlowbotManager()
                .PostMessageAsync<BotNotificationRequest<UserBotRecipient>, UserBotRecipient>(operationName, this.Request, false, TeamsFlowbotActionType.Notification)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        /// <summary>
        /// Create a new notification.
        /// </summary>
        [HttpPost]
        public async Task<HttpResponseMessage> PostChannelNotification()
        {
            this.PopulateSenderFromAuthHeader();
            string operationName = "TeamsFlowbotActionsConnectorController.NotifyChannel";

            return await this
                .GetTeamsFlowbotManager()
                .PostMessageAsync<BotMessageRequest<ChannelBotRecipient>, ChannelBotRecipient>(operationName, this.Request, false, TeamsFlowbotActionType.Notification)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        /// <summary>
        /// Create and subscribe to a new message with options.
        /// </summary>
        /// <param name="recipientType">The type of the recipient.</param>
        [HttpPost]
        public async Task<HttpResponseMessage> PostAndWaitForMessageWithOptions(string recipientType)
        {
            this.PopulateSenderFromAuthHeader();
            string operationName = "TeamsFlowbotActionsConnectorController.PostAndWaitForMessageWithOptions";
            TeamsFlowbotRecipientType teamsFlowbotRecipientType = recipientType.ParseWithDefault(defaultValue: TeamsFlowbotRecipientType.NotSpecified);
            Validation.RecipientType(teamsFlowbotRecipientType);

            return teamsFlowbotRecipientType == TeamsFlowbotRecipientType.User
                ? await this
                    .GetTeamsFlowbotManager()
                    .PostMessageAsync<BotMessageWithOptionsRequest<UserBotRecipient>, UserBotRecipient>(operationName, this.Request, true, TeamsFlowbotActionType.MessageWithOptions)
                    .ConfigureAwait(continueOnCapturedContext: false)
                : await this
                    .GetTeamsFlowbotManager()
                    .PostMessageAsync<BotMessageWithOptionsRequest<ChannelBotRecipient>, ChannelBotRecipient>(operationName, this.Request, true, TeamsFlowbotActionType.MessageWithOptions)
                    .ConfigureAwait(continueOnCapturedContext: false);
        }

        /// <summary>
        /// Create a new message with options.
        /// </summary>
        /// <param name="recipientType">The type of the recipient.</param>
        [HttpPost]
        public async Task<HttpResponseMessage> PostMessageWithOptions(string recipientType)
        {
            this.PopulateSenderFromAuthHeader();
            string operationName = "TeamsFlowbotActionsConnectorController.PostMessageWithOptions";
            TeamsFlowbotRecipientType teamsFlowbotRecipientType = recipientType.ParseWithDefault(defaultValue: TeamsFlowbotRecipientType.NotSpecified);
            Validation.RecipientType(teamsFlowbotRecipientType);

            return teamsFlowbotRecipientType == TeamsFlowbotRecipientType.User
                ? await this
                    .GetTeamsFlowbotManager()
                    .PostMessageAsync<BotMessageWithOptionsRequest<UserBotRecipient>, UserBotRecipient>(operationName, this.Request, false, TeamsFlowbotActionType.MessageWithOptions)
                    .ConfigureAwait(continueOnCapturedContext: false)
                : await this
                    .GetTeamsFlowbotManager()
                    .PostMessageAsync<BotMessageWithOptionsRequest<ChannelBotRecipient>, ChannelBotRecipient>(operationName, this.Request, false, TeamsFlowbotActionType.MessageWithOptions)
                    .ConfigureAwait(continueOnCapturedContext: false);
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

            var metadata = this
                .GetTeamsFlowbotManager()
                .GetMetadata(teamsFlowbotActionType, teamsFlowbotRecipientType, connectorMetadataType);

            return this.Request.CreateResponse(statusCode: HttpStatusCode.OK, value: metadata);
        }

        private TeamsFlowbotManager GetTeamsFlowbotManager()
        {
            return new TeamsFlowbotManager(
                processSimpleConfiguration: ProcessSimpleConfiguration.Instance,
                httpConfiguration: GlobalConfiguration.Configuration,
                withUnencryptedFlowbotPassword: true);
        }

        private void PopulateSenderFromAuthHeader()
        {
            var token = this.Request.Headers.Authorization.Parameter;
            var parsedToken = new JwtSecurityTokenHandler().ReadJwtToken(token) as JwtSecurityToken;
            var connectionRequestIdentity = new RequestIdentity
            {
                AuthenticationType = PowerFlowConstants.MicrosoftGraphTokenAuthenticationType,
                IsAuthenticated = true,
                Claims = parsedToken.Claims
                    .Where(claim => !claim.Type.EqualsInsensitively("signin_state"))
                    .Where(claim => !claim.Type.EqualsInsensitively("amr"))
                    .ToInsensitiveDictionary(claim => claim.Type, claim => claim.Value),
                Name = parsedToken.Claims.Where(claim => claim.Type.EqualsOrdinal("name")).FirstOrDefault().Value
            };

            // Get tenantId and objectId set them into authentication identity
            var tenantId = parsedToken.Claims.Where(thisClaim => thisClaim.Type.EqualsOrdinal("tid")).FirstOrDefault().Value;
            var objectId = parsedToken.Claims.Where(thisClaim => thisClaim.Type.EqualsOrdinal("oid")).FirstOrDefault().Value;
            connectionRequestIdentity.Claims.Add(ClaimsConstants.ArmClaimNames.TenantId, tenantId);
            connectionRequestIdentity.Claims.Add(ClaimsConstants.ArmClaimNames.ObjectId, objectId);
            RequestCorrelationContext.Current.SetAuthenticationIdentity(connectionRequestIdentity);
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