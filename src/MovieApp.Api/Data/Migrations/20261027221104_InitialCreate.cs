using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MovieApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceTokens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Token = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Movies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SearchTitle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Overview = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: false),
                    ReleaseDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PosterUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    VoteAverage = table.Column<double>(type: "REAL", nullable: false),
                    GenresJson = table.Column<string>(type: "TEXT", nullable: false),
                    Adult = table.Column<bool>(type: "INTEGER", nullable: false),
                    UsCertification = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movies", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Movies",
                columns: new[] { "Id", "Adult", "GenresJson", "LastModified", "Overview", "PosterUrl", "ReleaseDate", "SearchTitle", "Title", "UsCertification", "VoteAverage" },
                values: new object[,]
                {
                    { 1001, false, "[\"Adventure\",\"Family\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In Toy Harbor, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2020, 1, 1), "TOY HARBOR", "Toy Harbor", null, 6.0 },
                    { 1002, false, "[\"Drama\",\"Mystery\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In The Paper Observatory, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2021, 2, 2), "THE PAPER OBSERVATORY", "The Paper Observatory", null, 6.0999999999999996 },
                    { 1003, false, "[\"Adventure\",\"Family\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In Lanterns at Dawn, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2022, 3, 3), "LANTERNS AT DAWN", "Lanterns at Dawn", null, 6.2000000000000002 },
                    { 1004, false, "[\"Drama\",\"Mystery\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In A Map of Rain, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2023, 4, 4), "A MAP OF RAIN", "A Map of Rain", null, 6.2999999999999998 },
                    { 1005, false, "[\"Adventure\",\"Family\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In The Last Tram, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2024, 5, 5), "THE LAST TRAM", "The Last Tram", null, 6.4000000000000004 },
                    { 1006, false, "[\"Drama\",\"Mystery\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In Cloud Garden, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2025, 6, 6), "CLOUD GARDEN", "Cloud Garden", null, 6.5 },
                    { 1007, false, "[\"Adventure\",\"Family\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In Summer on Platform Nine, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2020, 7, 7), "SUMMER ON PLATFORM NINE", "Summer on Platform Nine", null, 6.5999999999999996 },
                    { 1008, false, "[\"Drama\",\"Mystery\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In The Quiet Compass, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2021, 8, 8), "THE QUIET COMPASS", "The Quiet Compass", null, 6.7000000000000002 },
                    { 1009, false, "[\"Adventure\",\"Family\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In Moonlight Workshop, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2022, 9, 9), "MOONLIGHT WORKSHOP", "Moonlight Workshop", null, 6.7999999999999998 },
                    { 1010, false, "[\"Drama\",\"Mystery\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In Letters from the Orchard, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2023, 10, 10), "LETTERS FROM THE ORCHARD", "Letters from the Orchard", null, 6.9000000000000004 },
                    { 1011, false, "[\"Adventure\",\"Family\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In The Copper Kite, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2024, 11, 11), "THE COPPER KITE", "The Copper Kite", null, 7.0 },
                    { 1012, false, "[\"Drama\",\"Mystery\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In River of Small Stars, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2025, 12, 12), "RIVER OF SMALL STARS", "River of Small Stars", null, 7.0999999999999996 },
                    { 1013, false, "[\"Adventure\",\"Family\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In Winter Bicycle, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2020, 1, 13), "WINTER BICYCLE", "Winter Bicycle", null, 7.2000000000000002 },
                    { 1014, false, "[\"Drama\",\"Mystery\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In The Clockmaker's Picnic, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2021, 2, 14), "THE CLOCKMAKER'S PICNIC", "The Clockmaker's Picnic", null, 7.2999999999999998 },
                    { 1015, false, "[\"Adventure\",\"Family\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In Beyond the Blue Gate, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2022, 3, 15), "BEYOND THE BLUE GATE", "Beyond the Blue Gate", null, 7.4000000000000004 },
                    { 1016, false, "[\"Drama\",\"Mystery\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In The Library Boat, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2023, 4, 16), "THE LIBRARY BOAT", "The Library Boat", null, 7.5 },
                    { 1017, false, "[\"Adventure\",\"Family\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In Midnight Marmalade, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2024, 5, 17), "MIDNIGHT MARMALADE", "Midnight Marmalade", null, 7.5999999999999996 },
                    { 1018, false, "[\"Drama\",\"Mystery\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In The Glass Mountain, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2025, 6, 18), "THE GLASS MOUNTAIN", "The Glass Mountain", null, 7.7000000000000002 },
                    { 1019, false, "[\"Adventure\",\"Family\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In A Season of Bridges, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2020, 7, 19), "A SEASON OF BRIDGES", "A Season of Bridges", null, 7.7999999999999998 },
                    { 1020, false, "[\"Drama\",\"Mystery\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In The Sparrow Expedition, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2021, 8, 20), "THE SPARROW EXPEDITION", "The Sparrow Expedition", null, 7.9000000000000004 },
                    { 1021, false, "[\"Adventure\",\"Family\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In The Painted Station, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2022, 9, 21), "THE PAINTED STATION", "The Painted Station", null, 8.0 },
                    { 1022, false, "[\"Drama\",\"Mystery\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In Toy Makers of Willow Lane, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2023, 10, 22), "TOY MAKERS OF WILLOW LANE", "Toy Makers of Willow Lane", null, 8.0999999999999996 },
                    { 1023, false, "[\"Adventure\",\"Family\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In The Wandering Cinema, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2024, 11, 23), "THE WANDERING CINEMA", "The Wandering Cinema", null, 8.1999999999999993 },
                    { 1024, false, "[\"Drama\",\"Mystery\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In The Sunday Astronomer, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2025, 12, 24), "THE SUNDAY ASTRONOMER", "The Sunday Astronomer", null, 8.3000000000000007 },
                    { 1025, false, "[\"Adventure\",\"Family\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In North of the Lighthouse, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2020, 1, 25), "NORTH OF THE LIGHTHOUSE", "North of the Lighthouse", null, 8.4000000000000004 },
                    { 1026, false, "[\"Drama\",\"Mystery\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In The Floating Bakery, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2021, 2, 26), "THE FLOATING BAKERY", "The Floating Bakery", null, 6.0 },
                    { 1027, false, "[\"Adventure\",\"Family\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In Echoes in the Greenhouse, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2022, 3, 27), "ECHOES IN THE GREENHOUSE", "Echoes in the Greenhouse", null, 6.0999999999999996 },
                    { 1028, false, "[\"Drama\",\"Mystery\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In The Pocket Orchestra, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2023, 4, 1), "THE POCKET ORCHESTRA", "The Pocket Orchestra", null, 6.2000000000000002 },
                    { 1029, false, "[\"Adventure\",\"Family\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In A Train for Tomorrow, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2024, 5, 2), "A TRAIN FOR TOMORROW", "A Train for Tomorrow", null, 6.2999999999999998 },
                    { 1030, false, "[\"Drama\",\"Mystery\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo fiction: In The Silver Umbrella, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.", null, new DateOnly(2025, 6, 3), "THE SILVER UMBRELLA", "The Silver Umbrella", null, 6.4000000000000004 },
                    { 9001, false, "[\"Demo\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Synthetic record reserved for local editing demonstrations.", null, new DateOnly(2026, 1, 1), "DEMO EDIT SANDBOX", "Demo Edit Sandbox", null, 7.0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTokens_Token",
                table: "DeviceTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Movies_ReleaseDate",
                table: "Movies",
                column: "ReleaseDate");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_SearchTitle",
                table: "Movies",
                column: "SearchTitle");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceTokens");

            migrationBuilder.DropTable(
                name: "Movies");
        }
    }
}
