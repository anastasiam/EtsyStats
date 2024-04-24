namespace EtsyStats.Models;

public class ListingStats
{
    public string TitlePhotoUrl { get; set; }
    public string Link { get; set; }
    public decimal Visits { get; set; }
    public decimal TotalViews { get; set; }
    public decimal Orders { get; set; }
    public decimal Revenue { get; set; }
    public decimal ConversionRate => Visits == 0 ? 0 : Math.Round(Orders / Visits * 100, 2);
    public decimal ClickRate => TotalViews == 0 ? 0 : Math.Round(Visits / TotalViews * 100, 2);
    public string DirectAndOtherTraffic { get; set; }
    public string EtsyAppAndOtherEtsyPages { get; set; }
    public string EtsyAds { get; set; }
    public string EtsyMarketingAndSeo { get; set; }
    public string SocialMedia { get; set; }
    public string EtsySearch { get; set; }
    public string Title { get; set; }
    public string SearchTerms { get; set; }
    public string Tags { get; set; }
    public string WorkingTags { get; set; }
    public string NonWorkingTags { get; set; }
    public string WorkingTitleWords { get; set; }
    public string NonWorkingTitleWords { get; set; }
    public string TaggingIdeasTags { get; set; }
    public string TaggingIdeasTitle { get; set; }


    // My
    //	Direct & other traffic	Etsy app & other Etsy pages	Etsy Ads	Etsy marketing & SEO	Social media	Etsy search	Title	Search Terms	Tags	Working Tags	Non-Working Tags	Working Title Words	Non-Working Title Words	Tagging ideas (Tags)	Tagging ideas (Title)
}