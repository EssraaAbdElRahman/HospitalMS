using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace HospitalMS.PL.TagHelpers
{
    [HtmlTargetElement(Attributes = "asp-permission")]
    public class PermissionTagHelper : TagHelper
    {
        private readonly IAuthorizationService _authorizationService;

        public PermissionTagHelper(IAuthorizationService authorizationService)
        {
            _authorizationService = authorizationService;
        }

        [HtmlAttributeName("asp-permission")]
        public string Permission { get; set; } = string.Empty;

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; } = null!;

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var result = await _authorizationService.AuthorizeAsync(ViewContext.HttpContext.User, Permission);

            if (!result.Succeeded)
            {
                output.SuppressOutput();
            }
        }
    }
}
