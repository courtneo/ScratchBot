using Microsoft.Azure.ProcessSimple.Data.Components.AdaptiveCards;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AdaptiveCards;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    [Serializable]
    public class EchoDialog : IDialog<object>
    {
        protected int count = 1;

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            try
            {
                var message = await argument;
                var incomingActivity = (message as Activity);

                if (message.Text == null)
                {
                    var adaptiveActionData = AdaptiveActionData.Deserialize(incomingActivity.Value.ToString());

                    if (adaptiveActionData.ActionType == AdaptiveActionType.OptionsResponse)
                    {
                        string responderName = incomingActivity.From.Name;
                        var cultureInfo = CultureInfo.CurrentCulture;
                        var adaptiveCard = AdaptiveCardBuilder.BuildOptionsResponseCard(
                            cultureInfo: cultureInfo,
                            optionResponseData: adaptiveActionData as AdaptiveOptionsResponseData,
                            responseDate: DateTime.UtcNow,
                            responderName: responderName);

                        var replyActivity = incomingActivity.CreateReply();
                        replyActivity.Attachments = new List<Attachment>();

                        var attachment = new Attachment
                        {
                            ContentType = AdaptiveCard.ContentType,
                            Content = adaptiveCard
                        };

                        replyActivity.Attachments.Add(attachment);

                        var connectorClient = new ConnectorClient(new Uri(incomingActivity.ServiceUrl));
                        await connectorClient.Conversations.UpdateActivityAsync(incomingActivity.Conversation.Id, incomingActivity.ReplyToId, replyActivity);
                    }
                    else if (adaptiveActionData.ActionType == AdaptiveActionType.ApprovalResponse)
                    {
                        string responderName = incomingActivity.From.Name;
                        var cultureInfo = CultureInfo.CurrentCulture;
                        var adaptiveCard = AdaptiveCardBuilder.BuildApprovalResponseCard(cultureInfo, responderName, DateTime.UtcNow, adaptiveActionData as AdaptiveApprovalResponseData);

                        var replyActivity = incomingActivity.CreateReply();
                        replyActivity.Attachments = new List<Attachment>();

                        var attachment = new Attachment
                        {
                            ContentType = AdaptiveCard.ContentType,
                            Content = adaptiveCard
                        };

                        replyActivity.Attachments.Add(attachment);

                        var connectorClient = new ConnectorClient(new Uri(incomingActivity.ServiceUrl));
                        await connectorClient.Conversations.UpdateActivityAsync(incomingActivity.Conversation.Id, incomingActivity.ReplyToId, replyActivity);
                    }
                }
                else
                {
                    var trimmedText = new Regex("<at>[a-zA-Z]+</at>").Replace(message.Text, "").Trim();

                    if (trimmedText == "reset")
                    {
                        PromptDialog.Confirm(
                            context,
                            AfterResetAsync,
                            "Are you sure you want to reset the count?",
                            "Didn't get that!",
                            promptStyle: PromptStyle.Auto);
                    }
                    else if (trimmedText.StartsWith("lookup"))
                    {
                        var teamId = trimmedText.Replace("lookup", "").Trim();
                        string appId = "087f000e-5e1c-4114-b991-6cc0845783d9";
                        string appPassword = "QW@%*>f(h_GrHRf(";
                        string scope = "https://api.botframework.com/.default";

                        var queryParams = new KeyValuePair<string, string>[]
                        {
                            new KeyValuePair<string, string>("grant_type", "client_credentials"),
                            new KeyValuePair<string, string>("client_id", appId),
                            new KeyValuePair<string, string>("client_secret", appPassword),
                            new KeyValuePair<string, string>("scope", scope)
                        };

                        using (var httpClient = new HttpClient())
                        {
                            HttpResponseMessage response;
                            string token;
                            string teamDetailsRequestUrl = $"{message.ServiceUrl}v3/teams/{teamId}";

                            using (var tokenRequestContent = new FormUrlEncodedContent(queryParams))
                            {
                                string tokenUrl = "https://login.microsoftonline.com/botframework.com/oauth2/v2.0/token";
                                string tokenContentType = "application/x-www-form-urlencoded";
                                tokenRequestContent.Headers.Clear();
                                tokenRequestContent.Headers.Add("Content-Type", tokenContentType);
                                response = await httpClient.PostAsync(tokenUrl, tokenRequestContent);
                                var responseContent = await response.Content.ReadAsStringAsync();
                                token = JsonConvert.DeserializeObject<JObject>(responseContent)["access_token"].ToString();
                            }

                            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                            response = await httpClient.GetAsync(teamDetailsRequestUrl);
                            await context.PostAsync($"Details for team {teamId}: {response.Content.ReadAsStringAsync().Result}");
                            context.Wait(MessageReceivedAsync);
                        }
                    }
                    else if (trimmedText.StartsWith("notification"))
                    {
                        var notification = trimmedText.Substring("notification".Length).Trim();
                        var notificationSegments = notification.Split(';').ToList();

                        var notificationTitle = notificationSegments[0].Trim();
                        notificationSegments.RemoveAt(0);

                        var notificationBody = notificationSegments.Count > 0 ? notificationSegments[0].Trim() : "";
                        notificationSegments.RemoveAt(0);

                        var notificationItemLinkTitle = notificationSegments.Count > 0 ? notificationSegments[0].Trim() : "";
                        notificationSegments.RemoveAt(0);

                        var notificationItemLinkUrl = notificationSegments.Count > 0 ? notificationSegments[0].Trim() : "";
                        notificationSegments.RemoveAt(0);

                        var notificationCreationDate = DateTime.Now.AddHours(-1);
                        var cultureInfo = CultureInfo.CurrentCulture;

                        var replyActivity = incomingActivity.CreateReply("The following notification has been issued");
                        replyActivity.Attachments = new List<Attachment>();

                        var adaptiveCard = AdaptiveCardBuilder.BuildNotificationCard(
                            cultureInfo: cultureInfo,
                            notificationTitle: notificationTitle,
                            notificationBody: notificationBody,
                            notificationItemLinkTitle: notificationItemLinkTitle,
                            notificationItemLinkUrl: notificationItemLinkUrl);

                        var attachment = new Attachment
                        {
                            ContentType = AdaptiveCard.ContentType,
                            Content = adaptiveCard
                        };

                        replyActivity.Attachments.Add(attachment);
                        await context.PostAsync(replyActivity);

                    }
                    else if (trimmedText.StartsWith("choice"))
                    {
                        var choice = trimmedText.Substring("choice".Length).Trim();
                        var choiceSegments = choice.Split(';').ToList();
                        var options = new[] { "option 1", "option 2", "option 3" };

                        var choiceTitle = choiceSegments[0].Trim();
                        choiceSegments.RemoveAt(0);

                        var choiceDetails = choiceSegments.Count > 0 ? choiceSegments[0].Trim() : "";
                        choiceSegments.RemoveAt(0);

                        var choiceItemLinkDescription = choiceSegments.Count > 0 ? choiceSegments[0].Trim() : "";
                        choiceSegments.RemoveAt(0);

                        var choiceItemLink = choiceSegments.Count > 0 ? choiceSegments[0].Trim() : "";
                        choiceSegments.RemoveAt(0);

                        var choiceCreationDate = DateTime.Now.AddHours(-1);
                        var cultureInfo = CultureInfo.CurrentCulture;

                        var replyActivity = incomingActivity.CreateReply("Your choice has been requested for the following item");
                        replyActivity.Attachments = new List<Attachment>();
                        var choiceResponseData = new AdaptiveOptionsResponseData { Options = options };

                        var adaptiveCard = AdaptiveCardBuilder.BuildOptionsRequestCard(
                            cultureInfo: cultureInfo,
                            choiceTitle: choiceTitle,
                            choiceCreationDate: choiceCreationDate,
                            requestorName: incomingActivity.From.Name,
                            choiceDetails: choiceDetails,
                            choiceItemLinkDescription: choiceItemLinkDescription,
                            choiceItemLink: choiceItemLink,
                            choiceResponseData: choiceResponseData);

                        var attachment = new Attachment
                        {
                            ContentType = AdaptiveCard.ContentType,
                            Content = adaptiveCard
                        };

                        replyActivity.Attachments.Add(attachment);
                        await context.PostAsync(replyActivity);
                    }
                    else if (trimmedText.StartsWith("approval"))
                    {
                        var environment = Guid.NewGuid().ToString();
                        var approvalName = Guid.NewGuid().ToString();
                        var approval = trimmedText.Substring("approval".Length).Trim();
                        var approvalSegments = approval.Split(';').ToList();
                        var approvalOptions = new[] { "option 1", "option 2", "option 3" };

                        var approvalTitle = approvalSegments[0].Trim();
                        approvalSegments.RemoveAt(0);

                        var approvalDetails = approvalSegments.Count > 0 ? approvalSegments[0].Trim() : "";
                        approvalSegments.RemoveAt(0);

                        var approvalLink = approvalSegments.Count > 0 ? approvalSegments[0].Trim() : "";
                        approvalSegments.RemoveAt(0);

                        var approvalItemLinkDescription = approvalSegments.Count > 0 ? approvalSegments[0].Trim() : "";
                        approvalSegments.RemoveAt(0);

                        var approvalItemLink = approvalSegments.Count > 0 ? approvalSegments[0].Trim() : "";
                        approvalSegments.RemoveAt(0);

                        var approvalCreationDate = DateTime.Now.AddHours(-1);
                        var cultureInfo = CultureInfo.CurrentCulture;

                        var replyActivity = incomingActivity.CreateReply("Your approval has been requested for the following item");
                        replyActivity.Attachments = new List<Attachment>();

                        var adaptiveCard = AdaptiveCardBuilder.BuildApprovalRequestCard(
                            cultureInfo: cultureInfo,
                            approvalTitle: approvalTitle,
                            approvalCreationDate: approvalCreationDate,
                            requestorName: incomingActivity.From.Name,
                            approvalDetails: approvalDetails,
                            approvalItemLinkDescription: approvalItemLinkDescription,
                            approvalItemLink: approvalItemLink,
                            environment: environment,
                            approvalLink: approvalLink,
                            approvalName: approvalName,
                            approvalOptions: approvalOptions);

                        var attachment = new Attachment
                        {
                            ContentType = AdaptiveCard.ContentType,
                            Content = adaptiveCard
                        };

                        replyActivity.Attachments.Add(attachment);
                        await context.PostAsync(replyActivity);
                    }
                    else
                    {
                        await context.PostAsync($"{this.count++}: I say you said {message.Text}");
                        context.Wait(MessageReceivedAsync);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.Write(ex.ToString());
            }
        }

        public async Task AfterResetAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                this.count = 1;
                await context.PostAsync("Reset count.");
            }
            else
            {
                await context.PostAsync("Did not reset count.");
            }

            context.Wait(MessageReceivedAsync);
        }
    }
}