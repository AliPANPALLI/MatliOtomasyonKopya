namespace MatliOtomasyon.Shared.Application.Menus
{
    public static class AppMenus
    {
        public static readonly List<MenuNode> TesisScada = new()
        {
            new MenuNode
            {
                Title = "Keskinoğlu",
                SubItems = new()
                {
                    new MenuNode { Title="Kesimhane Kampüsü", Link="/tesis-scada/kesimhane-kampusu" },
                    new MenuNode { Title="Viyol Tesisi", Link="/tesis-scada/viyol-tesisi" },
                    new MenuNode { Title="Rahmiye Tasnif", Link="/tesis-scada/rahmiye-tasnif" },
                    new MenuNode { Title="Merkez Tasnif Ünite-1", Link="/tesis-scada/merkez-tasnif-unite1" },
                    new MenuNode { Title="Merkez Tasnif Ünite-2", Link="/tesis-scada/merkez-tasnif-unite2" },
                    new MenuNode { Title="Rendering", Link="/tesis-scada/rendering" },
  }
            }
        };

        // MenuNode:
        // public sealed class MenuNode
        // {
        //     public string Title { get; init; } = default!;
        //     public string? Link { get; init; }
        //     public List<MenuNode>? SubItems { get; init; }
        // }

        public static readonly List<MenuNode> SunucuMcc = new()
        {
            new MenuNode
            {
                Title = "Matlı",
                SubItems = new()
                {
                    new MenuNode
                    {
                        Title = "Genel Merkez",
                        SubItems = new()
                        {
                            new MenuNode
                            {
                                Title = "Sunucu",
                                SubItems = new()
                                {
                                    new MenuNode
                                    {
                                        Title = "Server Odası",
                                        Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/1"
                                    },
                                    new MenuNode
                                    {
                                        Title = "UPS Odası",
                                        Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/2"
                                    },
                                    new MenuNode
                                    {
                                        Title = "Server Odası 2",
                                        Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/3"
                                    }
                                }
                            }
                        }
                    }
                }
            },

            new MenuNode
            {
                Title = "Proyem",
                SubItems = new()
                {
                    new MenuNode
                    {
                        Title = "Karacabey",
                        SubItems = new()
                        {
                            new MenuNode
                            {
                                Title = "Sunucu",
                                Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/4"
                            },
                            new MenuNode
                            {
                                Title = "MCC",
                                SubItems = new()
                                {
                                    new MenuNode { Title = "Büyükbaş Tesisi", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/5" },
                                    new MenuNode { Title = "Kanatlı Tesisi", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/6" },
                                    new MenuNode { Title = "Soya Tesisi", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/7" },
                                    new MenuNode { Title = "Kömürlü Kazan", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/8" },
                                    new MenuNode { Title = "Yeni Çelik Silo Tesisi", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/9" },
                                    new MenuNode { Title = "Flake Tesisi", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/10" },
                                }
                            }
                        }
                    },

                    new MenuNode
                    {
                        Title = "Turgutlu",
                        SubItems = new()
                        {
                            new MenuNode
                            {
                                Title = "Sunucu",
                                Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/11"
                            },
                            new MenuNode
                            {
                                Title = "MCC",
                                SubItems = new()
                                {
                                    new MenuNode { Title = "Üretim", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/12" },
                                    new MenuNode { Title = "Çelik Silo", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/13" },
                                }
                            }
                        }
                    },

                    new MenuNode
                    {
                        Title = "Çorlu",
                        SubItems = new()
                        {
                            new MenuNode
                            {
                                Title = "Sunucu",
                                Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/14"
                            },
                            new MenuNode
                            {
                                Title = "MCC",
                                SubItems = new()
                                {
                                    new MenuNode { Title = "Üretim", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/15" },
                                    new MenuNode { Title = "Çelik Silo", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/16" },
                                }
                            }
                        }
                    },

                    new MenuNode
                    {
                        Title = "Samsun",
                        SubItems = new()
                        {
                            new MenuNode
                            {
                                Title = "Sunucu",
                                Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/17"
                            },
                            new MenuNode
                            {
                                Title = "MCC",
                                SubItems = new()
                                {
                                    new MenuNode { Title = "Ortak", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/18" },
                                    new MenuNode { Title = "ADP", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/19" },
                                }
                            }
                        }
                    },

                    new MenuNode
                    {
                        Title = "Gaziantep",
                        SubItems = new()
                        {
                            new MenuNode
                            {
                                Title = "Sunucu",
                                Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/20"
                            },
                            new MenuNode
                            {
                                Title = "MCC",
                                SubItems = new()
                                {
                                    new MenuNode { Title = "MCC Odası", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/21" },
                                    new MenuNode { Title = "ADP", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/22" },
                                    new MenuNode { Title = "Lidaş", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/23" },
                                }
                            }
                        }
                    },

                    new MenuNode
                    {
                        Title = "Konya",
                        SubItems = new()
                        {
                            new MenuNode
                            {
                                Title = "Sunucu",
                                Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/24"
                            },
                            new MenuNode
                            {
                                Title = "MCC",
                                SubItems = new()
                                {
                                    new MenuNode { Title = "Eski MCC Odası", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/25" },
                                    new MenuNode { Title = "Yeni MCC Odası", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/26" },
                                }
                            }
                        }
                    },

                    new MenuNode
                    {
                        Title = "Burdur",
                        SubItems = new()
                        {
                            new MenuNode
                            {
                                Title = "Sunucu",
                                Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/27"
                            },
                            new MenuNode
                            {
                                Title = "MCC",
                                SubItems = new()
                                {
                                    new MenuNode { Title = "Üretim", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/28" },
                                    new MenuNode { Title = "Çelik Silo", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/29" },
                                }
                            }
                        }
                    },

                    new MenuNode
                    {
                        Title = "Polatlı",
                        SubItems = new()
                        {
                            new MenuNode
                            {
                                Title = "Sunucu",
                                Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/30"
                            },
                            new MenuNode
                            {
                                Title = "MCC",
                                SubItems = new()
                                {
                                    new MenuNode { Title = "Üretim", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/31" },
                                    new MenuNode { Title = "Lidaş-Çelik Silo", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/32" },
                                }
                            }
                        }
                    },

                    new MenuNode
                    {
                        Title = "Diyarbakır",
                        SubItems = new()
                        {
                            new MenuNode
                            {
                                Title = "Sunucu",
                                Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/33"
                            },
                            new MenuNode
                            {
                                Title = "MCC",
                                SubItems = new()
                                {
                                    new MenuNode { Title = "Üretim", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/34" },
                                    new MenuNode { Title = "Çelik Silo", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/35" },
                                }
                            }
                        }
                    },
                }
            },

            new MenuNode
            {
                Title = "Keskinoğlu",
                SubItems = new()
                {
                    new MenuNode
                    {
                        Title = "Kesimhane Kampüsü",
                        SubItems = new()
                        {
                            new MenuNode
                            {
                                Title = "Sunucu",
                                SubItems = new()
                                {
                                    new MenuNode { Title = "Sunucu Odası 1", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/36" },
                                    new MenuNode { Title = "Sunucu Odası 2", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/37" },
                                    new MenuNode { Title = "Sunucu Odası 3", Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/38" },
                                }
                            }
                        }
                    }
                }
            },

            new MenuNode
            {
                Title = "HEG Gıda",
                SubItems = new()
                {
                    new MenuNode
                    {
                        Title = "Merkez",
                        SubItems = new()
                        {
                            new MenuNode
                            {
                                Title = "Sunucu",
                                Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/39"
                            }
                        }
                    }
                }
            },

            new MenuNode
            {
                Title = "Yörsan",
                SubItems = new()
                {
                    new MenuNode
                    {
                        Title = "Merkez",
                        SubItems = new()
                        {
                            new MenuNode
                            {
                                Title = "Sunucu",
                                Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/40"
                            }
                        }
                    }
                }
            },

            new MenuNode
            {
                Title = "Verim Gıda",
                SubItems = new()
                {
                    new MenuNode
                    {
                        Title = "Merkez",
                        SubItems = new()
                        {
                            new MenuNode
                            {
                                Title = "Sunucu",
                                Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/41"
                            }
                        }
                    }
                }
            },

            new MenuNode
            {
                Title = "Sodru",
                SubItems = new()
                {
                    new MenuNode
                    {
                        Title = "Merkez",
                        SubItems = new()
                        {
                            new MenuNode
                            {
                                Title = "Sunucu",
                                Link = "/sunucu-mcc-izleme/sunucu-izleme/oda/42"
                            }
                        }
                    }
                }
            },
        };
    }
}