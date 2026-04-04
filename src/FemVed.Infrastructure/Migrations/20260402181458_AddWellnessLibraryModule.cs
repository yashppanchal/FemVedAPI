using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FemVed.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWellnessLibraryModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "library_video_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "order_source",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "scope",
                table: "coupons",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "ALL");

            migrationBuilder.CreateTable(
                name: "library_domain",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    hero_image_desktop = table.Column<string>(type: "text", nullable: true),
                    hero_image_mobile = table.Column<string>(type: "text", nullable: true),
                    hero_image_portrait = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_library_domain", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "library_price_tiers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tier_key = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_library_price_tiers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "library_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    domain_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    slug = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    card_image = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_library_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_library_categories_domain",
                        column: x => x.domain_id,
                        principalTable: "library_domain",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "library_filter_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    domain_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    filter_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    filter_target = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_library_filter_types", x => x.id);
                    table.ForeignKey(
                        name: "fk_library_filter_types_domain",
                        column: x => x.domain_id,
                        principalTable: "library_domain",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "library_tier_prices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    currency_symbol = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_library_tier_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_library_tier_prices_tier",
                        column: x => x.tier_id,
                        principalTable: "library_price_tiers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "library_videos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expert_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_tier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    synopsis = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    card_image = table.Column<string>(type: "text", nullable: true),
                    hero_image = table.Column<string>(type: "text", nullable: true),
                    icon_emoji = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    gradient_class = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    trailer_url = table.Column<string>(type: "text", nullable: true),
                    stream_url = table.Column<string>(type: "text", nullable: true),
                    video_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    total_duration = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    total_duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    release_year = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    featured_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    featured_position = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_library_videos", x => x.id);
                    table.ForeignKey(
                        name: "fk_library_videos_category",
                        column: x => x.category_id,
                        principalTable: "library_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_library_videos_expert",
                        column: x => x.expert_id,
                        principalTable: "experts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_library_videos_price_tier",
                        column: x => x.price_tier_id,
                        principalTable: "library_price_tiers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "library_video_episodes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    video_id = table.Column<Guid>(type: "uuid", nullable: false),
                    episode_number = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    duration = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    stream_url = table.Column<string>(type: "text", nullable: true),
                    thumbnail_url = table.Column<string>(type: "text", nullable: true),
                    is_free_preview = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_library_video_episodes", x => x.id);
                    table.ForeignKey(
                        name: "fk_library_video_episodes_video",
                        column: x => x.video_id,
                        principalTable: "library_videos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "library_video_features",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    video_id = table.Column<Guid>(type: "uuid", nullable: false),
                    icon = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_library_video_features", x => x.id);
                    table.ForeignKey(
                        name: "fk_library_video_features_video",
                        column: x => x.video_id,
                        principalTable: "library_videos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "library_video_prices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    video_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    currency_symbol = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    original_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_library_video_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_library_video_prices_video",
                        column: x => x.video_id,
                        principalTable: "library_videos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "library_video_tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    video_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_library_video_tags", x => x.id);
                    table.ForeignKey(
                        name: "fk_library_video_tags_video",
                        column: x => x.video_id,
                        principalTable: "library_videos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "library_video_testimonials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    video_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    review_text = table.Column<string>(type: "text", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_library_video_testimonials", x => x.id);
                    table.CheckConstraint("ck_library_video_testimonials_rating", "rating >= 1 AND rating <= 5");
                    table.ForeignKey(
                        name: "fk_library_video_testimonials_video",
                        column: x => x.video_id,
                        principalTable: "library_videos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_library_access",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    video_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchased_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_watched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    watch_progress_seconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_library_access", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_library_access_order",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_library_access_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_library_access_video",
                        column: x => x.video_id,
                        principalTable: "library_videos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_video_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    video_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    review_text = table.Column<string>(type: "text", nullable: true),
                    is_approved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_video_reviews", x => x.id);
                    table.CheckConstraint("ck_user_video_reviews_rating", "rating >= 1 AND rating <= 5");
                    table.ForeignKey(
                        name: "fk_user_video_reviews_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_video_reviews_video",
                        column: x => x.video_id,
                        principalTable: "library_videos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_episode_progress",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    episode_id = table.Column<Guid>(type: "uuid", nullable: false),
                    watch_progress_seconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    last_watched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_episode_progress", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_episode_progress_episode",
                        column: x => x.episode_id,
                        principalTable: "library_video_episodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_episode_progress_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "library_domain",
                columns: new[] { "id", "created_at", "description", "hero_image_desktop", "hero_image_mobile", "hero_image_portrait", "is_active", "name", "slug", "sort_order", "updated_at" },
                values: new object[] { new Guid("22222222-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Recorded wellness video content — masterclasses and series for self-paced learning.", null, null, null, true, "Wellness Library", "wellness-library", 1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                table: "library_price_tiers",
                columns: new[] { "id", "created_at", "display_name", "is_active", "sort_order", "tier_key" },
                values: new object[,]
                {
                    { new Guid("44444444-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Movie", true, 1, "MOVIE" },
                    { new Guid("44444444-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Standard", true, 2, "STANDARD" },
                    { new Guid("44444444-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Medium", true, 3, "MEDIUM" },
                    { new Guid("44444444-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Large", true, 4, "LARGE" }
                });

            migrationBuilder.InsertData(
                table: "library_filter_types",
                columns: new[] { "id", "created_at", "domain_id", "filter_key", "filter_target", "is_active", "name", "sort_order" },
                values: new object[,]
                {
                    { new Guid("33333333-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-0000-0000-0000-000000000001"), "masterclass", "VIDEOTYPE", true, "Masterclasses", 1 },
                    { new Guid("33333333-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-0000-0000-0000-000000000001"), "series", "VIDEOTYPE", true, "Series", 2 },
                    { new Guid("33333333-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-0000-0000-0000-000000000001"), "mindfulness", "CATEGORY", true, "Mindfulness", 3 }
                });

            migrationBuilder.InsertData(
                table: "library_tier_prices",
                columns: new[] { "id", "amount", "created_at", "currency_code", "currency_symbol", "location_code", "tier_id", "updated_at" },
                values: new object[,]
                {
                    { new Guid("55555555-0000-0000-0001-000000000001"), 499m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "INR", "₹", "IN", new Guid("44444444-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("55555555-0000-0000-0001-000000000002"), 9m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "GBP", "£", "GB", new Guid("44444444-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("55555555-0000-0000-0001-000000000003"), 12m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "USD", "$", "US", new Guid("44444444-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("55555555-0000-0000-0002-000000000001"), 999m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "INR", "₹", "IN", new Guid("44444444-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("55555555-0000-0000-0002-000000000002"), 19m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "GBP", "£", "GB", new Guid("44444444-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("55555555-0000-0000-0002-000000000003"), 24m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "USD", "$", "US", new Guid("44444444-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("55555555-0000-0000-0003-000000000001"), 1499m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "INR", "₹", "IN", new Guid("44444444-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("55555555-0000-0000-0003-000000000002"), 29m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "GBP", "£", "GB", new Guid("44444444-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("55555555-0000-0000-0003-000000000003"), 35m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "USD", "$", "US", new Guid("44444444-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("55555555-0000-0000-0004-000000000001"), 2199m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "INR", "₹", "IN", new Guid("44444444-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("55555555-0000-0000-0004-000000000002"), 39m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "GBP", "£", "GB", new Guid("44444444-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("55555555-0000-0000-0004-000000000003"), 49m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "USD", "$", "US", new Guid("44444444-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.AddCheckConstraint(
                name: "chk_psl_action",
                table: "program_session_log",
                sql: "action IN ('STARTED', 'PAUSED', 'RESUMED', 'ENDED', 'SCHEDULED')");

            migrationBuilder.CreateIndex(
                name: "IX_orders_library_video_id",
                table: "orders",
                column: "library_video_id");

            migrationBuilder.AddCheckConstraint(
                name: "chk_order_source",
                table: "orders",
                sql: "(order_source = 'GUIDED' AND duration_price_id IS NOT NULL AND library_video_id IS NULL) OR (order_source = 'LIBRARY' AND library_video_id IS NOT NULL AND duration_price_id IS NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_library_categories_domain_id",
                table: "library_categories",
                column: "domain_id");

            migrationBuilder.CreateIndex(
                name: "uq_library_categories_slug",
                table: "library_categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_library_domain_slug",
                table: "library_domain",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_library_filter_types_domain_id",
                table: "library_filter_types",
                column: "domain_id");

            migrationBuilder.CreateIndex(
                name: "uq_library_price_tiers_tier_key",
                table: "library_price_tiers",
                column: "tier_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_library_tier_prices_tier_location",
                table: "library_tier_prices",
                columns: new[] { "tier_id", "location_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_library_video_episodes_video_id",
                table: "library_video_episodes",
                column: "video_id");

            migrationBuilder.CreateIndex(
                name: "uq_library_video_episodes_video_number",
                table: "library_video_episodes",
                columns: new[] { "video_id", "episode_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_library_video_features_video_id",
                table: "library_video_features",
                column: "video_id");

            migrationBuilder.CreateIndex(
                name: "uq_library_video_prices_video_location",
                table: "library_video_prices",
                columns: new[] { "video_id", "location_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_library_video_tags_video_id",
                table: "library_video_tags",
                column: "video_id");

            migrationBuilder.CreateIndex(
                name: "idx_library_video_testimonials_video_id",
                table: "library_video_testimonials",
                column: "video_id");

            migrationBuilder.CreateIndex(
                name: "idx_library_videos_category_id",
                table: "library_videos",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "idx_library_videos_expert_id",
                table: "library_videos",
                column: "expert_id");

            migrationBuilder.CreateIndex(
                name: "idx_library_videos_is_featured",
                table: "library_videos",
                column: "is_featured");

            migrationBuilder.CreateIndex(
                name: "idx_library_videos_status",
                table: "library_videos",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_library_videos_price_tier_id",
                table: "library_videos",
                column: "price_tier_id");

            migrationBuilder.CreateIndex(
                name: "uq_library_videos_slug",
                table: "library_videos",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_user_episode_progress_user_id",
                table: "user_episode_progress",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_episode_progress_episode_id",
                table: "user_episode_progress",
                column: "episode_id");

            migrationBuilder.CreateIndex(
                name: "uq_user_episode_progress_user_episode",
                table: "user_episode_progress",
                columns: new[] { "user_id", "episode_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_user_library_access_user_id",
                table: "user_library_access",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_user_library_access_video_id",
                table: "user_library_access",
                column: "video_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_library_access_order_id",
                table: "user_library_access",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "uq_user_library_access_user_video",
                table: "user_library_access",
                columns: new[] { "user_id", "video_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_user_video_reviews_video_id",
                table: "user_video_reviews",
                column: "video_id");

            migrationBuilder.CreateIndex(
                name: "uq_user_video_reviews_user_video",
                table: "user_video_reviews",
                columns: new[] { "user_id", "video_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_orders_library_video",
                table: "orders",
                column: "library_video_id",
                principalTable: "library_videos",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_orders_library_video",
                table: "orders");

            migrationBuilder.DropTable(
                name: "library_filter_types");

            migrationBuilder.DropTable(
                name: "library_tier_prices");

            migrationBuilder.DropTable(
                name: "library_video_features");

            migrationBuilder.DropTable(
                name: "library_video_prices");

            migrationBuilder.DropTable(
                name: "library_video_tags");

            migrationBuilder.DropTable(
                name: "library_video_testimonials");

            migrationBuilder.DropTable(
                name: "user_episode_progress");

            migrationBuilder.DropTable(
                name: "user_library_access");

            migrationBuilder.DropTable(
                name: "user_video_reviews");

            migrationBuilder.DropTable(
                name: "library_video_episodes");

            migrationBuilder.DropTable(
                name: "library_videos");

            migrationBuilder.DropTable(
                name: "library_categories");

            migrationBuilder.DropTable(
                name: "library_price_tiers");

            migrationBuilder.DropTable(
                name: "library_domain");

            migrationBuilder.DropCheckConstraint(
                name: "chk_psl_action",
                table: "program_session_log");

            migrationBuilder.DropIndex(
                name: "IX_orders_library_video_id",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "chk_order_source",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "library_video_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "order_source",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "scope",
                table: "coupons");
        }
    }
}
