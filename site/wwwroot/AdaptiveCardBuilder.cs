namespace Microsoft.Azure.ProcessSimple.Data.Components.AdaptiveCards
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using global::AdaptiveCards;
    using BotActivity = Microsoft.Bot.Connector.Activity;
    using BotAttachment = Microsoft.Bot.Connector.Attachment;
    using AdaptiveFact = global::AdaptiveCards.Fact;
    using AdaptiveFactSet = global::AdaptiveCards.FactSet;
    using AdaptiveShowCardAction = global::AdaptiveCards.ShowCardAction;
    using AdaptiveSubmitAction = global::AdaptiveCards.SubmitAction;
    using AdaptiveTextBlock = global::AdaptiveCards.TextBlock;
    using AdaptiveTextInput = global::AdaptiveCards.TextInput;
    using AdaptiveTextSize = global::AdaptiveCards.TextSize;
    using AdaptiveTextWeight = global::AdaptiveCards.TextWeight;

    /// <summary>
    /// Class that builds adaptive cards.
    /// </summary>
    public static class AdaptiveCardBuilder
    {
        /// <summary>
        /// Builds a string containing markdown for a link with a title.
        /// </summary>
        private static string BuildMarkdownForLink(string linkTitle, string linkUrl)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "[{0}]({1})",
                string.IsNullOrWhiteSpace(linkTitle) ? linkUrl : linkTitle,
                linkUrl);
        }

        private static AdaptiveCard BuildEmptyCard()
        {
            // Teams currently requires version 1.0
            return new AdaptiveCard { Version = "1.0" };
        }

        private static AdaptiveCard WithTitle(this AdaptiveCard adaptiveCard, string title)
        {
            adaptiveCard.Body.Add(new AdaptiveTextBlock
            {
                Text = title,
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Bolder
            });

            return adaptiveCard;
        }

        private static AdaptiveCard WithText(this AdaptiveCard adaptiveCard, string text)
        {
            adaptiveCard.Body.Add(new AdaptiveTextBlock
            {
                Text = text,
                Wrap = true
            });

            return adaptiveCard;
        }

        private static AdaptiveCard WithResponseOptions(
            this AdaptiveCard adaptiveCard,
            AdaptiveOptionsResponseData optionChoiceData)
        {
            var responseOptions = optionChoiceData.ResponseOptions;

            foreach (var responseOption in responseOptions)
            {
                var adaptiveCardForOption = BuildEmptyCard()
                    .WithText("Comments");

                adaptiveCardForOption.Body.Add(new AdaptiveTextInput
                {
                    Id = "comments",
                    Placeholder = "Enter comments",
                    IsMultiline = true,
                    MaxLength = 1000
                });

                optionChoiceData.ResponseOption = responseOption;

                adaptiveCardForOption.Actions.Add(new AdaptiveSubmitAction
                {
                    Title = "Submit",
                    DataJson = optionChoiceData.Serialize()
                });

                adaptiveCard.Actions.Add(new AdaptiveShowCardAction
                {
                    Title = responseOption,
                    Card = adaptiveCardForOption
                });
            }

            return adaptiveCard;
        }

        /// <summary>
        /// Builds a notification adaptive card.
        /// </summary>
        /// <param name="cultureInfo">The culture info.</param>
        /// <param name="notificationTitle">Title of the notification.</param>
        /// <param name="notificationBody">Body of the notification.</param>
        /// <param name="notificationItemLinkTitle">Title of the item link in the notification.</param>
        /// <param name="notificationItemLinkUrl">Url of the item link in the notification.</param>
        public static AdaptiveCard BuildNotificationCard(
            CultureInfo cultureInfo,
            string notificationTitle,
            string notificationBody,
            string notificationItemLinkTitle,
            string notificationItemLinkUrl)
        {
            var adaptiveCard = BuildEmptyCard()
                .WithTitle(notificationTitle)
                .WithText(notificationBody);

            if (!string.IsNullOrEmpty(notificationItemLinkUrl))
            {
                adaptiveCard = adaptiveCard
                    .WithText(BuildMarkdownForLink(notificationItemLinkTitle, notificationItemLinkUrl));
            }

            return adaptiveCard;
        }

        /// <summary>
        /// Builds an adaptive card for an option choice.
        /// </summary>
        /// <param name="cultureInfo">The culture info.</param>
        /// <param name="optionResponseData">The option response data.</param>
        /// <param name="responderName">Name of the responder.</param>
        /// <param name="responseDate">Optional date of the response.</param>
        /// <param name="extraFact">An optional extra fact to add, if specified.</param>
        public static AdaptiveCard BuildOptionsResponseCard(
            CultureInfo cultureInfo,
            AdaptiveOptionsResponseData optionResponseData,
            string responderName = null,
            DateTime? responseDate = null,
            AdaptiveFact extraFact = null)
        {
            var adaptiveCard = BuildEmptyCard();

            adaptiveCard.Body.Add(new AdaptiveTextBlock
            {
                Text = string.Format(cultureInfo, "Response \"{0}\" recorded", optionResponseData.ResponseOption),
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Bolder
            });

            var factSet = new AdaptiveFactSet();

            if (responseDate != null)
            {
                factSet.Facts.Add(new AdaptiveFact
                {
                    Title = "On", // "Response date"?
                    Value = string.Format(cultureInfo, "{0:f} GMT", responseDate)
                });
            }

            if (responderName != null)
            {
                factSet.Facts.Add(new AdaptiveFact
                {
                    Title = "By",
                    Value = responderName
                });
            }

            factSet.Facts.Add(new AdaptiveFact
            {
                Title = "Comments",
                Value = optionResponseData.Comments
            });

            if (extraFact != null)
            {
                factSet.Facts.Add(extraFact);
            }

            adaptiveCard.Body.Add(factSet);
            return adaptiveCard;
        }

        /// <summary>
        /// Builds an adaptive card for an approval response.
        /// </summary>
        /// <param name="cultureInfo">The culture info.</param>
        /// <param name="responderName">Name of the responder.</param>
        /// <param name="responseDate">Date at which the response occurred.</param>
        /// <param name="approvalResponseData">The approval response data.</param>
        public static AdaptiveCard BuildApprovalResponseCard(
            CultureInfo cultureInfo,
            string responderName,
            DateTime responseDate,
            AdaptiveApprovalResponseData approvalResponseData)
        {
            var extraFact = string.IsNullOrEmpty(approvalResponseData.ApprovalLink)
                ? null
                : new AdaptiveFact
                    {
                        Title = "Approval",
                        Value = BuildMarkdownForLink(
                            approvalResponseData.ApprovalTitle,
                            approvalResponseData.ApprovalLink)
                    };

            return BuildOptionsResponseCard(cultureInfo, approvalResponseData, responderName, responseDate, extraFact);
        }

        /// <summary>
        /// Builds an adaptive card requesting that a choice be made among options.
        /// </summary>
        /// <param name="cultureInfo">The culture info.</param>
        /// <param name="choiceTitle">Title of the card.</param>
        /// <param name="choiceCreationDate">Date the choice request was created.</param>
        /// <param name="requestorName">Name of the creator of the choice request.</param>
        /// <param name="choiceDetails">Details of the choice request to be made.</param>
        /// <param name="choiceItemLinkDescription">Description of the item associated with the choice request.</param>
        /// <param name="choiceItemLink">Link to the item associated with the choice request.</param>
        /// <param name="choiceResponseData">Data describing the response options for the choice.</param>
        public static AdaptiveCard BuildOptionsRequestCard(
            CultureInfo cultureInfo,
            string choiceTitle,
            DateTime choiceCreationDate,
            string requestorName,
            string choiceDetails,
            string choiceItemLinkDescription,
            string choiceItemLink,
            AdaptiveOptionsResponseData choiceResponseData)
        {
            var adaptiveCard = BuildEmptyCard()
                .WithTitle(choiceTitle);

            var factSet = new AdaptiveFactSet();

            factSet.Facts.Add(new AdaptiveFact
            {
                Title = "Created",
                Value = string.Format(cultureInfo, "{0:f} GMT", choiceCreationDate)
            });

            factSet.Facts.Add(new AdaptiveFact
            {
                Title = "By",
                Value = requestorName
            });

            factSet.Facts.Add(new AdaptiveFact
            {
                Title = "Details",
                Value = choiceDetails
            });

            if (!string.IsNullOrEmpty(choiceItemLink))
            {
                factSet.Facts.Add(new AdaptiveFact
                {
                    Title = "Link",
                    Value = string.Format(
                        CultureInfo.InvariantCulture,
                        "[{0}]({1})",
                        string.IsNullOrWhiteSpace(choiceItemLinkDescription) ? choiceItemLink : choiceItemLinkDescription,
                        choiceItemLink)
                });
            }

            adaptiveCard.Body.Add(factSet);
            return adaptiveCard.WithResponseOptions(choiceResponseData);
        }

        /// <summary>
        /// Builds an adaptive card for an approval request.
        /// </summary>
        /// <param name="cultureInfo">The culture info.</param>
        /// <param name="approvalTitle">Title of the approval.</param>
        /// <param name="approvalCreationDate">Date the approval was created.</param>
        /// <param name="requestorName">Name of the creator of the approval.</param>
        /// <param name="approvalDetails">Details of the approval.</param>
        /// <param name="approvalItemLinkDescription">The item associated with the approval.</param>
        /// <param name="approvalItemLink">Link to the item associated with the approval.</param>
        /// <param name="environment">Environment containing the approval.</param>
        /// <param name="approvalLink">Link to the approval in Flow portal.</param>
        /// <param name="approvalName">Name of the approval.</param>
        /// <param name="approvalOptions">Array of options for the approval.</param>
        public static AdaptiveCard BuildApprovalRequestCard(
            CultureInfo cultureInfo,
            string approvalTitle,
            DateTime approvalCreationDate,
            string requestorName,
            string approvalDetails,
            string approvalItemLinkDescription,
            string approvalItemLink,
            string environment,
            string approvalLink,
            string approvalName,
            string[] approvalOptions)
        {
            var approvalResponseData = new AdaptiveApprovalResponseData
            {
                Environment = environment,
                ApprovalTitle = approvalTitle,
                ApprovalLink = approvalLink,
                ApprovalName = approvalName,
                ResponseOptions = approvalOptions
            };

            return BuildOptionsRequestCard(
                cultureInfo: cultureInfo,
                choiceTitle: approvalTitle,
                choiceCreationDate: approvalCreationDate,
                requestorName: requestorName,
                choiceDetails: approvalDetails,
                choiceItemLinkDescription: approvalItemLinkDescription,
                choiceItemLink: approvalItemLink,
                choiceResponseData: approvalResponseData);
        }

        /// <summary>
        /// Attach an adaptive card to the specified bot activity
        /// </summary>
        /// <param name="botActivity">The bot activity to attach the adaptive card to.</param>
        /// <param name="adaptiveCard">The adaptive card</param>
        /// <returns>The bot activity</returns>
        public static BotActivity WithAttachment(this BotActivity botActivity, AdaptiveCard adaptiveCard)
        {
            if (botActivity.Attachments == null)
            {
                botActivity.Attachments = new List<BotAttachment>();
            }

            botActivity.Attachments.Add(new BotAttachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = adaptiveCard
            });

            return botActivity;
        }
    }
}