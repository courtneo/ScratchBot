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

        private static AdaptiveCard WithText(
            this AdaptiveCard adaptiveCard,
            string text,
            AdaptiveTextSize textSize = default(AdaptiveTextSize),
            AdaptiveTextWeight textWeight = default(AdaptiveTextWeight),
            bool wrap = true)
        {
            adaptiveCard.Body.Add(new AdaptiveTextBlock
            {
                Text = text,
                Wrap = wrap,
                Size = textSize,
                Weight = textWeight
            });

            return adaptiveCard;
        }

        private static AdaptiveCard WithFactset(this AdaptiveCard adaptiveCard, AdaptiveFactSet adaptiveFactSet)
        {
            adaptiveCard.Body.Add(adaptiveFactSet);
            return adaptiveCard;
        }

        private static AdaptiveCard WithFact(this AdaptiveCard adaptiveCard, string factTitle, string factValue)
        {
            var adaptiveFactSet = adaptiveCard.Body.FindLast(cardElement => cardElement.GetType() == typeof(AdaptiveFactSet)) as AdaptiveFactSet;

            if (adaptiveFactSet == null)
            {
                adaptiveFactSet = new AdaptiveFactSet();
                adaptiveCard.Body.Add(adaptiveFactSet);
            }

            adaptiveFactSet.Facts.Add(new AdaptiveFact
            {
                Title = factTitle,
                Value = factValue
            });

            return adaptiveCard;
        }

        private static AdaptiveCard WithTextInput(this AdaptiveCard adaptiveCard, string id, string placeholder, bool isMultiline, int maxLength)
        {
            adaptiveCard.Body.Add(new AdaptiveTextInput
            {
                Id = id,
                Placeholder = placeholder,
                IsMultiline = isMultiline,
                MaxLength = maxLength
            });

            return adaptiveCard;
        }

        private static AdaptiveCard WithSubmitAction(this AdaptiveCard adaptiveCard, string title, string dataJson)
        {
            adaptiveCard.Actions.Add(new AdaptiveSubmitAction
            {
                Title = title,
                DataJson = dataJson
            });

            return adaptiveCard;
        }

        private static AdaptiveCard WithShowCardAction(this AdaptiveCard adaptiveCard, string title, AdaptiveCard adaptiveCardToShow)
        {
            adaptiveCard.Actions.Add(new AdaptiveShowCardAction
            {
                Title = title,
                Card = adaptiveCardToShow
            });

            return adaptiveCard;
        }

        private static AdaptiveCard WithResponseOptions(
            this AdaptiveCard adaptiveCard,
            AdaptiveOptionsResponseData optionChoiceData)
        {
            var options = optionChoiceData.Options;

            foreach (var option in options)
            {
                optionChoiceData.SelectedOption = option;

                adaptiveCard = adaptiveCard.WithShowCardAction(
                    title: option,
                    adaptiveCardToShow: BuildEmptyCard()
                        .WithText("Comments")
                        .WithTextInput(
                            id: "comments",
                            placeholder: "Enter comments",
                            isMultiline: true,
                            maxLength: 1000)
                        .WithSubmitAction(title: "Submit", dataJson: optionChoiceData.Serialize()));
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
        public static AdaptiveCard BuildOptionsResponseCard(
            CultureInfo cultureInfo,
            AdaptiveOptionsResponseData optionResponseData,
            string responderName = null,
            DateTime? responseDate = null)
        {
            return BuildEmptyCard()
                .WithText(
                    text: string.Format(cultureInfo, "Response \"{0}\" recorded", optionResponseData.SelectedOption),
                    textSize: AdaptiveTextSize.Large,
                    textWeight: AdaptiveTextWeight.Bolder)
                .WithFact(factTitle: "On", factValue: string.Format(cultureInfo, "{0:f} GMT", responseDate.Value.ToUniversalTime())) // .. factTitle "Response date"?
                .WithFact(factTitle: "By", factValue: responderName)
                .WithFact(factTitle: "Comments", factValue: optionResponseData.Comments);
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
            var adaptiveCard = BuildOptionsResponseCard(cultureInfo, approvalResponseData, responderName, responseDate);

            return string.IsNullOrEmpty(approvalResponseData.ApprovalLink)
                ? adaptiveCard
                : adaptiveCard
                    .WithFact(
                        factTitle: "Approval",
                        factValue: BuildMarkdownForLink(
                            approvalResponseData.ApprovalTitle,
                            approvalResponseData.ApprovalLink));
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
            return BuildEmptyCard()
                .WithTitle(choiceTitle)
                .WithFact(factTitle: "Created", factValue: string.Format(cultureInfo, "{0:f} GMT", choiceCreationDate.ToUniversalTime()))
                .WithFact(factTitle: "By", factValue: requestorName)
                .WithFact(factTitle: "Details", factValue: choiceDetails)
                .WithFact(factTitle: "Link", factValue: BuildMarkdownForLink(linkTitle: choiceItemLinkDescription, linkUrl: choiceItemLink))
                .WithResponseOptions(choiceResponseData);
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
                Options = approvalOptions
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