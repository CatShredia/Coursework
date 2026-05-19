using System.Globalization;
using System.Net.Http.Headers;

namespace CatshrediasNews.Client.Services;

public class CultureHttpHandler : DelegatingHandler
{
    private string _culture = CultureService.DefaultCulture;

    public void SetCulture(string culture) =>
        _culture = culture == CultureService.EnglishCulture
            ? CultureService.EnglishCulture
            : CultureService.DefaultCulture;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.AcceptLanguage.Clear();
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(
            _culture,
            1.0));
        return base.SendAsync(request, cancellationToken);
    }
}
