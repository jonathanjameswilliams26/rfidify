﻿using RFIDify.Services;
using RFIDify.Spotify.Apis.DelegatingHandlers;
using Serilog;
using System.Text.Json.Serialization;

namespace RFIDify;

public static class ConfigureServices
{
    public static void AddServices(this WebApplicationBuilder builder)
    {
        builder.AddCors();
        builder.AddSerilog();
        builder.AddJsonSerialization();
        builder.AddSwagger();
        builder.AddDatabase();
        builder.AddSpotifyApis();
        builder.Services.AddValidatorsFromAssembly(typeof(ConfigureServices).Assembly, includeInternalTypes: true);
        builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
    }

    private static void AddJsonSerialization(this WebApplicationBuilder builder)
    {
        builder.Services.ConfigureHttpJsonOptions(options => 
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        // Need this because of an open issue in the Swagger generator
        // See: https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/2293
        builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
    }

    private static void AddCors(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });
    }

    private static void AddSwagger(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c => c.UseOneOfForPolymorphism());
    }

    private static void AddSerilog(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, loggerConfiguration) =>
        {
            loggerConfiguration.ReadFrom.Configuration(context.Configuration);
        });
    }

    private static void AddDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite("Data Source=database/app.db");
        });
    }

    private static void AddSpotifyApis(this WebApplicationBuilder builder)
    {
        // Add the delegate handlers
        builder.Services.AddTransient<EnsureSuccessDelgatingHandler>();
        builder.Services.AddTransient<LoggingDelegatingHandler>();
        builder.Services.AddTransient<ClientIdAndSecretAuthenticationDelegatingHandler>();
        builder.Services.AddTransient<BearerTokenAuthenticationDelegatingHandler>();

        // Spotify Accounts API
        builder.Services.Configure<SpotifyAccountsApiOptions>(builder.Configuration.GetSection("Spotify:AccountsApi"));
        builder.Services.AddHttpClient<ISpotifyAccountsApi, SpotifyAccountsApi>(client =>
        {
            var baseUrl = builder.Configuration["Spotify:AccountsApi:BaseUrl"];
            client.BaseAddress = new Uri(baseUrl!);
        })
        .AddHttpMessageHandler<EnsureSuccessDelgatingHandler>()
        .AddHttpMessageHandler<LoggingDelegatingHandler>()
        .AddHttpMessageHandler<ClientIdAndSecretAuthenticationDelegatingHandler>();

        // Web API
        builder.Services.AddHttpClient<ISpotifyWebApi, SpotifyWebApi>(client =>
        {
            var baseUrl = builder.Configuration["Spotify:WebApi:BaseUrl"];
            client.BaseAddress = new Uri(baseUrl!);
        })
        .AddHttpMessageHandler<EnsureSuccessDelgatingHandler>()
        .AddHttpMessageHandler<LoggingDelegatingHandler>()
        .AddHttpMessageHandler<BearerTokenAuthenticationDelegatingHandler>();
    }
}
