using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace QuestEditor_Library
{
    public sealed class CQFDialogAIClient
    {
        public CQFDialogAIClient(string endpoint, string model, string apiKey, int timeoutSeconds)
        {
            string address = (endpoint ?? string.Empty).Trim().TrimEnd('/');
            if (!address.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            {
                address += "/chat/completions";
            }
            if (!Uri.TryCreate(address, UriKind.Absolute, out Uri uri)
                || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
                || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            {
                throw new ArgumentException("CQF_DialogAI_InvalidEndpoint");
            }
            if (string.IsNullOrWhiteSpace(model))
            {
                throw new ArgumentException("CQF_DialogAI_MissingModel");
            }
            this.endpoint = uri;
            this.model = model.Trim();
            this.apiKey = (apiKey ?? string.Empty).Trim();
            this.timeoutSeconds = Math.Max(10, Math.Min(600, timeoutSeconds));
        }

        public CQFAITokenUsage? LastUsage { get; private set; }
        public bool ReceivedResponse { get; private set; }
        public string? UsageError { get; private set; }

        public async Task<string> CompleteAsync(string instructions, string command, CancellationToken cancellation)
        {
            return await CompleteConversationAsync(instructions, new[] { new CQFAIMessage("user", command) }, cancellation).ConfigureAwait(false);
        }

        public async Task<string> CompleteConversationAsync(string instructions, IEnumerable<CQFAIMessage> messages, CancellationToken cancellation, IEnumerable<CQFAITool>? tools = null)
        {
            LastUsage = null; UsageError = null; ReceivedResponse = false;
            CQFAITool[] definitions = tools?.ToArray() ?? Array.Empty<CQFAITool>();
            bool nativeTools = definitions.Length > 0;
            if (nativeTools) instructions += "\nNative function tools are enabled. Request operations using tool_calls, not XML tool/query/change sections. Return conversational replies as plain text. All function arguments are strings.\n";
            XElement payload = new XElement("root", new XAttribute("type", "object"),
                new XElement("model", new XAttribute("type", "string"), this.model),
                new XElement("stream", new XAttribute("type", "boolean"), "false"),
                new XElement("messages", new XAttribute("type", "array"),
                    new XElement("item", new XAttribute("type", "object"),
                        new XElement("role", new XAttribute("type", "string"), "system"),
                        new XElement("content", new XAttribute("type", "string"), instructions)),
                    messages.Select(message => MessageJson(message, nativeTools))));
            if (nativeTools)
            {
                payload.Add(new XElement("tools", new XAttribute("type", "array"), definitions.Select(tool => tool.JsonDefinition)));
                payload.Add(new XElement("tool_choice", new XAttribute("type", "string"), "auto"));
            }
            string json = CQFAIJson.Write(payload);
            using (HttpClientHandler handler = new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
            using (HttpClient client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(this.timeoutSeconds), MaxResponseContentBufferSize = 2097152 })
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, this.endpoint))
            {
                if (this.apiKey.Length > 0)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.apiKey);
                }
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                using (HttpResponseMessage response = await client.SendAsync(request, cancellation).ConfigureAwait(false))
                {
                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException("CQF_DialogAI_HTTP " + (int)response.StatusCode);
                    }
                    ReceivedResponse = true;
                    byte[] bytes = Encoding.UTF8.GetBytes(body);
                    XmlDictionaryReaderQuotas quotas = new XmlDictionaryReaderQuotas { MaxStringContentLength = 2097152, MaxArrayLength = 2097152, MaxDepth = 64 };
                    using (XmlDictionaryReader reader = JsonReaderWriterFactory.CreateJsonReader(bytes, quotas))
                    {
                        XElement result = XElement.Load(reader);
                        try { LastUsage = CQFAITokenUsage.Read(result.Element("usage")); }
                        catch (InvalidDataException error) { UsageError = error.Message; }
                        XElement? choice = result.Element("choices")?.Element("item");
                        string? finish = choice?.Element("finish_reason")?.Value;
                        if (finish == "length" || finish == "content_filter")
                        {
                            throw new InvalidOperationException("CQF_DialogAI_Incomplete");
                        }
                        XElement? message = choice?.Element("message");
                        string? text = message?.Element("content")?.Value;
                        XElement? calls = message?.Element("tool_calls");
                        if (nativeTools && calls?.HasElements == true)
                        {
                            if (calls.Elements().Count() > 8) throw new InvalidDataException("CQF_AI_InvalidTool");
                            return new XElement("assistant", string.IsNullOrWhiteSpace(text) ? null : new XElement("reply", text),
                                new XElement("tools", calls.Elements().Select(call => CQFAIToolCall.FromJson(call).ToXml()))).ToString(SaveOptions.DisableFormatting);
                        }
                        if (string.IsNullOrWhiteSpace(text))
                        {
                            throw new InvalidDataException("CQF_DialogAI_EmptyResponse");
                        }
                        return nativeTools && !text!.TrimStart().StartsWith("<assistant", StringComparison.Ordinal) && !text.TrimStart().StartsWith("```", StringComparison.Ordinal)
                            ? new XElement("assistant", new XElement("reply", text)).ToString(SaveOptions.DisableFormatting) : text!;
                    }
                }
            }
        }

        private static XElement MessageJson(CQFAIMessage message, bool nativeTools)
        {
            XElement result = new XElement("item", new XAttribute("type", "object"),
                new XElement("role", new XAttribute("type", "string"), message.Role == "tool" && !nativeTools ? "system" : message.Role),
                new XElement("content", new XAttribute("type", "string"), nativeTools && message.ToolCalls.Count > 0 ? message.DisplayContent : message.Content));
            if (nativeTools && message.ToolCalls.Count > 0) result.Add(new XElement("tool_calls", new XAttribute("type", "array"), message.ToolCalls.Select(call => call.ToJson())));
            if (nativeTools && message.Role == "tool") result.Add(new XElement("tool_call_id", new XAttribute("type", "string"), message.ToolCallId ?? throw new InvalidDataException("CQF_AI_InvalidTool")));
            return result;
        }

        private readonly Uri endpoint;
        private readonly string model;
        private readonly string apiKey;
        private readonly int timeoutSeconds;
    }
}
