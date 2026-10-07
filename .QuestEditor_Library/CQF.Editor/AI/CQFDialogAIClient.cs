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
        public string? TransportNotice { get; private set; }
        public CQFAIStreamUpdate? LastProgress => Volatile.Read(ref progress);

        public async Task<string> CompleteAsync(string instructions, string command, CancellationToken cancellation)
        {
            return await CompleteConversationAsync(instructions, new[] { new CQFAIMessage("user", command) }, cancellation).ConfigureAwait(false);
        }

        public async Task<string> CompleteConversationAsync(string instructions, IEnumerable<CQFAIMessage> messages, CancellationToken cancellation, IEnumerable<CQFAITool>? tools = null, bool stream = false)
        {
            LastUsage = null; UsageError = null; ReceivedResponse = false;
            Volatile.Write(ref progress, null);
            stream &= streamingSupported;
            CQFAITool[] definitions = tools?.ToArray() ?? Array.Empty<CQFAITool>();
            bool nativeTools = definitions.Length > 0;
            if (nativeTools) instructions += "\nNative function tools are enabled. Request operations using tool_calls, not XML tool/query/change sections. Return conversational replies as plain text. All function arguments are strings.\n";
            XElement payload = new XElement("root", new XAttribute("type", "object"),
                new XElement("model", new XAttribute("type", "string"), this.model),
                new XElement("stream", new XAttribute("type", "boolean"), stream ? "true" : "false"),
                new XElement("messages", new XAttribute("type", "array"),
                    new XElement("item", new XAttribute("type", "object"),
                        new XElement("role", new XAttribute("type", "string"), "system"),
                        new XElement("content", new XAttribute("type", "string"), instructions)),
                    messages.Select(message => MessageJson(message, nativeTools))));
            if (stream && streamingUsageSupported) payload.Add(new XElement("stream_options", new XAttribute("type", "object"), new XElement("include_usage", new XAttribute("type", "boolean"), "true")));
            if (nativeTools)
            {
                payload.Add(new XElement("tools", new XAttribute("type", "array"), definitions.Select(tool => tool.JsonDefinition)));
                payload.Add(new XElement("tool_choice", new XAttribute("type", "string"), "auto"));
            }
            long contextLength = payload.Descendants().Where(element => element.Attribute("type")?.Value == "string").Sum(element => (long)element.Value.Length);
            if (contextLength > 600000) throw new InvalidDataException("CQF_AI_RequestTooLarge: " + contextLength);
            using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            deadline.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            using HttpClientHandler handler = new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false };
            using HttpClient client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan, MaxResponseContentBufferSize = 2097152 };
            try
            {
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    deadline.Token.ThrowIfCancellationRequested();
                    deadline.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
                    using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                    if (apiKey.Length > 0) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    request.Content = new StringContent(CQFAIJson.Write(payload), Encoding.UTF8, "application/json");
                    using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false);
                    deadline.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
                    if (!response.IsSuccessStatusCode)
                    {
                        string body = await ReadBodyAsync(response, deadline).ConfigureAwait(false);
                        if ((int)response.StatusCode is 400 or 422 && payload.Element("stream_options") != null
                            && body.IndexOf("stream_options", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            payload.Element("stream_options")!.Remove();
                            streamingUsageSupported = false;
                            TransportNotice = "CQF_AI_StreamUsageFallback";
                            continue;
                        }
                        if ((int)response.StatusCode is 400 or 422 && payload.Element("stream")?.Value == "true"
                            && body.IndexOf("stream", StringComparison.OrdinalIgnoreCase) >= 0
                            && (body.IndexOf("unsupported", StringComparison.OrdinalIgnoreCase) >= 0 || body.IndexOf("not support", StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            payload.Element("stream")!.Value = "false";
                            payload.Element("stream_options")?.Remove();
                            streamingSupported = false;
                            TransportNotice = "CQF_AI_StreamFallback";
                            continue;
                        }
                        throw new InvalidOperationException("CQF_DialogAI_HTTP " + (int)response.StatusCode);
                    }
                    ReceivedResponse = true;
                    XElement result;
                    if (response.Content.Headers.ContentType?.MediaType?.Equals("text/event-stream", StringComparison.OrdinalIgnoreCase) == true)
                        result = await ReadStreamAsync(response, deadline).ConfigureAwait(false);
                    else
                    {
                        result = CQFAIJson.Read(await ReadBodyAsync(response, deadline).ConfigureAwait(false));
                        ReadUsage(result.Element("usage"));
                        XElement? message = result.Element("choices")?.Element("item")?.Element("message");
                        string thought = message?.Element("reasoning_summary")?.Value ?? message?.Element("reasoning_content")?.Value ?? message?.Element("reasoning")?.Value ?? string.Empty;
                        Volatile.Write(ref progress, new CQFAIStreamUpdate(message?.Element("content")?.Value ?? string.Empty, thought,
                            message?.Element("tool_calls")?.Elements().Select(call => call.Element("function")?.Element("name")?.Value ?? string.Empty).ToArray() ?? Array.Empty<string>()));
                    }
                    string? finish = result.Element("choices")?.Element("item")?.Element("finish_reason")?.Value;
                    if (finish == "length" || finish == "content_filter") throw new InvalidOperationException("CQF_DialogAI_Incomplete");
                    XElement? answer = result.Element("choices")?.Element("item")?.Element("message");
                    string? text = answer?.Element("content")?.Value;
                    XElement? calls = answer?.Element("tool_calls");
                    if (nativeTools && calls?.HasElements == true)
                    {
                        if (calls.Elements().Count() > 8) throw new InvalidDataException("CQF_AI_InvalidTool");
                        return new XElement("assistant", string.IsNullOrWhiteSpace(text) ? null : new XElement("reply", text),
                            new XElement("tools", calls.Elements().Select(call => CQFAIToolCall.FromJson(call).ToXml()))).ToString(SaveOptions.DisableFormatting);
                    }
                    if (string.IsNullOrWhiteSpace(text)) throw new InvalidDataException("CQF_DialogAI_EmptyResponse");
                    return nativeTools && !text!.TrimStart().StartsWith("<assistant", StringComparison.Ordinal) && !text.TrimStart().StartsWith("```", StringComparison.Ordinal)
                        ? new XElement("assistant", new XElement("reply", text)).ToString(SaveOptions.DisableFormatting) : text!;
                }
                throw new InvalidOperationException("CQF_AI_StreamError");
            }
            catch (Exception) when (deadline.IsCancellationRequested) { throw new OperationCanceledException(deadline.Token); }
        }

        private async Task<XElement> ReadStreamAsync(HttpResponseMessage response, CancellationTokenSource deadline)
        {
            CancellationToken cancellation = deadline.Token;
            CQFAIStreamResponse parser = new CQFAIStreamResponse(value => Volatile.Write(ref progress, value));
            using Stream input = new CQFAIResponseStream(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), deadline, TimeSpan.FromSeconds(timeoutSeconds));
            using CancellationTokenRegistration registration = cancellation.Register(() => input.Dispose());
            using StreamReader reader = new StreamReader(input, Encoding.UTF8);
            StringBuilder data = new StringBuilder();
            int length = 0;
            try
            {
                while (!parser.Done)
                {
                    cancellation.ThrowIfCancellationRequested();
                    string? line = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (line == null) break;
                    length += line.Length;
                    if (length > 2097152) throw new InvalidDataException("CQF_AI_ResponseTooLarge");
                    if (line.Length == 0)
                    {
                        if (data.Length == 0) continue;
                        parser.Append(data.ToString());
                        data.Clear();
                    }
                    else if (line.StartsWith("data:", StringComparison.Ordinal))
                    {
                        if (data.Length > 0) data.Append('\n');
                        data.Append(line.Substring(5).TrimStart(' '));
                    }
                }
                if (!parser.Done && data.Length > 0) parser.Append(data.ToString());
                cancellation.ThrowIfCancellationRequested();
                return parser.Complete();
            }
            finally { ReadUsage(parser.Usage); }
        }

        private async Task<string> ReadBodyAsync(HttpResponseMessage response, CancellationTokenSource deadline)
        {
            CancellationToken cancellation = deadline.Token;
            using Stream input = new CQFAIResponseStream(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), deadline, TimeSpan.FromSeconds(timeoutSeconds));
            using CancellationTokenRegistration registration = cancellation.Register(() => input.Dispose());
            using StreamReader reader = new StreamReader(input, Encoding.UTF8);
            StringBuilder text = new StringBuilder();
            char[] buffer = new char[4096];
            while (true)
            {
                cancellation.ThrowIfCancellationRequested();
                int count = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                if (count == 0) break;
                if (text.Length + count > 2097152) throw new InvalidDataException("CQF_AI_ResponseTooLarge");
                text.Append(buffer, 0, count);
            }
            cancellation.ThrowIfCancellationRequested();
            return text.ToString();
        }

        private void ReadUsage(XElement? usage)
        {
            try { LastUsage = CQFAITokenUsage.Read(usage); }
            catch (InvalidDataException error) { LastUsage = null; UsageError = error.Message; }
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

        private CQFAIStreamUpdate? progress;
        private bool streamingSupported = true;
        private bool streamingUsageSupported = true;
        private readonly Uri endpoint;
        private readonly string model;
        private readonly string apiKey;
        private readonly int timeoutSeconds;
    }
}
