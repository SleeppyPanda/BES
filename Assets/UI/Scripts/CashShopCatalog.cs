using System.Collections.Generic;

namespace BES.UI
{
    public enum CashShopTab
    {
        DiamondPurchase,
        GoldenExchange,
        SolarPeaExchange,
        TidalPeaExchange,
        PackShop,
        LightPurchase
    }

    public readonly struct CashShopProduct
    {
        public readonly string id;
        public readonly string spriteName;

        public CashShopProduct(string id, string spriteName)
        {
            this.id = id;
            this.spriteName = spriteName;
        }
    }

    public static class CashShopCatalog
    {
        static readonly CashShopProduct[] DiamondProducts =
        {
            new("diamond_60", "Group 427322977"),
            new("diamond_300", "Group 427322978"),
            new("diamond_980", "Group 427322979"),
            new("diamond_1980", "Group 427322980"),
            new("diamond_3280", "Group 427322982"),
            new("diamond_6480", "Group 427322981")
        };

        static readonly CashShopProduct[] GoldenProducts =
        {
            new("gold_ocean_echoes", "Group 427322986"),
            new("gold_plant_echoes", "Group 427322985"),
            new("gold_gift_stars", "Group 427322984"),
            new("gold_gift_bottle", "Group 427322983")
        };

        static readonly CashShopProduct[] SolarProducts =
        {
            new("solar_celestial_light", "Group 427322952"),
            new("solar_wanderer_light", "Group 427322951")
        };

        static readonly CashShopProduct[] TidalProducts =
        {
            new("tidal_celestial_light", "Group 427322966"),
            new("tidal_wanderer_light", "Group 427322960"),
            new("tidal_sky_memories", "Group 427322967"),
            new("tidal_forest_memories", "Group 427322968"),
            new("tidal_ocean_echoes", "Group 427322969"),
            new("tidal_plant_echoes", "Group 427322970"),
            new("tidal_gift_stars", "Group 427322973"),
            new("tidal_gift_bottle", "Group 427322972")
        };

        static readonly CashShopProduct[] PackProducts =
        {
            new("starter_packed", "Group 427322974")
        };

        static readonly CashShopProduct[] LightProducts =
        {
            new("light_wanderer", "Group 427322951"),
            new("light_celestial", "Group 427322952"),
            new("light_contract", "Group 4273229561")
        };

        public static string TitleFor(CashShopTab tab) => tab switch
        {
            CashShopTab.DiamondPurchase => "Diamond Purchase",
            CashShopTab.GoldenExchange => "Golden Exchange",
            CashShopTab.SolarPeaExchange => "Exchange SHOP - Solar Pea Exchange",
            CashShopTab.TidalPeaExchange => "Tidal Pea Exchange",
            CashShopTab.PackShop => "Pack Shop",
            CashShopTab.LightPurchase => "Light Purchase",
            _ => "Cash Shop"
        };

        public static IReadOnlyList<CashShopProduct> ProductsFor(CashShopTab tab) => tab switch
        {
            CashShopTab.DiamondPurchase => DiamondProducts,
            CashShopTab.GoldenExchange => GoldenProducts,
            CashShopTab.SolarPeaExchange => SolarProducts,
            CashShopTab.TidalPeaExchange => TidalProducts,
            CashShopTab.PackShop => PackProducts,
            CashShopTab.LightPurchase => LightProducts,
            _ => DiamondProducts
        };
    }
}
