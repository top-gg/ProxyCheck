using System.Net;
using NSubstitute;
using Topgg.ProxyCheck;

namespace ProxyCheckUtil.Tests;

public class DeserializeTest
{
    [Fact]
    public async Task TestSuccess()
    {
        const string json =
            """
            {
                "status": "ok",
                "104.16.255.200": {
                    "asn": "AS13335",
                    "range": "104.16.0.0/16",
                    "provider": "CLOUDFLARENET - Cloudflare, Inc., US",
                    "organisation": "Cloudflare, Inc.",
                    "continent": "North America",
                    "continentcode": "NA",
                    "country": "Canada",
                    "isocode": "CA",
                    "region": "Ontario",
                    "regioncode": "ON",
                    "timezone": "America/Toronto",
                    "city": "Toronto",
                    "postcode": "M5A",
                    "latitude": 43.6532,
                    "longitude": -79.3832,
                    "currency": {
                        "code": "CAD",
                        "name": "Dollar",
                        "symbol": "CA$"
                    },
                    "devices": {
                        "address": 0,
                        "subnet": 0
                    },
                    "proxy": "no",
                    "type": "Business"
                }
            }
            """;

        // Arrange
        var handler = Substitute.For<HttpMessageHandler>();
        handler
            .SetupRequest(HttpMethod.Post, "https://proxycheck.io/v2/&vpn=0&asn=0&node=0&time=0&inf=1&port=0&seen=0&days=7&risk=0")
            .ReturnsResponse(HttpStatusCode.OK, json);

        using var client = new HttpClient(handler);

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient().Returns(client);

        var proxyCheck = new ProxyCheck(httpClientFactory)
        {
            UseTLS = true
        };

        // Act
        var result = await proxyCheck.QueryAsync("104.16.255.200");

        // Assert
        Assert.Equal(StatusResult.OK, result.Status);
        Assert.Single(result.Results);

        var ipResult = result.Results.First().Value;
        Assert.Equal("AS13335", ipResult.ASN);
        Assert.Equal("CLOUDFLARENET - Cloudflare, Inc., US", ipResult.Provider);
        Assert.Equal("Canada", ipResult.Country);
        Assert.Equal(43.6532, ipResult.Latitude);
        Assert.Equal(-79.3832, ipResult.Longitude);
        Assert.Equal("Toronto", ipResult.City);
        Assert.Equal("CA", ipResult.ISOCode);
        Assert.False(ipResult.IsProxy);
        Assert.Equal("Business", ipResult.ProxyType);
    }

    [Fact]
    public async Task ExtensionsTest()
    {
        const string json =
            """
            {
                "foo": "bar"
            }
            """;

        // Arrange
        var handler = Substitute.For<HttpMessageHandler>();
        handler
            .SetupRequest(HttpMethod.Post, "https://proxycheck.io/v2/&vpn=0&asn=0&node=0&time=0&inf=1&port=0&seen=0&days=7&risk=0")
            .ReturnsResponse(HttpStatusCode.OK, json);

        using var client = new HttpClient(handler);

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient().Returns(client);

        var proxyCheck = new ProxyCheck(httpClientFactory)
        {
            UseTLS = true
        };

        // Act
        var result = await proxyCheck.QueryAsync("104.16.255.200");

        // Assert
        Assert.Single(result.ExtensionData);
        Assert.Equal("bar", result.ExtensionData["foo"].GetString());
    }
}
