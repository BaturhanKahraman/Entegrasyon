namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontSettings : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    // Theme & Branding
    public int ThemeId { get; set; } = 1;
    public string StoreName { get; set; } = null!;
    public string? StoreSlogan { get; set; }
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string PrimaryColor { get; set; } = "#2563EB";
    public string? SecondaryColor { get; set; }
    public string? AccentColor { get; set; }
    public string? CustomCss { get; set; }

    // Company Info
    public string CompanyName { get; set; } = null!;
    public string CompanyTaxOffice { get; set; } = null!;
    public string CompanyTaxNumber { get; set; } = null!;
    public string? MersisNumber { get; set; }
    public string? KepAddress { get; set; }

    // Contact
    public string ContactPhone { get; set; } = null!;
    public string? WhatsAppNumber { get; set; }
    public string ContactEmail { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string City { get; set; } = null!;
    public string? District { get; set; }

    // Social Media
    public string? InstagramUrl { get; set; }
    public string? FacebookUrl { get; set; }
    public string? TwitterUrl { get; set; }
    public string? YouTubeUrl { get; set; }
    public string? TikTokUrl { get; set; }

    // Analytics
    public string? GoogleAnalyticsId { get; set; }
    public string? GoogleTagManagerId { get; set; }
    public string? FacebookPixelId { get; set; }

    // Announcement Bar
    public string? AnnouncementBarText { get; set; }
    public bool AnnouncementBarActive { get; set; }
    public string? AnnouncementBarColor { get; set; }

    // Legal Pages (HTML)
    public string? AboutHtml { get; set; }
    public string? ReturnPolicyHtml { get; set; }
    public string? PrivacyPolicyHtml { get; set; }
    public string? TermsHtml { get; set; }
    public string? KvkkHtml { get; set; }
    public string? CookiePolicyHtml { get; set; }
    public string? DistanceSalesContractHtml { get; set; }
    public string? PreInfoFormHtml { get; set; }
    public string? DeliveryTermsHtml { get; set; }

    // SEO Defaults
    public string? DefaultSeoTitle { get; set; }
    public string? DefaultSeoDescription { get; set; }
    public string? DefaultSeoKeywords { get; set; }

    // Shipping
    public decimal FreeShippingThreshold { get; set; }
    public decimal FlatShippingRate { get; set; }
    public int EstimatedDeliveryDays { get; set; } = 3;

    // Feature Flags
    public bool IsMaintenanceMode { get; set; }
    public string? MaintenanceMessage { get; set; }
    public bool CookieConsentActive { get; set; } = true;
    public bool IsWhatsAppWidgetActive { get; set; }
    public bool NewsletterEnabled { get; set; }

    // Auto Campaigns
    public bool AutoWelcomeCouponEnabled { get; set; }
    public int? AutoWelcomeCouponPercent { get; set; }
    public bool AutoReviewRewardEnabled { get; set; }
    public int? AutoReviewRewardPercent { get; set; }

    // Marketplace
    public bool MarketplaceEnabled { get; set; } // DEFAULT false — single-shop by default

    // Loyalty Program
    public bool LoyaltyProgramEnabled { get; set; }
    public int LoyaltyPointsPerLira { get; set; } = 1; // every 1 TL = 1 point
    public int LoyaltyPointsRedemptionRate { get; set; } = 100; // 100 points = 1 TL
    public int LoyaltyMinRedemption { get; set; } = 500; // min points to redeem
    public int LoyaltyWelcomeBonus { get; set; } = 50;
    public int LoyaltyReviewBonus { get; set; } = 50;
    public int LoyaltyReferralBonus { get; set; } = 200;
}
