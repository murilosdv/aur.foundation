namespace Aurore.Foundation.Core.Constants;

/// <summary>
/// Names of the telemetry sources and attributes used across the foundation, so spans, logs and metrics agree on
/// spelling.
/// </summary>
public static class OpenTelemetryProperties
{
    /// <summary>
    /// Attribute names.
    /// </summary>
    public static class Attributes
    {
        /// <summary>
        /// Attribute names defined by the OpenTelemetry semantic conventions.
        /// </summary>
        public static class Standard
        {
            /// <summary>
            /// Attribute <c>deployment.environment.name</c>.
            /// </summary>
            public const string EnvironmentName = "deployment.environment.name";
            /// <summary>
            /// Attribute <c>service.instance.id</c>.
            /// </summary>
            public const string ServiceInstanceId = "service.instance.id";
            /// <summary>
            /// Attribute <c>service.name</c>.
            /// </summary>
            public const string ServiceName = "service.name";
            /// <summary>
            /// Attribute <c>service.version</c>.
            /// </summary>
            public const string ServiceVersion = "service.version";
            /// <summary>
            /// Attribute <c>http.request.method</c>.
            /// </summary>
            public const string HttpRequestMethod = "http.request.method";
            /// <summary>
            /// Attribute <c>server.address</c>.
            /// </summary>
            public const string ServerAddress = "server.address";
            /// <summary>
            /// Attribute <c>url.path</c>.
            /// </summary>
            public const string UrlPath = "url.path";
            /// <summary>
            /// Attribute <c>url.query</c>.
            /// </summary>
            public const string UrlQuery = "url.query";
            /// <summary>
            /// Attribute <c>client.address</c>.
            /// </summary>
            public const string ClientAddress = "client.address";
            /// <summary>
            /// Attribute <c>network.peer.address</c>.
            /// </summary>
            public const string NetworkPeerAddress = "network.peer.address";
            /// <summary>
            /// Attribute <c>enduser.id</c>.
            /// </summary>
            public const string EndUserId = "enduser.id";
            /// <summary>
            /// Attribute <c>user_agent.original</c>.
            /// </summary>
            public const string UserAgentOriginal = "user_agent.original";
        }
    }
}
