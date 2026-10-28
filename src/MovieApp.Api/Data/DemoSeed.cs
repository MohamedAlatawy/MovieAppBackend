using System.Text.Json;
using MovieApp.Api.Entities;

namespace MovieApp.Api.Data;

public static class DemoSeed
{
    public static IEnumerable<Movie> Movies()
    {
        string[] titles = ["Toy Harbor", "The Paper Observatory", "Lanterns at Dawn", "A Map of Rain", "The Last Tram", "Cloud Garden", "Summer on Platform Nine", "The Quiet Compass", "Moonlight Workshop", "Letters from the Orchard", "The Copper Kite", "River of Small Stars", "Winter Bicycle", "The Clockmaker's Picnic", "Beyond the Blue Gate", "The Library Boat", "Midnight Marmalade", "The Glass Mountain", "A Season of Bridges", "The Sparrow Expedition", "The Painted Station", "Toy Makers of Willow Lane", "The Wandering Cinema", "The Sunday Astronomer", "North of the Lighthouse", "The Floating Bakery", "Echoes in the Greenhouse", "The Pocket Orchestra", "A Train for Tomorrow", "The Silver Umbrella"];
        for (var i = 0; i < titles.Length; i++)
            yield return new Movie
            {
                Id = 1001 + i,
                Title = titles[i],
                SearchTitle = titles[i].ToUpperInvariant(),
                Overview = $"Demo fiction: In {titles[i]}, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.",
                ReleaseDate = new DateOnly(2020 + i % 6, 1 + i % 12, 1 + i % 27),
                VoteAverage = Math.Round(6 + (i % 25) / 10.0, 1),
                GenresJson = JsonSerializer.Serialize(i % 2 == 0 ? new[] { "Adventure", "Family" } : new[] { "Drama", "Mystery" }),
                Adult = false,
                UsCertification = null,
                LastModified = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };
        yield return new Movie { Id = 9001, Title = "Demo Edit Sandbox", SearchTitle = "DEMO EDIT SANDBOX", Overview = "Synthetic record reserved for local editing demonstrations.", ReleaseDate = new DateOnly(2026, 1, 1), VoteAverage = 7, GenresJson = "[\"Demo\"]", LastModified = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
    }
}
