using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Net.Http;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

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
            var message = await argument;
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
            else if (trimmedText == "increment")
            {
                await context.PostAsync($"{this.count++}: I say you said {message.Text}");
                context.Wait(MessageReceivedAsync);
            }
            else if(trimmedText.StartsWith("lookup"))
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