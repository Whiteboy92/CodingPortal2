using Microsoft.AspNetCore.Mvc.RazorPages;
namespace CodingPortal2.Pages;

public class PrivacyModel : PageModel
{
    private readonly ILogger<PrivacyModel> logger;

    public PrivacyModel(ILogger<PrivacyModel> logger)
    {
        this.logger = logger;
    }

    public void OnGet()
    {
    }
}