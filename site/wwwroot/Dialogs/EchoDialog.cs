extern alias FlowCommon;
extern alias FlowData;
extern alias FlowWeb;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.WindowsAzure.ResourceStack.Common.Instrumentation;
using FlowCommon::Microsoft.Azure.ProcessSimple.Common.Context;
using FlowData::Microsoft.Azure.ProcessSimple.Data.Entities;
using FlowData::Microsoft.Azure.ProcessSimple.Data.Components.AdaptiveCards;
using FlowData::Microsoft.Azure.ProcessSimple.Data.Configuration;
using FlowWeb::Microsoft.Azure.ProcessSimple.Web.Components;
using AdaptiveActionData = FlowData::Microsoft.Azure.ProcessSimple.Data.Components.AdaptiveCards.AdaptiveActionData;

namespace SimpleEchoBot.Dialogs
{
    [Serializable]
    public class EchoDialog : IDialog<object>
    {
        private int count = 1;

        public static Activity LastSeenActivity  { get; set; }

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
                LastSeenActivity = incomingActivity;

                var teamsFlowbotManager = new TeamsFlowbotManager(
                    processSimpleConfiguration: ProcessSimpleConfiguration.Instance,
                    httpConfiguration: GlobalConfiguration.Configuration,
                    withUnencryptedFlowbotPassword: true);

                var sendingAccount = incomingActivity.From.ToBotChannelAccount();
                var responderUserIdentity = new UserIdentity { ObjectId = incomingActivity.From.AadObjectId, UserPrincipalName = sendingAccount.Id };

                if (message.Text == null)
                {
                    // Current flowSvc code is case sensitive on this, and its incoming capitalization from emulator is now lowercase:
                    var adaptiveActionData = AdaptiveActionData.Deserialize(JToken.Parse(incomingActivity.Value.ToString().Replace("actionType", "ActionType")));

                    await teamsFlowbotManager.ReceiveAdaptiveAction(
                        adaptiveActionData: adaptiveActionData,
                        replyActivity: incomingActivity.CreateReply().ToBotActivity(),
                        sendingAccount: sendingAccount,
                        responderUserIdentity: responderUserIdentity,
                        idOfActivityFromWhichTheActionWasEmitted: null, // the method won't use this since we're supplying it with our own post method
                        cancellationToken: new CancellationTokenSource().Token,
                        asyncPostActivity: (botActivity) => {
                            if (incomingActivity.ServiceUrl.StartsWith("http://localhost"))
                            {
                                // Message update appears to be broken in emulator: incomingActivity.ReplyToId is null, and even if we use the value it
                                // should have, it doesn't work. Does not help to set id and replyToId on botActivity to match those of the message we're
                                // updating. So for now, in emulator we post rather than updating.
                                return new ConnectorClient(new Uri(incomingActivity.ServiceUrl))
                                    .Conversations
                                    .ReplyToActivityAsync(incomingActivity.Conversation.Id, incomingActivity.Id, botActivity.ToActivity());
                            }
                            else
                            {
                                return new ConnectorClient(new Uri(incomingActivity.ServiceUrl))
                                    .Conversations
                                    .UpdateActivityAsync(incomingActivity.Conversation.Id, incomingActivity.ReplyToId, botActivity.ToActivity());
                            }
                        }
                    );

                    context.Wait(MessageReceivedAsync);
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

                        var notificationRequestData = new BotNotificationRequest<UserBotRecipient>
                        {
                            Recipient = new UserBotRecipient { To = incomingActivity.From.Name },
                            MessageTitle = PopFrom(notificationSegments),
                            MessageBody = PopFrom(notificationSegments)
                        };

                        // todo: split up recipients array and look them up in graph
                        var adaptiveCard = AdaptiveCardBuilder.BuildNotificationCard(
                            cultureInfo: CultureInfo.CurrentCulture,
                            requestor: new RequestIdentity { Name = (notificationRequestData.Recipient as UserBotRecipient).To },
                            notificationTitle: notificationRequestData.MessageTitle,
                            notificationBody: notificationRequestData.MessageBody);

                        // await context.PostAsync(incomingActivity.CreateReply("You have been issued the following notification").ToBotActivity().WithAttachment(adaptiveCard).ToActivity());
                        var messageWithFooter = string.Format(
                            CultureInfo.InvariantCulture,
                            "{0}\r\n\r\n**{1}**",
                            notificationRequestData.MessageBody,
                            AdaptiveCardBuilder.GetFooterFromRequestor(
                                new RequestIdentity
                                {
                                    Name = (notificationRequestData.Recipient as UserBotRecipient).To //,
                                    // Claims = new Dictionary<string, string> { { "upn", "courtneo@microsoft.com" } }
                                }));

                        await context.PostAsync(incomingActivity.CreateReply(messageWithFooter));

                        context.Wait(MessageReceivedAsync);
                    }
                    else if (trimmedText.StartsWith("choice"))
                    {
                        var choice = trimmedText.Substring("choice".Length).Trim();
                        var choiceSegments = choice.Split(';').ToList();
                        var options = new[] { "option 1", "option 2", "option 3" };

                        var messageWithOptionsRequestData = new BotMessageWithOptionsRequest<UserBotRecipient>
                        {
                            Recipient = new UserBotRecipient { To = incomingActivity.From.Name },
                            MessageTitle = PopFrom(choiceSegments),
                            MessageBody = PopFrom(choiceSegments),
                            Options = options
                        };

                        // todo: split up recipients array and look them up in graph
                        var adaptiveCard = AdaptiveCardBuilder.BuildMessageWithOptionsRequestCard(
                            cultureInfo: CultureInfo.CurrentCulture,
                            choiceTitle: messageWithOptionsRequestData.MessageTitle,
                            choiceCreationDate: DateTime.Now,
                            requestor: new RequestIdentity { Name = messageWithOptionsRequestData.Recipient.To, Claims = new Dictionary<string, string> { { "upn", "courtneo@microsoft.com" } } },
                            choiceDetails: messageWithOptionsRequestData.MessageBody,
                            choiceOptions: messageWithOptionsRequestData.Options,
                            notificationUrl: null);

                        await context.PostAsync(incomingActivity.CreateReply("Your choice has been requested for the following item").ToBotActivity().WithAttachment(adaptiveCard).ToActivity());
                        context.Wait(MessageReceivedAsync);
                    }
                    else if (trimmedText.StartsWith("approval"))
                    {
                        var environment = Guid.NewGuid().ToString();
                        var approvalName = Guid.NewGuid().ToString();
                        var approvalLink = "http://linkToApproval/inFlowPortal.com";
                        var approval = trimmedText.Substring("approval".Length).Trim();
                        var approvalSegments = approval.Split(';').ToList();
                        var approvalOptions = new[] { "option 1", "option 2", "option 3" };

                        var approvalTitle = PopFrom(approvalSegments);
                        var approvalDetails = PopFrom(approvalSegments);
                        var approvalItemLinkDescription = PopFrom(approvalSegments);
                        var approvalItemLink = PopFrom(approvalSegments);

                        var approvalCreationDate = DateTime.Now.AddHours(-1);
                        var cultureInfo = CultureInfo.CurrentCulture;

                        var adaptiveCard = AdaptiveCardBuilder.BuildApprovalRequestCard(
                            cultureInfo: cultureInfo,
                            approvalTitle: approvalTitle,
                            approvalCreationDate: approvalCreationDate,
                            requestor: new RequestIdentity { Name = incomingActivity.From.Name, Claims = new Dictionary<string, string> { { "upn", "courtneo@microsoft.com" } } },
                            approvalDetails: approvalDetails,
                            environment: environment,
                            approvalLink: approvalLink,
                            approvalName: approvalName,
                            approvalOptions: approvalOptions,
                            itemLink: approvalItemLink,
                            itemLinkDescription: approvalItemLinkDescription,
                            onBehalfOfNotice: "The OnBehalfOf Notice!!!");

                        var replyActivity = incomingActivity.CreateReply("Your approval has been requested for the following item");
                        await context.PostAsync(replyActivity.ToBotActivity().WithAttachment(adaptiveCard).ToActivity());
                        context.Wait(MessageReceivedAsync);
                    }
                    else if (trimmedText.StartsWith("html"))
                    {
                        var replyActivity = incomingActivity.CreateReply("<b>This is bold</b>And this is not <a href=\"https://www.google.com\">link</a>");
                        replyActivity.TextFormat = "html";
                        await context.PostAsync(replyActivity);
                        context.Wait(MessageReceivedAsync);
                    }
                    else if (trimmedText.StartsWith("mention"))
                    {
                        var text = trimmedText.Substring("mention".Length).Trim();
                        var mentions = incomingActivity.GetMentions();
                        var replyActivity = incomingActivity.CreateReply(string.Format("Your text contained {0} mentions. Here is a mention for you: ", mentions.Length));

                        // (this is actually Vincent)
                        var mention = new ChannelAccount(
                            id: "29:1P42CnPU5FKEBUXSfFX0pQS-yvsggkTHkNkpfnMisIfnI1X84UJo25DoffCfECYCnJG6Q8TC6wEQC04W7G4fMSQ",
                            name: "Baz Bing");

                        replyActivity = replyActivity.AddMentionToText(mention, MentionTextLocation.AppendText, "Foo Bar");

                        var card = AdaptiveCardBuilder.BuildNotificationCard(
                            CultureInfo.CurrentCulture,
                            new RequestIdentity { Name = incomingActivity.From.Name, Claims = new Dictionary<string, string> { { "upn", "courtneo@microsoft.com" } } },
                            "Here's a notification",
                            "and this is it's body which contains this mention which won't work because mentions aren't supported in cards: <at>Foo Bar</at>. the end.");

                        await context.PostAsync(replyActivity.WithAttachment(card));
                        context.Wait(MessageReceivedAsync);
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

        private static string PopFrom(List<string> list)
        {
            var item = list.Count > 0 ? list[0].Trim() : "";
            list.RemoveAt(0);
            return item;
        }
    }
}