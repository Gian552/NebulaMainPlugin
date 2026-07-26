using Discord;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using LabApi.Features.Console;
using NebMainPluginLabApi;

#nullable enable

namespace NebMainPlugin.API
{
    public static class DiscordWebhookAPI
    {
        private static readonly HttpClient _client = new HttpClient();

        /// <summary>
        /// Send Message to specific webhook
        /// </summary>
        /// <param name="message"></param>
        public static async Task SendMs(string message)
        {
            string webhook = Main.Instance.WebHookLogs;

            if (!webhook.StartsWith("https") || webhook == "")
            {
                Logger.Error("Discord Webhook not valid!");
                return;
            }

            var content = new StringContent("{\"content\":\"" + message + "\"}", Encoding.UTF8, "application/json");

            try
            {
                await _client.PostAsync(webhook, content);
                return;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to send Webhook \n{ex.Message}");
                return;
            }
        }

        /// <summary>
        /// Send Message to custom webhook
        /// </summary>
        /// <param name="message"></param>
        /// <param name="webhook"></param>
        public static async void SendMs(string message, string webhook)
        {
            WebClient client = new WebClient();
            client.Headers.Add("Content-Type", "application/json");
            string payload = "{\"content\": \"" + message + "\"}";
            await Task.Run(() => client.UploadData(webhook, Encoding.UTF8.GetBytes(payload)));
        }

        /// <summary>
        /// Sends a Message to a custom webhook with embed
        /// </summary>
        /// <param name="content"></param>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public static async Task SendMs(string content, string title, string description, ConcurrentDictionary<string, string>? fields = null)
        {
            var _WebhookClient = new Discord.Webhook.DiscordWebhookClient(Main.Instance.TeamTimeControllWebhook);
            string webhook = Main.Instance.TeamTimeControllWebhook;
            if (!webhook.StartsWith("https") || String.IsNullOrEmpty(webhook))
            {
                Logger.Error("Discord Webhook not valid!");
                return;
            }

            var _footer = new EmbedFooterBuilder()
                .WithText($"Made By @skorp1.0 • {DateTime.Now}")
                .WithIconUrl("https://cdn.discordapp.com/avatars/504875989776596992/8542a836150144bf7db92fea8f7a886c.png?size=1024");

            var _embed = new EmbedBuilder()
                .WithColor(new Color(45, 45, 255))
                .WithFields()
                .WithFooter(_footer)
                .WithTitle(title)
                .WithDescription(description);

            if (fields != null && fields.Count > 0)
            {
                foreach (var field in fields)
                {
                    var inline = field.Value.Length <= 1024;
                    _embed.AddField(new EmbedFieldBuilder()
                        .WithName(field.Key)
                        .WithValue(field.Value.Length > 1024 ? field.Value.Substring(0, 1021) + "..." : field.Value)
                        .WithIsInline(inline));
                }
            }

            try
            {
                await _WebhookClient.SendMessageAsync(embeds: new[] { _embed.Build() });
                return;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to send Webhook \n{ex.Message}");
                SendMs("Ja der Time Report hat sich eingeschissen :3 \nMach mal was" ,webhook);
                return;
            }
        }
    }
}