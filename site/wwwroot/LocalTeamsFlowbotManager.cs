extern alias FlowWeb;
extern alias FlowData;
extern alias FlowCommon;
using System;
using System.Web.Http;
using System.Threading.Tasks;
using System.Globalization;
using FlowWeb::Microsoft.Azure.ProcessSimple.Web.Components;
using FlowData::Microsoft.Azure.ProcessSimple.Data.Components.AdaptiveCards;
using FlowData::Microsoft.Azure.ProcessSimple.Data.Configuration;
using FlowData::Microsoft.Azure.ProcessSimple.Data.DataProviders;
using FlowData::Microsoft.Azure.ProcessSimple.Data.Entities;
using FlowCommon::Microsoft.Azure.ProcessSimple.Common.Logging;
using AdaptiveCards;

namespace SimpleEchoBot
{
    public class LocalTeamsFlowbotManager : TeamsFlowbotManager
    {
        private CreateBotActivityDelegate CreateActivity { get; set; }

        private SendBotActivityAsyncDelegate AsyncPostActivity { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalTeamsFlowbotManager" /> class.
        /// </summary>
        /// <param name="processSimpleConfiguration">The process simple configuration.</param>
        /// <param name="httpConfiguration">The HTTP configuration.</param>
        /// <param name="createActivity">Delegate to create a new bot activity.</param>
        /// <param name="postActivityAsync">Delegate to post an activity.</param>
        /// <param name="updateActivityAsync">Delegate to update an exiting, already-posted activity.</param>
        public LocalTeamsFlowbotManager(
            ProcessSimpleConfiguration processSimpleConfiguration,
            HttpConfiguration httpConfiguration,
            CreateBotActivityDelegate createActivity,
            SendBotActivityAsyncDelegate postActivityAsync,
            SendBotActivityAsyncDelegate updateActivityAsync)
            : base(
                  processSimpleConfiguration: processSimpleConfiguration,
                  httpConfiguration: httpConfiguration,
                  createActivity: createActivity,
                  updateActivityAsync: updateActivityAsync)
        {
            this.CreateActivity = createActivity;
            this.AsyncPostActivity = postActivityAsync;
        }

        /// <summary>
        /// Send a notification to teams.
        /// </summary>
        /// <param name="notificationRequestData">Data about the notification to send.</param>
        /// <typeparam name="T">The type of the recipient of the notification</typeparam>
        public Task SendNotification<T>(BotNotificationWithLinkRequest<T> notificationRequestData) where T : BotRecipient
        {
            // todo: split up recipients array and look them up in graph
            string requestorName = notificationRequestData.Recipient is UserBotRecipient
                ? (notificationRequestData.Recipient as UserBotRecipient).To
                : (notificationRequestData.Recipient as ChannelBotRecipient).ChannelId;

            var adaptiveCard = AdaptiveCardBuilder.BuildNotificationCard(
                cultureInfo: CultureInfo.CurrentCulture,
                notificationTitle: notificationRequestData.MessageTitle,
                notificationBody: notificationRequestData.MessageBody,
                notificationItemLinkTitle: notificationRequestData.LinkDescription,
                notificationItemLinkUrl: notificationRequestData.LinkURL);

            return this.SendAdaptiveCard(
                adaptiveCard,
                "You have been issued the following notification");
        }

        /// <summary>
        /// Send a message with options to teams.
        /// </summary>
        /// <param name="messageWithOptionsRequestData">Data about the message with options to send.</param>
        /// <param name="notificationUrl">Url to notify when the message with options request has been responded to.</param>
        public Task SendMessageWithOptions(BotMessageWithOptionsRequest<UserBotRecipient> messageWithOptionsRequestData, string notificationUrl = null)
        {
            // todo: split up recipients array and look them up in graph
            string requestorName = messageWithOptionsRequestData.Recipient.To;

            var adaptiveCard = AdaptiveCardBuilder.BuildMessageWithOptionsRequestCard(
                cultureInfo: CultureInfo.CurrentCulture,
                choiceTitle: messageWithOptionsRequestData.MessageTitle,
                choiceCreationDate: DateTime.Now,
                requestorName: requestorName,
                choiceDetails: messageWithOptionsRequestData.MessageBody,
                choiceItemLinkDescription: messageWithOptionsRequestData.LinkDescription,
                choiceItemLink: messageWithOptionsRequestData.LinkURL,
                choiceOptions: messageWithOptionsRequestData.Options,
                notificationUrl: notificationUrl);

            return this.SendAdaptiveCard(
                adaptiveCard,
                "Your choice has been requested for the following item");
        }

        /// <summary>
        /// Send an adaptive card to Teams.
        /// </summary>
        /// <param name="adaptiveCard">The card to send.</param>
        /// <param name="message">A text message to send with the card, which will appear above it when posted.</param>
        public Task SendAdaptiveCard(AdaptiveCard adaptiveCard, string message)
        {
            return this.AsyncPostActivity(this.CreateActivity(message).WithAttachment(adaptiveCard));
        }

        protected override LogicAppsRuntimeDataProvider GetLogicAppsRuntimeDataProvider()
        {
            return new LogicAppsRuntimeDataProvider(eventSource: ProcessSimpleLog.Current, serviceClientDataProvider: new ServiceClientDataProvider());
        }
    }
}