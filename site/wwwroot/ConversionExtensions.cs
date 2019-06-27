// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

namespace SimpleEchoBot
{
    extern alias FlowCommon;
    extern alias FlowData;
    extern alias FlowWeb;
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Builder.Dialogs;
    using global::AdaptiveCards;
    using FlowData::Microsoft.Azure.ProcessSimple.Data.Entities;
    using System.Linq;

    public static class ConversionExtensions
    {
        public static BotActivity ToBotActivity(this Activity activity)
        {
            return new BotActivity
            {
                Type = activity.Type,
                Id = activity.Id,
                Timestamp = activity.Timestamp.HasValue ? activity.Timestamp.Value.UtcDateTime : (DateTime?)null,
                ServiceUrl = activity.ServiceUrl,
                ChannelId = activity.ChannelId,
                ChannelData = activity.ChannelData.ToJObject(),
                From = activity.From.ToBotChannelAccount(),
                Conversation = activity.Conversation.ToBotConversationAccount(),
                Entities = activity.Entities.ToBotActivityEntityList(),
                Recipient = activity.Recipient.ToBotChannelAccount(),
                TextFormat = activity.TextFormat,
                TopicName = activity.TopicName,
                Locale = activity.Locale,
                Text = activity.Text,
                Summary = activity.Summary,
                ReplyToId = activity.ReplyToId,
                Attachments = activity.Attachments.ToBotAttachmentsList(),
                Value = activity.Value
            };
        }

        public static Activity ToActivity(this BotActivity botActivity)
        {
            return new Activity
            {
                Type = botActivity.Type,
                Id = botActivity.Id,
                Timestamp = botActivity.Timestamp.HasValue ? botActivity.Timestamp.Value : (DateTime?)null,
                ServiceUrl = botActivity.ServiceUrl,
                ChannelId = botActivity.ChannelId,
                ChannelData = botActivity.ChannelData.ToObject(),
                From = botActivity.From.ToChannelAccount(),
                Conversation = botActivity.Conversation.ToConversationAccount(),
                Entities = botActivity.Entities.ToEntityList(),
                Recipient = botActivity.Recipient.ToChannelAccount(),
                TextFormat = botActivity.TextFormat,
                TopicName = botActivity.TopicName,
                Locale = botActivity.Locale,
                Text = botActivity.Text,
                Summary = botActivity.Summary,
                ReplyToId = botActivity.ReplyToId,
                Attachments = botActivity.Attachments.ToAttachmentsList(),
                Value = botActivity.Value
            };
        }

        public static List<BotActivityEntity> ToBotActivityEntityList(this IEnumerable<Entity> entities)
        {
            return entities == null
                ? null
                : entities
                    .Select(e => e is Mention
                        ? (BotActivityEntity)e.GetAs<Mention>().ToBotActivityMentionEntity()
                        : (BotActivityEntity)e.GetAs<ClientInfo>().ToBotActivityClientInfoEntity())
                    .ToList();
        }

        public static List<Entity> ToEntityList(this IEnumerable<BotActivityEntity> entities)
        {
            return entities == null
                ? null
                : entities
                    .Select(e => e is BotActivityMentionEntity
                        ? (Entity)(e as BotActivityMentionEntity).ToMention()
                        : (Entity)(e as BotActivityClientInfoEntity).ToClientInfo())
                    .ToList();
        }

        public static BotActivityClientInfoEntity ToBotActivityClientInfoEntity(this ClientInfo clientInfo)
        {
            return new BotActivityClientInfoEntity
            {
                Locale = clientInfo.Locale,
                Country = clientInfo.Country,
                Platform = clientInfo.Platform
            };
        }

        public static ClientInfo ToClientInfo(this BotActivityClientInfoEntity clientInfo)
        {
            return new ClientInfo
            {
                Locale = clientInfo.Locale,
                Country = clientInfo.Country,
                Platform = clientInfo.Platform
            };
        }

        public static BotActivityMentionEntity ToBotActivityMentionEntity(this Mention mention)
        {
            return new BotActivityMentionEntity
            {
                Type = mention.Type,
                Text = mention.Text,
                Mentioned = new BotChannelAccount
                {
                    Id = mention.Mentioned.Id,
                    Name = mention.Mentioned.Name
                }
            };
        }

        public static Mention ToMention(this BotActivityMentionEntity mention)
        {
            return new Mention
            {
                Type = mention.Type,
                Text = mention.Text,
                Mentioned = new ChannelAccount
                {
                    Id = mention.Mentioned.Id,
                    Name = mention.Mentioned.Name
                }
            };
        }

        public static JObject ToJObject(this object instance)
        {
            return instance == null ? (JObject)null : JObject.FromObject(instance);
        }

        public static object ToObject(this JObject jObject)
        {
            return jObject == null ? null : jObject.ToObject<object>();
        }

        public static List<BotAttachment> ToBotAttachmentsList(this IEnumerable<Attachment> attachments)
        {
            return attachments == null
                ? null
                : attachments.Select(a => new BotAttachment { Content = a.Content, ContentType = a.ContentType }).ToList();
        }

        public static List<Attachment> ToAttachmentsList(this IEnumerable<BotAttachment> botAttachments)
        {
            return botAttachments == null
                ? null
                : botAttachments.Select(a => new Attachment { Content = a.Content, ContentType = a.ContentType }).ToList();
        }

        public static BotChannelAccount ToBotChannelAccount(this ChannelAccount channelAccount)
        {
            return channelAccount == null
                ? null
                : new BotChannelAccount { Id = channelAccount.Id, Name = channelAccount.Name };
        }

        public static ChannelAccount ToChannelAccount(this BotChannelAccount botChannelAccount)
        {
            return botChannelAccount == null
                ? null
                : new ChannelAccount { Id = botChannelAccount.Id, Name = botChannelAccount.Name };
        }

        public static BotConversationAccount ToBotConversationAccount(this ConversationAccount conversationAccount)
        {
            return conversationAccount == null
                ? null
                : new BotConversationAccount { Id = conversationAccount.Id, Name = conversationAccount.Name, IsGroup = conversationAccount.IsGroup };
        }

        public static ConversationAccount ToConversationAccount(this BotConversationAccount botConversationAccount)
        {
            return botConversationAccount == null
                ? null
                : new ConversationAccount { Id = botConversationAccount.Id, Name = botConversationAccount.Name, IsGroup = botConversationAccount.IsGroup };
        }

        public static JToken[] ToJTokenArray(this IEnumerable<Entity> entities)
        {
            return entities == null
                ? null
                : entities.Select(e => e == null ? null : e.GetAs<JToken>()).ToArray();
        }

        public static List<Entity> ToEntityList(this IEnumerable<JToken> jTokens)
        {
            if(jTokens == null)
            {
                return null;
            }

            var list = new List<Entity>();

            foreach(var token in jTokens)
            {
                var entity = new Entity();
                entity.SetAs<JToken>(token);
                list.Add(entity);
            }

            return list;
        }

        /// <summary>
        /// Attach an adaptive card to the specified activity
        /// </summary>
        /// <param name="activity">The activity to attach the adaptive card to.</param>
        /// <param name="adaptiveCard">The adaptive card</param>
        /// <returns>The bot activity</returns>
        public static Activity WithAttachment(this Activity activity, AdaptiveCard adaptiveCard)
        {
            if (activity.Attachments == null)
            {
                activity.Attachments = new List<Attachment>();
            }

            activity.Attachments.Add(new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = adaptiveCard
            });

            return activity;
        }
    }

    public class ClientInfo : Entity
    {
        /// <summary>
        /// Gets or sets the Locale.
        /// </summary>
        public string Locale { get; set; }

        /// <summary>
        /// Gets or sets the Country.
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// Gets or sets the Platform.
        /// </summary>
        public string Platform { get; set; }
    }
}