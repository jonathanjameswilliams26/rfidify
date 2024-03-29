﻿namespace RFIDify.Spotify.Apis.DelegatingHandlers;

public class LoggingDelegatingHandler(ILogger<LoggingDelegatingHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending HTTP Request {Method} {Uri}", request.Method, request.RequestUri);
        var response = await base.SendAsync(request, cancellationToken);
        logger.LogInformation("Received response {StatusCode} from {Method} {Uri}", response.StatusCode, request.Method, request.RequestUri);
        return response;
    }
}