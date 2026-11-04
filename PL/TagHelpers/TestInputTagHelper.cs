using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;

namespace PL.TagHelpers
{
    [HtmlTargetElement("tw-input")]
    public class TestInputTagHelper : InputTagHelper
    {
        public TestInputTagHelper(IHtmlGenerator generator) : base(generator)
        {
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            base.Process(context, output);
            output.TagName = "input";

            var id = output.Attributes["id"]?.Value?.ToString();
            var labelText = For.Metadata.DisplayName ?? For.Metadata.PropertyName;

            // Generate Validation Span
            var validationTag = Generator.GenerateValidationMessage(
                ViewContext,
                For.ModelExplorer,
                For.Name,
                message: null,
                tag: "span",
                htmlAttributes: new { @class = "block text-xs text-red-400 -mt-1" }
            );

            // 2. Convert generated tag to HTML
            using var writer = new StringWriter();
            validationTag.WriteTo(writer, HtmlEncoder.Default);
            var validationHtml = writer.ToString();

            // 4. Construct HTML
            var preHtml = $@"
            <div class=""mb-2 text-sm font-semibold text-white/80"">
                <label for=""{id}"">{labelText}</label>
                {validationHtml}
            </div>";

            output.PreElement.SetHtmlContent(preHtml);

            // Apply the CSS (Tailwind)
            var tailwindClasses = "h-11 w-full rounded-lg border border-white/10 bg-white/[0.07] px-3.5 text-sm text-white placeholder:text-white/25 outline-none transition duration-200 focus:border-[#a875bc] focus:bg-white/[0.09] focus:ring-2 focus:ring-[#603273]/40";

            var existingClasses = output.Attributes["class"]?.Value?.ToString();
            var mergedClasses = string.IsNullOrEmpty(existingClasses)
                ? tailwindClasses
                : $"{tailwindClasses} {existingClasses}";

            output.Attributes.SetAttribute("class", mergedClasses);
        }
    }
}