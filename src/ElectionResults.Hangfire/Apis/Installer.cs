﻿using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json;
using ElectionResults.Hangfire.Apis.RoAep;
using ElectionResults.Hangfire.Options;
using Microsoft.Extensions.Options;
using Refit;

namespace ElectionResults.Hangfire.Apis;

public static class Installer
{
    public static IServiceCollection ConfigureApis(this IServiceCollection services)
    {
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };

        services
            .AddRefitClient<IRoAepApi>(new RefitSettings(new SystemTextJsonContentSerializer(jsonSerializerOptions))
            {
                UrlParameterFormatter = new RoAepUrlParameterFormatter(),
            })
            .ConfigureHttpClient((sp, client) =>
            {
                var roAepOptions = sp.GetService<IOptions<RoAepOptions>>()!;
                client.BaseAddress = new Uri(roAepOptions.Value.ApiUrl);
            });

        return services;
    }
}

public class RoAepUrlParameterFormatter : IUrlParameterFormatter
{
    public string? Format(object? value, ICustomAttributeProvider attributeProvider, Type type)
    {
        return value.ToString()!.ToLower();
    }
}