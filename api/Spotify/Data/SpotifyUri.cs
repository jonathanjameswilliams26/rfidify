﻿using System.Text.RegularExpressions;

namespace RFIDify.Spotify.Data;

public partial record SpotifyUri
{
    public string Uri { get; init; }
    public string Id { get; init; }
    public SpotifyItemType Type { get; init; }

    public SpotifyUri(string uri)
    {
        Uri = uri;

        if (!IsValid(uri))
        {
            throw new ArgumentException("Invalid Spotify Uri", nameof(uri));
        }

        var sections = uri.Split(':');
        Id = sections[2];
        Type = sections[1] switch
        {
            "album" => SpotifyItemType.Album,
            "artist" => SpotifyItemType.Artist,
            "playlist" => SpotifyItemType.Playlist,
            "track" => SpotifyItemType.Track,
            _ => throw new ArgumentException("Invalid Spotify Uri", nameof(uri))
        };
    }

    [GeneratedRegex(@"spotify:(playlist|album|track|artist):[a-zA-Z0-9]+")]
    private static partial Regex SpotifyUriRegex();
    public static bool IsValid(string uri) => SpotifyUriRegex().IsMatch(uri);
}