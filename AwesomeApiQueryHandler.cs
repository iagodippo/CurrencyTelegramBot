using Microsoft.Extensions.Options;

namespace CurrencyTelegramBot;

public class AwesomeApiQueryHandler(IOptions<AwesomeApiOptions> options) : DelegatingHandler
{
    private readonly AwesomeApiOptions _options = options.Value;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uriBuilder = new UriBuilder(request.RequestUri!);
        var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
        
        // Adiciona parâmetros fixos
        query["token"] = _options.Token; // ou outro query param fixo
        uriBuilder.Query = query.ToString();
        request.RequestUri = uriBuilder.Uri;

        return base.SendAsync(request, cancellationToken);
    }
}