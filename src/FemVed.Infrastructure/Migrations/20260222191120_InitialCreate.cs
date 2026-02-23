using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FemVed.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "coupons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    discount_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    max_uses = table.Column<int>(type: "integer", nullable: true),
                    used_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    valid_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coupons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guided_domains",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guided_domains", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guided_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    domain_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    hero_title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    hero_subtext = table.Column<string>(type: "text", nullable: false),
                    cta_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cta_link = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    page_header = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guided_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_guided_categories_domain",
                        column: x => x.domain_id,
                        principalTable: "guided_domains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_guided_categories_parent",
                        column: x => x.parent_id,
                        principalTable: "guided_categories",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<short>(type: "smallint", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    country_dial_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    country_iso_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    mobile_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    full_mobile = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    is_mobile_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_email_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    whatsapp_opt_in = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_role",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "category_key_areas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    area_text = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_key_areas", x => x.id);
                    table.ForeignKey(
                        name: "fk_category_key_areas_category",
                        column: x => x.category_id,
                        principalTable: "guided_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "category_whats_included",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_text = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_whats_included", x => x.id);
                    table.ForeignKey(
                        name: "fk_category_whats_included_category",
                        column: x => x.category_id,
                        principalTable: "guided_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "admin_audit_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    admin_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    before_value = table.Column<string>(type: "jsonb", nullable: true),
                    after_value = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_audit_log", x => x.id);
                    table.ForeignKey(
                        name: "fk_admin_audit_log_user",
                        column: x => x.admin_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    bio = table.Column<string>(type: "text", nullable: false),
                    short_bio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    profile_image_url = table.Column<string>(type: "text", nullable: true),
                    specialisations = table.Column<string[]>(type: "text[]", nullable: true),
                    years_experience = table.Column<short>(type: "smallint", nullable: true),
                    credentials = table.Column<string[]>(type: "text[]", nullable: true),
                    location_country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experts", x => x.id);
                    table.ForeignKey(
                        name: "fk_experts_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gdpr_deletion_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    processed_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gdpr_deletion_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_gdpr_processed_by",
                        column: x => x.processed_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_gdpr_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    template_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    recipient = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_log", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_log_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "password_reset_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_password_reset_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_password_reset_tokens_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "programs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expert_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    slug = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    grid_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    grid_image_url = table.Column<string>(type: "text", nullable: true),
                    overview = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_programs", x => x.id);
                    table.ForeignKey(
                        name: "fk_programs_category",
                        column: x => x.category_id,
                        principalTable: "guided_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_programs_expert",
                        column: x => x.expert_id,
                        principalTable: "experts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "program_durations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    program_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    weeks = table.Column<short>(type: "smallint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_durations", x => x.id);
                    table.ForeignKey(
                        name: "fk_program_durations_program",
                        column: x => x.program_id,
                        principalTable: "programs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "program_tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    program_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_tags", x => x.id);
                    table.ForeignKey(
                        name: "fk_program_tags_program",
                        column: x => x.program_id,
                        principalTable: "programs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "program_testimonials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    program_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reviewer_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    review_text = table.Column<string>(type: "text", nullable: false),
                    rating = table.Column<short>(type: "smallint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_testimonials", x => x.id);
                    table.ForeignKey(
                        name: "fk_program_testimonials_program",
                        column: x => x.program_id,
                        principalTable: "programs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "program_what_you_get",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    program_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_text = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_what_you_get", x => x.id);
                    table.ForeignKey(
                        name: "fk_program_what_you_get_program",
                        column: x => x.program_id,
                        principalTable: "programs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "program_who_is_this_for",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    program_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_text = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_who_is_this_for", x => x.id);
                    table.ForeignKey(
                        name: "fk_program_who_is_this_for_program",
                        column: x => x.program_id,
                        principalTable: "programs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "duration_prices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    duration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    currency_symbol = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_duration_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_duration_prices_duration",
                        column: x => x.duration_id,
                        principalTable: "program_durations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    duration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    duration_price_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    location_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    coupon_id = table.Column<Guid>(type: "uuid", nullable: true),
                    discount_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    payment_gateway = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    gateway_order_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    gateway_payment_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    gateway_response = table.Column<string>(type: "jsonb", nullable: true),
                    failure_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_orders_coupon",
                        column: x => x.coupon_id,
                        principalTable: "coupons",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_orders_duration",
                        column: x => x.duration_id,
                        principalTable: "program_durations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_orders_duration_price",
                        column: x => x.duration_price_id,
                        principalTable: "duration_prices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_orders_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refunds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    refund_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    gateway_refund_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    initiated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refunds", x => x.id);
                    table.ForeignKey(
                        name: "fk_refunds_initiated_by",
                        column: x => x.initiated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_refunds_order",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_program_access",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    program_id = table.Column<Guid>(type: "uuid", nullable: false),
                    duration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expert_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reminder_sent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_program_access", x => x.id);
                    table.ForeignKey(
                        name: "fk_upa_duration",
                        column: x => x.duration_id,
                        principalTable: "program_durations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_upa_expert",
                        column: x => x.expert_id,
                        principalTable: "experts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_upa_order",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_upa_program",
                        column: x => x.program_id,
                        principalTable: "programs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_upa_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "expert_progress_updates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    access_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expert_id = table.Column<Guid>(type: "uuid", nullable: false),
                    update_note = table.Column<string>(type: "text", nullable: false),
                    send_email = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expert_progress_updates", x => x.id);
                    table.ForeignKey(
                        name: "fk_epu_access",
                        column: x => x.access_id,
                        principalTable: "user_program_access",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_epu_expert",
                        column: x => x.expert_id,
                        principalTable: "experts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "guided_domains",
                columns: new[] { "id", "created_at", "is_active", "name", "slug", "sort_order", "updated_at" },
                values: new object[] { new Guid("11111111-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "Guided 1:1 Care", "guided-1-1-care", 1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { (short)1, "Admin" },
                    { (short)2, "Expert" },
                    { (short)3, "User" }
                });

            migrationBuilder.InsertData(
                table: "guided_categories",
                columns: new[] { "id", "category_type", "created_at", "cta_label", "cta_link", "domain_id", "hero_subtext", "hero_title", "image_url", "is_active", "name", "page_header", "parent_id", "slug", "sort_order", "updated_at" },
                values: new object[,]
                {
                    { new Guid("22222222-0000-0000-0000-000000000001"), "Hormonal Health Support", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Book Your Program", "/guided/hormonal-health-support", new Guid("11111111-0000-0000-0000-000000000001"), "When hormonal changes feel overwhelming and online advice leaves you confused, you deserve guidance you can trust. Get one-to-one support from experienced practitioners and create a personalised wellness plan that fits your life, accessible from anywhere.", "Get Guided Hormonal Care", null, true, "Hormonal Health Support", "Choose and book the guided journey that best fits your needs, goals, and life right now.", null, "hormonal-health-support", 1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-0000-0000-0000-000000000002"), "Mind and Spirituality", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Book Your Program", "/guided/mental-spiritual-wellbeing", new Guid("11111111-0000-0000-0000-000000000001"), "When constant advice and quick fixes leave you feeling overwhelmed, the right guidance helps you slow down. Get one-to-one support from experienced counsellors and spiritual practitioners to find emotional clarity and inner balance, from the comfort of your home.", "Begin Your Personal Mind Support", null, true, "Mental and Spiritual Wellbeing", "Choose and book the guided journey that best fits your needs, goals, and life right now.", null, "mental-spiritual-wellbeing", 2, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-0000-0000-0000-000000000003"), "Longevity", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Book Your Program", "/guided/longevity-healthy-ageing", new Guid("11111111-0000-0000-0000-000000000001"), "When longevity trends and conflicting wellness advice leave you confused, the right guidance brings clarity. Work one-to-one with experienced experts to create a personalised longevity plan rooted in science, lifestyle, and prevention, accessible from home.", "Plan Your Long-Term Health", null, true, "Longevity and Healthy Ageing", "Choose and book the guided journey that best fits your needs, goals, and life right now.", null, "longevity-healthy-ageing", 3, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-0000-0000-0000-000000000004"), "Fitness and Body Care", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Book Your Program", "/guided/fitness-personal-care", new Guid("11111111-0000-0000-0000-000000000001"), "When online fitness advice leaves you unsure what your body truly needs, personalised guidance makes the difference. Get one-to-one support to build a fitness plan that respects your strength, recovery, and rhythm, from the comfort of your home.", "Book Your Personal Wellness Program", null, true, "Fitness and Personal Care Support", "Choose and book the guided journey that best fits your needs, goals, and life right now.", null, "fitness-personal-care", 4, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "id", "country_dial_code", "country_iso_code", "created_at", "email", "first_name", "full_mobile", "is_active", "is_email_verified", "last_name", "mobile_number", "password_hash", "role_id", "updated_at" },
                values: new object[,]
                {
                    { new Guid("33333333-0000-0000-0000-000000000001"), "+91", "IN", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "prathima@femved.com", "Prathima", null, true, true, "Nagesh", null, "$2a$12$PLACEHOLDER_HASH_PRATHIMA_CHANGE_ON_FIRST_LOGIN", (short)2, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("33333333-0000-0000-0000-000000000002"), "+44", "GB", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "kimberly@femved.com", "Kimberly", null, true, true, "Parsons", null, "$2a$12$PLACEHOLDER_HASH_KIMBERLY_CHANGE_ON_FIRST_LOGIN", (short)2, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "category_key_areas",
                columns: new[] { "id", "area_text", "category_id", "sort_order" },
                values: new object[,]
                {
                    { new Guid("bbbbbbbb-0001-0000-0000-000000000001"), "Preconception and fertility nutrition", new Guid("22222222-0000-0000-0000-000000000001"), 1 },
                    { new Guid("bbbbbbbb-0001-0000-0000-000000000002"), "Pregnancy and postpartum support", new Guid("22222222-0000-0000-0000-000000000001"), 2 },
                    { new Guid("bbbbbbbb-0001-0000-0000-000000000003"), "PCOS, endometriosis, and cycle health", new Guid("22222222-0000-0000-0000-000000000001"), 3 },
                    { new Guid("bbbbbbbb-0001-0000-0000-000000000004"), "Hormone and stress balance", new Guid("22222222-0000-0000-0000-000000000001"), 4 },
                    { new Guid("bbbbbbbb-0001-0000-0000-000000000005"), "Intuitive eating and metabolic health", new Guid("22222222-0000-0000-0000-000000000001"), 5 },
                    { new Guid("bbbbbbbb-0001-0000-0000-000000000006"), "Menopause and perimenopause care", new Guid("22222222-0000-0000-0000-000000000001"), 6 },
                    { new Guid("bbbbbbbb-0001-0000-0000-000000000007"), "Diabetes and weight management", new Guid("22222222-0000-0000-0000-000000000001"), 7 },
                    { new Guid("bbbbbbbb-0001-0000-0000-000000000008"), "Life stage hormonal guidance", new Guid("22222222-0000-0000-0000-000000000001"), 8 }
                });

            migrationBuilder.InsertData(
                table: "category_whats_included",
                columns: new[] { "id", "category_id", "item_text", "sort_order" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-0001-0000-0000-000000000001"), new Guid("22222222-0000-0000-0000-000000000001"), "In-depth health assessment with a hormonal wellness expert", 1 },
                    { new Guid("aaaaaaaa-0001-0000-0000-000000000002"), new Guid("22222222-0000-0000-0000-000000000001"), "One-to-one guidance and ongoing support", 2 },
                    { new Guid("aaaaaaaa-0001-0000-0000-000000000003"), new Guid("22222222-0000-0000-0000-000000000001"), "Personalised 4–12 week wellness plan", 3 },
                    { new Guid("aaaaaaaa-0001-0000-0000-000000000004"), new Guid("22222222-0000-0000-0000-000000000001"), "Care tailored to your hormones, life stage, and goals", 4 },
                    { new Guid("aaaaaaaa-0001-0000-0000-000000000005"), new Guid("22222222-0000-0000-0000-000000000001"), "Customised diet and lifestyle plan with shopping guidance", 5 },
                    { new Guid("aaaaaaaa-0001-0000-0000-000000000006"), new Guid("22222222-0000-0000-0000-000000000001"), "Support for concerns like PCOS, endometriosis, fertility, and cycle health", 6 }
                });

            migrationBuilder.InsertData(
                table: "experts",
                columns: new[] { "id", "bio", "created_at", "credentials", "display_name", "is_active", "location_country", "profile_image_url", "short_bio", "specialisations", "title", "updated_at", "user_id", "years_experience" },
                values: new object[,]
                {
                    { new Guid("44444444-0000-0000-0000-000000000001"), "Dr. Prathima is a distinguished BAMS, MD Ayurvedic physician with over 25 years of clinical experience, specialising in women's health and holistic well-being. A trained Clinical Researcher (GCSRT) from Harvard Medical School, she blends classical Ayurvedic wisdom with evidence-informed clinical practice. Over the years, she has successfully supported women through complex health challenges including menstrual disorders, hormonal imbalances, fertility concerns, and chronic lifestyle-related conditions.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new[] { "BAMS", "MD Ayurveda", "GCSRT - Harvard Medical School" }, "Dr. Prathima Nagesh", true, "India", null, "BAMS, MD Ayurvedic physician with 25+ years of experience in women's hormonal health and Ayurvedic medicine.", new[] { "Hormonal Health", "Ayurveda", "Women's Wellness", "Perimenopause", "PCOS", "Fertility" }, "Ayurvedic Physician & Women's Health Specialist", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("33333333-0000-0000-0000-000000000001"), (short)25 },
                    { new Guid("44444444-0000-0000-0000-000000000002"), "Kimberly Parsons is the founder of Naturalli.me, an all-natural clinic focused on women's hormonal health. Australian-born and trained, she holds a Bachelor of Health Science in Naturopathy and brings over 20 years of experience supporting women through herbal medicine, nutrition, and lifestyle care. She is the internationally best-selling author of The Yoga Kitchen series and creator of the Naturalli 28Days app. Kimberly has led wellness retreats around the world, sharing her philosophy of healing through food, herbs, and rhythm-based living.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new[] { "Bachelor of Health Science in Naturopathy" }, "Kimberly Parsons", true, "United Kingdom", null, "Naturopath and herbalist with 20+ years experience. Best-selling author of The Yoga Kitchen series. Founder of Naturalli.me.", new[] { "Naturopathy", "Herbal Medicine", "PCOS", "Hormonal Health", "Metabolism", "Menopause" }, "Naturopath, Herbalist & Author", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("33333333-0000-0000-0000-000000000002"), (short)20 }
                });

            migrationBuilder.InsertData(
                table: "programs",
                columns: new[] { "id", "category_id", "created_at", "end_date", "expert_id", "grid_description", "grid_image_url", "is_active", "name", "overview", "slug", "sort_order", "start_date", "status", "updated_at" },
                values: new object[,]
                {
                    { new Guid("55555555-0000-0000-0000-000000000001"), new Guid("22222222-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("44444444-0000-0000-0000-000000000001"), "A 6-week Ayurvedic program to reset stress patterns, restore hormonal balance, and rebuild daily rhythm.", null, true, "Break the Stress–Hormone–Health Triangle", "Did you know that chronic stress can quietly disrupt hormonal balance, digestion, sleep, and long-term vitality? In this 6-week guided program, you will move through structured, personalised phases designed to regulate the stress response, improve digestion and hormone metabolism, nourish reproductive and adrenal health, and restore circadian rhythm. Each phase introduces practical Ayurvedic lifestyle tools, self-care rituals, dietary guidance, and stress-regulation techniques that fit into everyday life.", "break-stress-hormone-health-triangle", 1, null, "PUBLISHED", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("55555555-0000-0000-0000-000000000002"), new Guid("22222222-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("44444444-0000-0000-0000-000000000001"), "A 4-week Ayurvedic program to stabilise hormonal transitions, support emotional balance, and strengthen long-term vitality.", null, true, "Balancing & Managing Perimenopause with Ayurveda", "In Ayurveda, perimenopause reflects natural changes in reproductive tissues and dosha balance, often marked by fluctuations in Vata and Pitta. In this 4-week guided program, you will move through personalised Ayurvedic phases designed to understand perimenopausal changes, regulate hormonal fluctuations, support digestion and tissue nourishment, and establish lifestyle rhythms that ease this transition.", "balancing-perimenopause-ayurveda", 2, null, "PUBLISHED", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("55555555-0000-0000-0000-000000000003"), new Guid("22222222-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("44444444-0000-0000-0000-000000000002"), "An 8-week naturopath-led program to restore metabolic balance, regulate hormones, and support fertility.", null, true, "The Metabolic PCOS Reset", "PCOS is not just a reproductive condition but a metabolic and hormonal imbalance often driven by insulin resistance, androgen excess, inflammation, and chronic stress. In this 8-week program, you will work through a structured, personalised approach designed to identify the metabolic drivers behind your PCOS and reverse them using herbal medicine, nutrition therapy, and hormone-supportive lifestyle strategies.", "metabolic-pcos-reset", 3, null, "PUBLISHED", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("55555555-0000-0000-0000-000000000004"), new Guid("22222222-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("44444444-0000-0000-0000-000000000002"), "A 28-day food-based protocol to balance hormones, reset metabolism, and restore energy stability during midlife.", null, true, "28-Day Mastering Midlife Metabolism Method", "Weight gain, fatigue, and metabolic slowdown during midlife are rarely just about calories or exercise. As women transition through perimenopause and menopause, natural shifts in oestrogen, progesterone, insulin, and cortisol significantly impact metabolism. This method integrates cycle-synced nutrition, strategic fasting windows, seed cycling, and lifestyle rhythm correction to support hormonal balance without extreme dieting.", "28-day-midlife-metabolism-method", 4, null, "PUBLISHED", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("55555555-0000-0000-0000-000000000005"), new Guid("22222222-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("44444444-0000-0000-0000-000000000002"), "An 8-week root-cause naturopath program to rebalance hormones, restore energy, and reclaim your natural flow.", null, true, "The Happy Hormone Method", "Hormonal symptoms like PMS, bloating, mood swings, fatigue, and sleep disruption are often signals of deeper imbalances involving inflammation, gut health, stress hormones, and nutrient deficiencies. In this 8-week 1:1 program, you will follow a structured, personalised treatment pathway to identify root causes and support healing through targeted herbal medicine, nutrition therapy, and lifestyle rhythm correction.", "happy-hormone-method", 5, null, "PUBLISHED", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "program_durations",
                columns: new[] { "id", "created_at", "is_active", "label", "program_id", "sort_order", "updated_at", "weeks" },
                values: new object[,]
                {
                    { new Guid("66666666-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "6 weeks", new Guid("55555555-0000-0000-0000-000000000001"), 1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), (short)6 },
                    { new Guid("66666666-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "4 weeks", new Guid("55555555-0000-0000-0000-000000000002"), 1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), (short)4 },
                    { new Guid("66666666-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "8 weeks", new Guid("55555555-0000-0000-0000-000000000003"), 1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), (short)8 },
                    { new Guid("66666666-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "4 weeks", new Guid("55555555-0000-0000-0000-000000000004"), 1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), (short)4 },
                    { new Guid("66666666-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "8 weeks", new Guid("55555555-0000-0000-0000-000000000005"), 1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), (short)8 }
                });

            migrationBuilder.InsertData(
                table: "program_tags",
                columns: new[] { "id", "program_id", "tag" },
                values: new object[,]
                {
                    { new Guid("eeeeeeee-0001-0001-0000-000000000001"), new Guid("55555555-0000-0000-0000-000000000001"), "stress" },
                    { new Guid("eeeeeeee-0001-0002-0000-000000000001"), new Guid("55555555-0000-0000-0000-000000000001"), "hormones" },
                    { new Guid("eeeeeeee-0001-0003-0000-000000000001"), new Guid("55555555-0000-0000-0000-000000000001"), "ayurveda" },
                    { new Guid("eeeeeeee-0001-0004-0000-000000000001"), new Guid("55555555-0000-0000-0000-000000000001"), "sleep" },
                    { new Guid("eeeeeeee-0002-0001-0000-000000000001"), new Guid("55555555-0000-0000-0000-000000000002"), "perimenopause" },
                    { new Guid("eeeeeeee-0002-0002-0000-000000000001"), new Guid("55555555-0000-0000-0000-000000000002"), "hormones" },
                    { new Guid("eeeeeeee-0002-0003-0000-000000000001"), new Guid("55555555-0000-0000-0000-000000000002"), "ayurveda" },
                    { new Guid("eeeeeeee-0002-0004-0000-000000000001"), new Guid("55555555-0000-0000-0000-000000000002"), "ageing" },
                    { new Guid("eeeeeeee-0003-0001-0000-000000000001"), new Guid("55555555-0000-0000-0000-000000000003"), "pcos" },
                    { new Guid("eeeeeeee-0003-0002-0000-000000000001"), new Guid("55555555-0000-0000-0000-000000000003"), "hormones" },
                    { new Guid("eeeeeeee-0003-0003-0000-000000000001"), new Guid("55555555-0000-0000-0000-000000000003"), "fertility" },
                    { new Guid("eeeeeeee-0003-0004-0000-000000000001"), new Guid("55555555-0000-0000-0000-000000000003"), "metabolism" },
                    { new Guid("eeeeeeee-0004-0001-0000-000000000001"), new Guid("55555555-0000-0000-0000-000000000004"), "menopause" },
                    { new Guid("eeeeeeee-0004-0002-0000-000000000001"), new Guid("55555555-0000-0000-0000-000000000004"), "metabolism" },
                    { new Guid("eeeeeeee-0004-0003-0000-000000000001"), new Guid("55555555-0000-0000-0000-000000000004"), "weight" },
                    { new Guid("eeeeeeee-0004-0004-0000-000000000001"), new Guid("55555555-0000-0000-0000-000000000004"), "hormones" },
                    { new Guid("eeeeeeee-0005-0001-0000-000000000001"), new Guid("55555555-0000-0000-0000-000000000005"), "hormones" },
                    { new Guid("eeeeeeee-0005-0002-0000-000000000001"), new Guid("55555555-0000-0000-0000-000000000005"), "gut-health" },
                    { new Guid("eeeeeeee-0005-0003-0000-000000000001"), new Guid("55555555-0000-0000-0000-000000000005"), "pms" },
                    { new Guid("eeeeeeee-0005-0004-0000-000000000001"), new Guid("55555555-0000-0000-0000-000000000005"), "energy" }
                });

            migrationBuilder.InsertData(
                table: "program_testimonials",
                columns: new[] { "id", "created_at", "is_active", "program_id", "rating", "review_text", "reviewer_name", "reviewer_title", "sort_order" },
                values: new object[,]
                {
                    { new Guid("ffffffff-0001-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, new Guid("55555555-0000-0000-0000-000000000001"), (short)5, "After years of irregular cycles and unexplained fatigue, Dr. Prathima's program completely changed how I understand my own body. The Ayurvedic approach felt deeply personal and actually worked.", "Riya S.", "Marketing Professional, Mumbai", 1 },
                    { new Guid("ffffffff-0003-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, new Guid("55555555-0000-0000-0000-000000000003"), (short)5, "The PCOS Reset program was the first time someone treated my PCOS as a metabolic condition and not just a hormonal one. Six months on and my cycles are regular for the first time in three years.", "Meera T.", "Software Engineer, Bangalore", 1 }
                });

            migrationBuilder.InsertData(
                table: "program_what_you_get",
                columns: new[] { "id", "item_text", "program_id", "sort_order" },
                values: new object[,]
                {
                    { new Guid("cccccccc-0001-0000-0000-000000000001"), "Personalised Dosha, lifestyle, and hormonal pattern assessment", new Guid("55555555-0000-0000-0000-000000000001"), 1 },
                    { new Guid("cccccccc-0001-0000-0000-000000000002"), "Step-by-step weekly structure to regulate stress and support hormone balance", new Guid("55555555-0000-0000-0000-000000000001"), 2 },
                    { new Guid("cccccccc-0001-0000-0000-000000000003"), "Ayurvedic self-care rituals including daily rhythm and nervous system calming practices", new Guid("55555555-0000-0000-0000-000000000001"), 3 },
                    { new Guid("cccccccc-0001-0000-0000-000000000004"), "Practical breathwork and relaxation techniques to stabilise stress hormones", new Guid("55555555-0000-0000-0000-000000000001"), 4 },
                    { new Guid("cccccccc-0001-0000-0000-000000000005"), "Hormone-supportive dietary and digestion strengthening guidelines", new Guid("55555555-0000-0000-0000-000000000001"), 5 },
                    { new Guid("cccccccc-0001-0000-0000-000000000006"), "Long-term maintenance plan to sustain hormonal stability", new Guid("55555555-0000-0000-0000-000000000001"), 6 },
                    { new Guid("cccccccc-0003-0000-0000-000000000001"), "Personalised herbal tincture formulas designed to address your PCOS root cause", new Guid("55555555-0000-0000-0000-000000000003"), 1 },
                    { new Guid("cccccccc-0003-0000-0000-000000000002"), "Custom supplement protocol to support metabolic and hormonal balance", new Guid("55555555-0000-0000-0000-000000000003"), 2 },
                    { new Guid("cccccccc-0003-0000-0000-000000000003"), "Tailored 5-day metabolic cleanse to reset blood sugar and inflammation pathways", new Guid("55555555-0000-0000-0000-000000000003"), 3 },
                    { new Guid("cccccccc-0003-0000-0000-000000000004"), "28-day hormone-supportive metabolic meal plan", new Guid("55555555-0000-0000-0000-000000000003"), 4 },
                    { new Guid("cccccccc-0003-0000-0000-000000000005"), "Identification of your PCOS subtype: insulin-driven, inflammatory, or androgen-dominant", new Guid("55555555-0000-0000-0000-000000000003"), 5 }
                });

            migrationBuilder.InsertData(
                table: "program_who_is_this_for",
                columns: new[] { "id", "item_text", "program_id", "sort_order" },
                values: new object[,]
                {
                    { new Guid("dddddddd-0001-0000-0000-000000000001"), "Women experiencing hormonal imbalance, irregular cycles, PMS, or fatigue", new Guid("55555555-0000-0000-0000-000000000001"), 1 },
                    { new Guid("dddddddd-0001-0000-0000-000000000002"), "Individuals dealing with chronic stress, burnout, or sleep disturbances", new Guid("55555555-0000-0000-0000-000000000001"), 2 },
                    { new Guid("dddddddd-0001-0000-0000-000000000003"), "Women navigating thyroid, metabolic, or adrenal health concerns", new Guid("55555555-0000-0000-0000-000000000001"), 3 },
                    { new Guid("dddddddd-0001-0000-0000-000000000004"), "Anyone wanting structured, sustainable lifestyle tools rooted in Ayurveda", new Guid("55555555-0000-0000-0000-000000000001"), 4 },
                    { new Guid("dddddddd-0003-0000-0000-000000000001"), "Women aged 25–40 diagnosed with PCOS or strongly suspecting PCOS", new Guid("55555555-0000-0000-0000-000000000003"), 1 },
                    { new Guid("dddddddd-0003-0000-0000-000000000002"), "Women experiencing irregular or absent cycles, fertility challenges, or hormonal acne", new Guid("55555555-0000-0000-0000-000000000003"), 2 },
                    { new Guid("dddddddd-0003-0000-0000-000000000003"), "Individuals struggling with stubborn weight gain, sugar cravings, or insulin resistance", new Guid("55555555-0000-0000-0000-000000000003"), 3 },
                    { new Guid("dddddddd-0003-0000-0000-000000000004"), "Women preparing for conception and seeking to optimise reproductive health", new Guid("55555555-0000-0000-0000-000000000003"), 4 }
                });

            migrationBuilder.InsertData(
                table: "duration_prices",
                columns: new[] { "id", "amount", "created_at", "currency_code", "currency_symbol", "duration_id", "is_active", "location_code", "updated_at" },
                values: new object[,]
                {
                    { new Guid("77777777-0001-0001-0000-000000000001"), 400.00m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "USD", "$", new Guid("66666666-0000-0000-0000-000000000001"), true, "US", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("77777777-0001-0002-0000-000000000001"), 320.00m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "GBP", "£", new Guid("66666666-0000-0000-0000-000000000001"), true, "GB", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("77777777-0001-0003-0000-000000000001"), 33000.00m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "INR", "₹", new Guid("66666666-0000-0000-0000-000000000001"), true, "IN", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("77777777-0002-0001-0000-000000000001"), 350.00m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "USD", "$", new Guid("66666666-0000-0000-0000-000000000002"), true, "US", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("77777777-0002-0002-0000-000000000001"), 280.00m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "GBP", "£", new Guid("66666666-0000-0000-0000-000000000002"), true, "GB", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("77777777-0002-0003-0000-000000000001"), 29000.00m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "INR", "₹", new Guid("66666666-0000-0000-0000-000000000002"), true, "IN", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("77777777-0003-0001-0000-000000000001"), 879.00m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "GBP", "£", new Guid("66666666-0000-0000-0000-000000000003"), true, "GB", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("77777777-0003-0002-0000-000000000001"), 1099.00m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "USD", "$", new Guid("66666666-0000-0000-0000-000000000003"), true, "US", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("77777777-0003-0003-0000-000000000001"), 90000.00m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "INR", "₹", new Guid("66666666-0000-0000-0000-000000000003"), true, "IN", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("77777777-0004-0001-0000-000000000001"), 499.00m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "GBP", "£", new Guid("66666666-0000-0000-0000-000000000004"), true, "GB", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("77777777-0004-0002-0000-000000000001"), 625.00m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "USD", "$", new Guid("66666666-0000-0000-0000-000000000004"), true, "US", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("77777777-0004-0003-0000-000000000001"), 51000.00m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "INR", "₹", new Guid("66666666-0000-0000-0000-000000000004"), true, "IN", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("77777777-0005-0001-0000-000000000001"), 899.00m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "GBP", "£", new Guid("66666666-0000-0000-0000-000000000005"), true, "GB", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("77777777-0005-0002-0000-000000000001"), 1125.00m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "USD", "$", new Guid("66666666-0000-0000-0000-000000000005"), true, "US", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("77777777-0005-0003-0000-000000000001"), 92000.00m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "INR", "₹", new Guid("66666666-0000-0000-0000-000000000005"), true, "IN", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "idx_admin_audit_log_admin_user_id",
                table: "admin_audit_log",
                column: "admin_user_id");

            migrationBuilder.CreateIndex(
                name: "idx_admin_audit_log_created_at",
                table: "admin_audit_log",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_admin_audit_log_entity_type_entity_id",
                table: "admin_audit_log",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "idx_category_key_areas_category_id",
                table: "category_key_areas",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "idx_category_whats_included_category_id",
                table: "category_whats_included",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "uq_coupons_code",
                table: "coupons",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_duration_prices_duration_location",
                table: "duration_prices",
                columns: new[] { "duration_id", "location_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_epu_access_id",
                table: "expert_progress_updates",
                column: "access_id");

            migrationBuilder.CreateIndex(
                name: "IX_expert_progress_updates_expert_id",
                table: "expert_progress_updates",
                column: "expert_id");

            migrationBuilder.CreateIndex(
                name: "idx_experts_user_id",
                table: "experts",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_gdpr_deletion_requests_status",
                table: "gdpr_deletion_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_gdpr_deletion_requests_user_id",
                table: "gdpr_deletion_requests",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gdpr_deletion_requests_processed_by",
                table: "gdpr_deletion_requests",
                column: "processed_by");

            migrationBuilder.CreateIndex(
                name: "idx_guided_categories_domain_id",
                table: "guided_categories",
                column: "domain_id");

            migrationBuilder.CreateIndex(
                name: "idx_guided_categories_parent_id",
                table: "guided_categories",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "uq_guided_categories_slug",
                table: "guided_categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_guided_domains_slug",
                table: "guided_domains",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_notification_log_type",
                table: "notification_log",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "idx_notification_log_user_id",
                table: "notification_log",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_orders_created_at",
                table: "orders",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_orders_status",
                table: "orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_orders_user_id",
                table: "orders",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_coupon_id",
                table: "orders",
                column: "coupon_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_duration_id",
                table: "orders",
                column: "duration_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_duration_price_id",
                table: "orders",
                column: "duration_price_id");

            migrationBuilder.CreateIndex(
                name: "uq_orders_idempotency_key",
                table: "orders",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_password_reset_tokens_user_id",
                table: "password_reset_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_program_durations_program_id",
                table: "program_durations",
                column: "program_id");

            migrationBuilder.CreateIndex(
                name: "idx_program_tags_program_id",
                table: "program_tags",
                column: "program_id");

            migrationBuilder.CreateIndex(
                name: "idx_program_tags_tag",
                table: "program_tags",
                column: "tag");

            migrationBuilder.CreateIndex(
                name: "uq_program_tags_program_tag",
                table: "program_tags",
                columns: new[] { "program_id", "tag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_program_testimonials_program_id",
                table: "program_testimonials",
                column: "program_id");

            migrationBuilder.CreateIndex(
                name: "idx_program_what_you_get_program_id",
                table: "program_what_you_get",
                column: "program_id");

            migrationBuilder.CreateIndex(
                name: "idx_program_who_is_this_for_program_id",
                table: "program_who_is_this_for",
                column: "program_id");

            migrationBuilder.CreateIndex(
                name: "idx_programs_category_id",
                table: "programs",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "idx_programs_expert_id",
                table: "programs",
                column: "expert_id");

            migrationBuilder.CreateIndex(
                name: "idx_programs_status",
                table: "programs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "uq_programs_slug",
                table: "programs",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_refunds_order_id",
                table: "refunds",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_refunds_initiated_by",
                table: "refunds",
                column: "initiated_by");

            migrationBuilder.CreateIndex(
                name: "idx_upa_expert_id",
                table: "user_program_access",
                column: "expert_id");

            migrationBuilder.CreateIndex(
                name: "idx_upa_program_id",
                table: "user_program_access",
                column: "program_id");

            migrationBuilder.CreateIndex(
                name: "idx_upa_status",
                table: "user_program_access",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_upa_user_id",
                table: "user_program_access",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_program_access_duration_id",
                table: "user_program_access",
                column: "duration_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_program_access_order_id",
                table: "user_program_access",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "idx_users_iso_code",
                table: "users",
                column: "country_iso_code");

            migrationBuilder.CreateIndex(
                name: "idx_users_role_id",
                table: "users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "uq_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_audit_log");

            migrationBuilder.DropTable(
                name: "category_key_areas");

            migrationBuilder.DropTable(
                name: "category_whats_included");

            migrationBuilder.DropTable(
                name: "expert_progress_updates");

            migrationBuilder.DropTable(
                name: "gdpr_deletion_requests");

            migrationBuilder.DropTable(
                name: "notification_log");

            migrationBuilder.DropTable(
                name: "password_reset_tokens");

            migrationBuilder.DropTable(
                name: "program_tags");

            migrationBuilder.DropTable(
                name: "program_testimonials");

            migrationBuilder.DropTable(
                name: "program_what_you_get");

            migrationBuilder.DropTable(
                name: "program_who_is_this_for");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "refunds");

            migrationBuilder.DropTable(
                name: "user_program_access");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "coupons");

            migrationBuilder.DropTable(
                name: "duration_prices");

            migrationBuilder.DropTable(
                name: "program_durations");

            migrationBuilder.DropTable(
                name: "programs");

            migrationBuilder.DropTable(
                name: "guided_categories");

            migrationBuilder.DropTable(
                name: "experts");

            migrationBuilder.DropTable(
                name: "guided_domains");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}
