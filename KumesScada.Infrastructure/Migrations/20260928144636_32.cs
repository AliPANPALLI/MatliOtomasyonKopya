using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KumesScada.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _32 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KumesHayvanDetay",
                columns: table => new
                {
                    KumesHayvanDetayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KumesHayvanDetayAdi = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KumesHayvanDetay", x => x.KumesHayvanDetayId);
                });

            migrationBuilder.CreateTable(
                name: "KumesYumurtaDurum",
                columns: table => new
                {
                    KumesYumurtaDurumId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KumesYumurtaDurumAd = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KumesYumurtaDurum", x => x.KumesYumurtaDurumId);
                });

            migrationBuilder.CreateTable(
                name: "KumesYumurtaKaliteDurum",
                columns: table => new
                {
                    KumesYumurtaKaliteDurumId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KumesYumurtaKaliteDurumAd = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KumesYumurtaKaliteDurum", x => x.KumesYumurtaKaliteDurumId);
                });

            migrationBuilder.CreateTable(
                name: "RaporKategori",
                columns: table => new
                {
                    RaporKategoriId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RaporKategoriAd = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaporKategori", x => x.RaporKategoriId);
                });

            migrationBuilder.CreateTable(
                name: "TasnifMakinesi",
                columns: table => new
                {
                    TasnifMakinesiId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TasnifMakinesiAdi = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TasnifMakinesi", x => x.TasnifMakinesiId);
                });

            migrationBuilder.CreateTable(
                name: "RaporVeri",
                columns: table => new
                {
                    RaporVerilerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RaporKategoriId = table.Column<int>(type: "int", nullable: false),
                    RaporHedefDeger = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaporVeri", x => x.RaporVerilerId);
                    table.ForeignKey(
                        name: "FK_RaporVeri_RaporKategori_RaporKategoriId",
                        column: x => x.RaporKategoriId,
                        principalTable: "RaporKategori",
                        principalColumn: "RaporKategoriId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Kumesler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KumesId = table.Column<int>(type: "int", nullable: true),
                    TasnifMakinesiId = table.Column<int>(type: "int", nullable: true),
                    KumesNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    KumesName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Aktif = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    Silindi = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kumesler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kumesler_TasnifMakinesi_TasnifMakinesiId",
                        column: x => x.TasnifMakinesiId,
                        principalTable: "TasnifMakinesi",
                        principalColumn: "TasnifMakinesiId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CanliAgirlik",
                columns: table => new
                {
                    CanliAgirlikId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CanliAgirlikTarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CanliAgirlikMiktar = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    KumeslerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanliAgirlik", x => x.CanliAgirlikId);
                    table.ForeignKey(
                        name: "FK_CanliAgirlik_Kumesler_KumeslerId",
                        column: x => x.KumeslerId,
                        principalTable: "Kumesler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KaliteGizliCatlak",
                columns: table => new
                {
                    KaliteGizliCatlakId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KaliteGizliCatlakTarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KaliteGizliCatlakMiktar = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    KumeslerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KaliteGizliCatlak", x => x.KaliteGizliCatlakId);
                    table.ForeignKey(
                        name: "FK_KaliteGizliCatlak_Kumesler_KumeslerId",
                        column: x => x.KumeslerId,
                        principalTable: "Kumesler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KaliteMukavemet",
                columns: table => new
                {
                    KaliteMukavemetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KaliteMukavemetTarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KaliteMukavemetMiktar = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    KumeslerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KaliteMukavemet", x => x.KaliteMukavemetId);
                    table.ForeignKey(
                        name: "FK_KaliteMukavemet_Kumesler_KumeslerId",
                        column: x => x.KumeslerId,
                        principalTable: "Kumesler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KumesVeri",
                columns: table => new
                {
                    KumesVeriId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KumeslerId = table.Column<int>(type: "int", nullable: false),
                    SiparisNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    KumesHayvanDetayId = table.Column<int>(type: "int", nullable: true),
                    KumesHayvanDetayDeger = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    KumesYumurtaDurumId = table.Column<int>(type: "int", nullable: true),
                    KumesYumurtaDurumDeger = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    KumesYumurtaKaliteDurumId = table.Column<int>(type: "int", nullable: true),
                    KumesYumurtaKaliteDurumDeger = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KumesVeri", x => x.KumesVeriId);
                    table.ForeignKey(
                        name: "FK_KumesVeri_KumesHayvanDetay_KumesHayvanDetayId",
                        column: x => x.KumesHayvanDetayId,
                        principalTable: "KumesHayvanDetay",
                        principalColumn: "KumesHayvanDetayId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KumesVeri_KumesYumurtaDurum_KumesYumurtaDurumId",
                        column: x => x.KumesYumurtaDurumId,
                        principalTable: "KumesYumurtaDurum",
                        principalColumn: "KumesYumurtaDurumId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KumesVeri_KumesYumurtaKaliteDurum_KumesYumurtaKaliteDurumId",
                        column: x => x.KumesYumurtaKaliteDurumId,
                        principalTable: "KumesYumurtaKaliteDurum",
                        principalColumn: "KumesYumurtaKaliteDurumId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KumesVeri_Kumesler_KumeslerId",
                        column: x => x.KumeslerId,
                        principalTable: "Kumesler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Olum",
                columns: table => new
                {
                    OlumId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OlumTarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OlumMiktar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KumeslerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Olum", x => x.OlumId);
                    table.ForeignKey(
                        name: "FK_Olum_Kumesler_KumeslerId",
                        column: x => x.KumeslerId,
                        principalTable: "Kumesler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Randiman",
                columns: table => new
                {
                    RandimanId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RandimanTarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RandimanMiktar = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    KumeslerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Randiman", x => x.RandimanId);
                    table.ForeignKey(
                        name: "FK_Randiman_Kumesler_KumeslerId",
                        column: x => x.KumeslerId,
                        principalTable: "Kumesler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Su",
                columns: table => new
                {
                    SuId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SuTarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SuMiktar = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    KumeslerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Su", x => x.SuId);
                    table.ForeignKey(
                        name: "FK_Su_Kumesler_KumeslerId",
                        column: x => x.KumeslerId,
                        principalTable: "Kumesler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Uygulama",
                columns: table => new
                {
                    UygulamalarId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UygulamalarTarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UygulamaAciklama = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    KumeslerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uygulama", x => x.UygulamalarId);
                    table.ForeignKey(
                        name: "FK_Uygulama_Kumesler_KumeslerId",
                        column: x => x.KumeslerId,
                        principalTable: "Kumesler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VbapRandiman",
                columns: table => new
                {
                    VbapRandimanId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VbapRandimanTarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VbapRandimanMiktar = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    KumeslerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VbapRandiman", x => x.VbapRandimanId);
                    table.ForeignKey(
                        name: "FK_VbapRandiman_Kumesler_KumeslerId",
                        column: x => x.KumeslerId,
                        principalTable: "Kumesler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Yem",
                columns: table => new
                {
                    YemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    YemTarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    YemMiktar = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    KumeslerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Yem", x => x.YemId);
                    table.ForeignKey(
                        name: "FK_Yem_Kumesler_KumeslerId",
                        column: x => x.KumeslerId,
                        principalTable: "Kumesler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "YumurtaAgirlik",
                columns: table => new
                {
                    YumurtaAgirlikId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    YumurtaAgirlikTarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    YumurtaAgirlikMiktar = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    KumeslerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YumurtaAgirlik", x => x.YumurtaAgirlikId);
                    table.ForeignKey(
                        name: "FK_YumurtaAgirlik_Kumesler_KumeslerId",
                        column: x => x.KumeslerId,
                        principalTable: "Kumesler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CanliAgirlik_KumeslerId",
                table: "CanliAgirlik",
                column: "KumeslerId");

            migrationBuilder.CreateIndex(
                name: "IX_KaliteGizliCatlak_KumeslerId",
                table: "KaliteGizliCatlak",
                column: "KumeslerId");

            migrationBuilder.CreateIndex(
                name: "IX_KaliteMukavemet_KumeslerId",
                table: "KaliteMukavemet",
                column: "KumeslerId");

            migrationBuilder.CreateIndex(
                name: "IX_Kumesler_KumesId",
                table: "Kumesler",
                column: "KumesId");

            migrationBuilder.CreateIndex(
                name: "IX_Kumesler_TasnifMakinesiId",
                table: "Kumesler",
                column: "TasnifMakinesiId");

            migrationBuilder.CreateIndex(
                name: "IX_KumesVeri_KumesHayvanDetayId",
                table: "KumesVeri",
                column: "KumesHayvanDetayId");

            migrationBuilder.CreateIndex(
                name: "IX_KumesVeri_KumeslerId",
                table: "KumesVeri",
                column: "KumeslerId");

            migrationBuilder.CreateIndex(
                name: "IX_KumesVeri_KumesYumurtaDurumId",
                table: "KumesVeri",
                column: "KumesYumurtaDurumId");

            migrationBuilder.CreateIndex(
                name: "IX_KumesVeri_KumesYumurtaKaliteDurumId",
                table: "KumesVeri",
                column: "KumesYumurtaKaliteDurumId");

            migrationBuilder.CreateIndex(
                name: "IX_Olum_KumeslerId",
                table: "Olum",
                column: "KumeslerId");

            migrationBuilder.CreateIndex(
                name: "IX_Randiman_KumeslerId",
                table: "Randiman",
                column: "KumeslerId");

            migrationBuilder.CreateIndex(
                name: "IX_RaporVeri_RaporKategoriId",
                table: "RaporVeri",
                column: "RaporKategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_Su_KumeslerId",
                table: "Su",
                column: "KumeslerId");

            migrationBuilder.CreateIndex(
                name: "IX_Uygulama_KumeslerId",
                table: "Uygulama",
                column: "KumeslerId");

            migrationBuilder.CreateIndex(
                name: "IX_VbapRandiman_KumeslerId",
                table: "VbapRandiman",
                column: "KumeslerId");

            migrationBuilder.CreateIndex(
                name: "IX_Yem_KumeslerId",
                table: "Yem",
                column: "KumeslerId");

            migrationBuilder.CreateIndex(
                name: "IX_YumurtaAgirlik_KumeslerId",
                table: "YumurtaAgirlik",
                column: "KumeslerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CanliAgirlik");

            migrationBuilder.DropTable(
                name: "KaliteGizliCatlak");

            migrationBuilder.DropTable(
                name: "KaliteMukavemet");

            migrationBuilder.DropTable(
                name: "KumesVeri");

            migrationBuilder.DropTable(
                name: "Olum");

            migrationBuilder.DropTable(
                name: "Randiman");

            migrationBuilder.DropTable(
                name: "RaporVeri");

            migrationBuilder.DropTable(
                name: "Su");

            migrationBuilder.DropTable(
                name: "Uygulama");

            migrationBuilder.DropTable(
                name: "VbapRandiman");

            migrationBuilder.DropTable(
                name: "Yem");

            migrationBuilder.DropTable(
                name: "YumurtaAgirlik");

            migrationBuilder.DropTable(
                name: "KumesHayvanDetay");

            migrationBuilder.DropTable(
                name: "KumesYumurtaDurum");

            migrationBuilder.DropTable(
                name: "KumesYumurtaKaliteDurum");

            migrationBuilder.DropTable(
                name: "RaporKategori");

            migrationBuilder.DropTable(
                name: "Kumesler");

            migrationBuilder.DropTable(
                name: "TasnifMakinesi");
        }
    }
}
