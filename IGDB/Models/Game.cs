using System;
using Newtonsoft.Json;

namespace IGDB.Models
{
  public class Game : ITimestamps, IIdentifier, IHasChecksum
  {
    public IdentitiesOrValues<AgeRating> AgeRatings { get; set; }

    public double? AggregatedRating { get; set; }

    public int? AggregatedRatingCount { get; set; }

    public IdentitiesOrValues<AlternativeName> AlternativeNames { get; set; }

    public IdentitiesOrValues<Artwork> Artworks { get; set; }

    public IdentitiesOrValues<Game> Bundles { get; set; }

    public string Checksum { get; set; }

    public IdentitiesOrValues<Collection> Collections { get; set; }

    public IdentityOrValue<Cover> Cover { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public IdentitiesOrValues<Game> Dlcs { get; set; }

    public IdentitiesOrValues<Game> ExpandedGames { get; set; }

    public IdentitiesOrValues<Game> Expansions { get; set; }

    public IdentitiesOrValues<ExternalGame> ExternalGames { get; set; }

    public DateTimeOffset? FirstReleaseDate { get; set; }

    public IdentitiesOrValues<Game> Forks { get; set; }

    public IdentityOrValue<Franchise> Franchise { get; set; }

    public IdentitiesOrValues<Franchise> Franchises { get; set; }

    public IdentitiesOrValues<GameEngine> GameEngines { get; set; }

    public IdentitiesOrValues<GameLocalization> GameLocalizations { get; set; }

    public IdentitiesOrValues<GameMode> GameModes { get; set; }
    public IdentityOrValue<GameStatus> GameStatus { get; set; }
    public IdentityOrValue<GameType> GameType { get; set; }

    public IdentitiesOrValues<Genre> Genres { get; set; }

    public int? Hypes { get; set; }

    public long? Id { get; set; }

    public IdentitiesOrValues<InvolvedCompany> InvolvedCompanies { get; set; }

    public IdentitiesOrValues<Keyword> Keywords { get; set; }

    public IdentitiesOrValues<LanguageSupport> LanguageSupports { get; set; }

    public IdentitiesOrValues<MultiplayerMode> MultiplayerModes { get; set; }

    public string Name { get; set; }

    public IdentityOrValue<Game> ParentGame { get; set; }

    public IdentitiesOrValues<Platform> Platforms { get; set; }

    public IdentitiesOrValues<PlayerPerspective> PlayerPerspectives { get; set; }

    public IdentitiesOrValues<Game> Ports { get; set; }

    public double? Rating { get; set; }

    public int? RatingCount { get; set; }

    public IdentitiesOrValues<ReleaseDate> ReleaseDates { get; set; }

    public IdentitiesOrValues<Game> Remakes { get; set; }

    public IdentitiesOrValues<Game> Remasters { get; set; }

    public IdentitiesOrValues<Screenshot> Screenshots { get; set; }

    public IdentitiesOrValues<Game> SimilarGames { get; set; }

    public string Slug { get; set; }

    public IdentitiesOrValues<Game> StandaloneExpansions { get; set; }

    public string Storyline { get; set; }

    public string Summary { get; set; }

    public int[] Tags { get; set; }

    public IdentitiesOrValues<Theme> Themes { get; set; }

    public double? TotalRating { get; set; }

    public int? TotalRatingCount { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public string Url { get; set; }

    public IdentityOrValue<Game> VersionParent { get; set; }

    public string VersionTitle { get; set; }

    public IdentitiesOrValues<GameVideo> Videos { get; set; }

    public IdentitiesOrValues<Website> Websites { get; set; }
  }
}