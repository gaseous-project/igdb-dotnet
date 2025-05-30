﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using IGDB.Models;
using IGDB.Serialization;
using Microsoft.Extensions.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Polly;
using RestEase;
using RestEase.Implementation;

namespace IGDB
{
  [Header("Accept", "application/json")]
  public interface IGDBApi
  {
    /// <summary>
    /// Your secret, private IGDB API token
    /// </summary>
    /// <value></value>
    [Header("client-id")]
    string ClientId { get; set; }

    /// <summary>
    /// Queries a standard IGDB endpoint with an APIcalypse query. See endpoints in <see cref="IGDB.IGDBClient.Endpoints" />.
    /// </summary>
    /// <param name="endpoint">The IGDB endpoint name to query, see <see cref="IGDB.IGDBClient.Endpoints" /></param>
    /// <param name="query">The APIcalypse query to send</param>
    /// <typeparam name="T">The IGDB.Models.* entity to deserialize the response for.</typeparam>
    /// <returns>Array of IGDB models of the specified type</returns>
    [Post("/{endpoint}")]
    Task<T[]> QueryAsync<T>([Path] string endpoint, [Body] string query = null);

    /// <summary>
    /// Queries a standard IGDB endpoint with an APIcalypse query and returns the deserialized response along with the HttpResponseMessage. See endpoints in <see cref="IGDB.IGDBClient.Endpoints" />.
    /// </summary>
    /// <param name="endpoint">The IGDB endpoint name to query, see <see cref="IGDB.IGDBClient.Endpoints" /></param>
    /// <param name="query">The APIcalypse query to send</param>
    /// <typeparam name="T">The IGDB.Models.* entity to deserialize the response for.</typeparam>
    /// <returns>RestEase Response containing the HttpResponseMessage and the array of IGDB models of the specified type</returns>
    [Post("/{endpoint}")]
    Task<Response<T[]>> QueryWithResponseAsync<T>([Path] string endpoint, [Body] string query = null);

    /// <summary>
    /// Queries a standard IGDB endpoint with an APIcalypse query. See endpoints in <see cref="IGDB.IGDBClient.Endpoints" />.
    /// </summary>
    /// <param name="endpoint">The IGDB endpoint name to query, see <see cref="IGDB.IGDBClient.Endpoints" /></param>
    /// <param name="query">The APIcalypse query to send</param>
    /// <typeparam name="T">The IGDB.Models.* entity to deserialize the response for.</typeparam>
    /// <returns>Array of IGDB models of the specified type</returns>
    [Post("/{endpoint}/count")]
    Task<CountResponse> CountAsync([Path] string endpoint, [Body] string query = null);

    /// <summary>
    /// Retrieves list of available data dumps (IGDB Partners only)
    /// </summary>
    [Get("/dumps")]
    Task<DataDump[]> GetDataDumpsAsync();

    /// <summary>
    /// Retrieves the download URL of a data dump (IGDB Partners only). Use the S3Url to download the dump (link expires after 5 minutes).
    /// </summary>
    [Get("/dumps/{endpoint}")]
    Task<DataDumpEndpoint> GetDataDumpForEndpointAsync([Path] string endpoint);
  }

  public sealed class IGDBClient
  {
    private readonly IGDBApi _api;
    private readonly TokenManager _tokenManager;

    public static JsonSerializerSettings DefaultJsonSerializerSettings = new JsonSerializerSettings()
    {
      Converters = new List<JsonConverter>() {
          new IdentityConverter(),
          new UnixTimestampConverter()
        },
      ContractResolver = new DefaultContractResolver()
      {
        NamingStrategy = new SnakeCaseNamingStrategy()
      }
    };

    /// <summary>
    /// Create a IGDB API client based on a custom-created RestEase client. Adds required
    /// JSON serializer settings on top of any existing settings. Uses default in-memory access token management.
    /// </summary>
    /// <returns></returns>
    public IGDBClient(string clientId, string clientSecret) :
     this(clientId, clientSecret,
        new InMemoryTokenStore(), null)
    {
    }
    
    /// <summary>
    /// Create a IGDB API client based on a custom-created RestEase client. Adds required
    /// JSON serializer settings on top of any existing settings. Pass in your own token store (default is in-memory).
    /// </summary>
    /// <returns></returns>
    public IGDBClient(string clientId, string clientSecret, ITokenStore tokenStore) :
     this(clientId, clientSecret,
        tokenStore, null)
    {
    }

    /// Initializes a new custom instance of the <see cref="IGDBClient"/> class.
    /// </summary>
    /// <param name="clientId">
    /// The IGDB client ID. You can find this in the IGDB developer portal.
    /// </param>
    /// <param name="clientSecret">
    /// The IGDB client secret. You can find this in the IGDB developer portal.
    /// </param>
    /// <param name="tokenStore">
    /// The token store implementation for storing and retrieving OAuth tokens. 
    /// Pass <c>InMemoryTokenStore</c> if you do not have a custom store implemented.
    /// </param>
    /// <param name="policy">
    /// An optional Polly <see cref="IAsyncPolicy{HttpResponseMessage}"/> to apply to HTTP requests for resilience (e.g., retries, circuit breakers).
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="clientId"/>, <paramref name="clientSecret"/>, or <paramref name="tokenStore"/> is <c>null</c>.
    /// </exception>
    public IGDBClient(string clientId, string clientSecret, ITokenStore tokenStore, IAsyncPolicy<HttpResponseMessage> policy)
    {
      if (clientId == null)
      {
        throw new ArgumentNullException(nameof(clientId), "Missing IGDB client ID. You can find this in the IGDB developer portal.");
      }
      if (clientSecret == null)
      {
        throw new ArgumentNullException(nameof(clientSecret), "Missing IGDB client secret. You can find this in the IGDB developer portal.");
      }
      if (tokenStore == null)
      {
        throw new ArgumentNullException(nameof(tokenStore),
          "A ITokenStore is required. Pass InMemoryTokenStore if you do not have a custom store implemented.");
      }

      RequestModifier requestModifier = async (request, cancellationToken) =>
      {
        var twitchToken = await _tokenManager.AcquireTokenAsync();

        if (twitchToken?.AccessToken != null)
        {
          request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", twitchToken.AccessToken);
        }
      };

      _tokenManager = new TokenManager(tokenStore, new TwitchOAuthClient(clientId, clientSecret));

      RestClient client = null;
      if (policy != null)
      {
        client = new RestClient("https://api.igdb.com/v4", new PolicyHttpMessageHandler(policy)
        {
          InnerHandler = new ModifyingClientHttpHandler(requestModifier)
        });
      }
      else
      {
        client = new RestClient("https://api.igdb.com/v4", requestModifier);
      }

      client.JsonSerializerSettings = DefaultJsonSerializerSettings;

      var api = client.For<IGDBApi>();
      api.ClientId = clientId;
      _api = api;
    }

    public static IGDBClient CreateWithDefaults(string clientId, string clientSecret)
    {
      return new IGDBClient(clientId, clientSecret, new InMemoryTokenStore(), ApiPolicy.DefaultApiPolicy);
    }

    public async Task<T[]> QueryAsync<T>(string endpoint, string query = null)
    {
      try
      {
        return await _api.QueryAsync<T>(endpoint, query);
      }
      catch (ApiException apiEx)
      {
        // Acquire new token and retry request (once)
        if (IsInvalidTokenResponse(apiEx))
        {
          await _tokenManager.RefreshTokenAsync();

          return await _api.QueryAsync<T>(endpoint, query);
        }

        // Pass up any other exceptions
        throw apiEx;
      }
    }

    public async Task<Response<T[]>> QueryWithResponseAsync<T>(string endpoint, string query = null)
    {
      try
      {
        return await _api.QueryWithResponseAsync<T>(endpoint, query);
      }
      catch (ApiException apiEx)
      {
        // Acquire new token and retry request (once)
        if (IsInvalidTokenResponse(apiEx))
        {
          await _tokenManager.RefreshTokenAsync();

          return await _api.QueryWithResponseAsync<T>(endpoint, query);
        }

        // Pass up any other exceptions
        throw apiEx;
      }
    }

    public async Task<CountResponse> CountAsync(string endpoint, string query = null)
    {
      try
      {
        return await _api.CountAsync(endpoint, query);
      }
      catch (ApiException apiEx)
      {
        // Acquire new token and retry request (once)
        if (IsInvalidTokenResponse(apiEx))
        {
          await _tokenManager.RefreshTokenAsync();

          return await _api.CountAsync(endpoint, query);
        }

        // Pass up any other exceptions
        throw apiEx;
      }
    }

    public async Task<DataDump[]> GetDataDumpsAsync()
    {
      try
      {
        return await _api.GetDataDumpsAsync();
      }
      catch (ApiException apiEx)
      {
        // Acquire new token and retry request (once)
        if (IsInvalidTokenResponse(apiEx))
        {
          await _tokenManager.RefreshTokenAsync();

          return await _api.GetDataDumpsAsync();
        }

        // Pass up any other exceptions
        throw apiEx;
      }
    }

    public async Task<DataDumpEndpoint> GetDataDumpEndpointAsync(string endpoint)
    {
      try
      {
        return await _api.GetDataDumpForEndpointAsync(endpoint);
      }
      catch (ApiException apiEx)
      {
        // Acquire new token and retry request (once)
        if (IsInvalidTokenResponse(apiEx))
        {
          await _tokenManager.RefreshTokenAsync();

          return await _api.GetDataDumpForEndpointAsync(endpoint);
        }

        // Pass up any other exceptions
        throw apiEx;
      }
    }

    /// <summary>
    /// Whether or not an ApiException represents an invalid_token response.
    /// </summary>
    /// <param name="ex"></param>
    /// <remarks>See: https://dev.twitch.tv/docs/authentication</remarks>
    /// <returns></returns>
    public static bool IsInvalidTokenResponse(ApiException ex)
    {
      return ex.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
        ex.Headers.WwwAuthenticate.ToString().Contains("invalid_token");
    }

    public static class Endpoints
    {
      public const string AgeRating = "age_ratings";
      public const string AgeRatingCategories = "age_rating_categories";
      public const string AgeRatingContentDescriptionsV2 = "age_rating_content_descriptions_v2";
      public const string AgeRatingOrganizations = "age_rating_organizations";
      public const string AlternativeNames = "alternative_names";
      public const string Artworks = "artworks";
      public const string Characters = "characters";
      public const string CharacterGenders = "character_genders";
      public const string CharacterMugShots = "character_mug_shots";
      public const string CharacterSpecies = "character_species";
      public const string Collections = "collections";
      public const string Companies = "companies";
      public const string CompanyStatus = "company_status";
      public const string CompanyWebsites = "company_websites";
      public const string CompanyLogos = "company_logos";
      public const string Covers = "covers";
      public const string DateFormats = "date_formats";
      public const string ExternalGames = "external_games";
      public const string ExternalGameSources = "external_game_sources";
      public const string Franchies = "franchises";
      public const string Games = "games";
      public const string GameEngines = "game_engines";
      public const string GameEngineLogos = "game_engine_logos";
      public const string GameVersions = "game_versions";
      public const string GameModes = "game_modes";
      public const string GameReleaseFormats = "game_release_formats";
      public const string GameStatuses = "game_statuses";
      public const string GameTimeToBeats = "game_time_to_beats";
      public const string GameTypes = "game_types";
      public const string GameVersionFeatures = "game_version_features";
      public const string GameVersionFeatureValues = "game_version_feature_values";
      public const string GameVideos = "game_videos";
      public const string Genres = "genres";
      public const string InvolvedCompanies = "involved_companies";
      public const string Keywords = "keywords";
      public const string MultiplayerModes = "multiplayer_modes";
      public const string Platforms = "platforms";
      public const string PlatformFamilies = "platform_families";
      public const string PlatformLogos = "platform_logos";
      public const string PlatformTypes = "platform_types";
      public const string PlatformVersions = "platform_versions";
      public const string PlatformVersionCompanies = "platform_version_companies";
      public const string PlatformVersionReleaseDates = "platform_version_release_dates";
      public const string PlatformWebsites = "platform_websites";
      public const string PlayerPerspectives = "player_perspectives";
      public const string PopularityPrimitives = "popularity_primitives";
      public const string PopularityTypes = "popularity_types";
      public const string ReleaseDates = "release_dates";
      public const string ReleaseDateRegions = "release_date_regions";
      public const string Screenshots = "screenshots";
      public const string Search = "search";
      public const string Themes = "themes";
      public const string Websites = "websites";
      public const string WebsiteTypes = "website_types";
    }
  }
}
