extern alias FlowData;
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
using FlowData::Microsoft.Azure.ProcessSimple.Data.Entities;
using FlowData::Microsoft.Azure.ProcessSimple.Data.Components.AdaptiveCards;
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
                var teamsFlowbotManager = this.GetTeamsFlowbotManager(context, incomingActivity);
                LastSeenActivity = incomingActivity;

                if (message.Text == null)
                {
                    var adaptiveActionData = AdaptiveActionData.Deserialize(incomingActivity.Value.ToString());
                    await teamsFlowbotManager.ReceiveAdaptiveAction(adaptiveActionData, incomingActivity.From.ToBotChannelAccount(), incomingActivity.From.AadObjectId);
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

                        var notificationRequestData = new NotificationRequest
                        {
                            Title = PopFrom(notificationSegments),
                            Details = PopFrom(notificationSegments),
                            ItemLinkDescription = PopFrom(notificationSegments),
                            ItemLink = PopFrom(notificationSegments)
                        };

                        await teamsFlowbotManager.SendNotification(notificationRequestData);
                        context.Wait(MessageReceivedAsync);
                    }
                    else if (trimmedText.StartsWith("choice"))
                    {
                        var choice = trimmedText.Substring("choice".Length).Trim();
                        var choiceSegments = choice.Split(';').ToList();
                        var options = new[] { "option 1", "option 2", "option 3" };

                        var optionsRequestData = new MessageWithOptionsRequest
                        {
                            Title = PopFrom(choiceSegments),
                            Recipients = incomingActivity.From.Name,
                            Details = PopFrom(choiceSegments),
                            ItemLinkDescription = PopFrom(choiceSegments),
                            ItemLink = PopFrom(choiceSegments),
                            Options = options
                        };

                        await teamsFlowbotManager.SendMessageWithOptions(optionsRequestData);
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
                            requestorName: incomingActivity.From.Name,
                            approvalDetails: approvalDetails,
                            approvalItemLinkDescription: approvalItemLinkDescription,
                            approvalItemLink: approvalItemLink,
                            environment: environment,
                            approvalLink: approvalLink,
                            approvalName: approvalName,
                            approvalOptions: approvalOptions);

                        await teamsFlowbotManager.SendAdaptiveCard(adaptiveCard, "Your approval has been requested for the following item");
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