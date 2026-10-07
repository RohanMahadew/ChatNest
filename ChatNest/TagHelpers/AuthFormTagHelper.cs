using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace ChatNest.TagHelpers;

[HtmlTargetElement("register-form")]
public sealed class RegisterFormTagHelper : TagHelper
{
    private readonly IUrlHelperFactory _urlHelperFactory;
    private readonly IHtmlGenerator _htmlGenerator;
    public RegisterFormTagHelper(
        IUrlHelperFactory urlHelperFactory,
        IHtmlGenerator htmlGenerator)
    {
        _urlHelperFactory = urlHelperFactory;
        _htmlGenerator = htmlGenerator;
    }

    [HtmlAttributeName("method")]
    public string Method { get; set; } = "post";

    [HtmlAttributeName("action")]
    public string? Action { get; set; }

    [HtmlAttributeName("class")]
    public string? CssClass { get; set; }

    [HtmlAttributeName("heading")]
    public string Heading { get; set; } = "Get started";

    [HtmlAttributeName("title")]
    public string Title { get; set; } = "Create your account";

    [HtmlAttributeName("description")]
    public string Description { get; set; } =
        "It only takes a minute. Choose your details below.";

    [HtmlAttributeName("submit-text")]
    public string SubmitText { get; set; } = "Create account";

    [HtmlAttributeName("has-account")]
    public bool HasAccount { get; set; } = true;

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    public override async Task ProcessAsync(
        TagHelperContext context,
        TagHelperOutput output)
    {
        var content = await output.GetChildContentAsync();
        var card = Element(
            "div",
            "w-full max-w-md rounded-2xl border border-white/10 bg-[#091a50]/85 p-6 shadow-2xl shadow-black/30 backdrop-blur-xl sm:p-8");

        var heading = Element("div", "mb-7");
        heading.InnerHtml.AppendHtml(
            Element(
                "p",
                "text-xs font-bold uppercase tracking-[0.2em] text-[#b98acb]",
                Heading));
        heading.InnerHtml.AppendHtml(
            Element(
                "h2",
                "mt-2 text-2xl font-bold tracking-tight",
                Title));
        heading.InnerHtml.AppendHtml(
            Element(
                "p",
                "mt-2 text-sm leading-6 text-white/50",
                Description));
        card.InnerHtml.AppendHtml(heading);

        var form = new TagBuilder("form");
        form.Attributes["method"] = Method;
        if (Action is not null)
        {
            form.Attributes["action"] = Action;
        }

        if (CssClass is not null)
        {
            form.AddCssClass(CssClass);
        }
        else
        {
            form.AddCssClass("space-y-4");
        }

        form.InnerHtml.AppendHtml(
        _htmlGenerator.GenerateAntiforgery(ViewContext));

        form.InnerHtml.AppendHtml(content);

        var button = Element(
            "button",
            "group relative mt-2 flex h-11 w-full items-center justify-center overflow-hidden rounded-lg bg-[#603273] text-sm font-bold text-white shadow-lg shadow-[#603273]/20 transition duration-300 hover:bg-[#75418b] hover:shadow-[#603273]/30 focus:outline-none focus:ring-2 focus:ring-white/50 focus:ring-offset-2 focus:ring-offset-[#091a50]");
        button.Attributes["type"] = "submit";
        button.InnerHtml.AppendHtml(
            Element("span", "relative z-10", SubmitText));
        button.InnerHtml.AppendHtml(
            Element(
                "span",
                "absolute inset-0 -translate-x-full bg-gradient-to-r from-transparent via-white/10 to-transparent transition duration-700 group-hover:translate-x-full"));
        form.InnerHtml.AppendHtml(button);
        card.InnerHtml.AppendHtml(form);

        var login = Element("p", "mt-6 text-center text-xs text-white/40");
        login.InnerHtml.Append(
            HasAccount ? "Already registered? " : "Don't have an account yet? ");
        var loginLink = Element(
            "a",
            "font-semibold text-[#c79bd5] transition hover:text-white",
            HasAccount ? "Sign in" : "Sign up");
        var urlHelper = _urlHelperFactory.GetUrlHelper(ViewContext);
        loginLink.Attributes["href"] = urlHelper.Page(
            HasAccount ? "/Login" : "/Register")!;
        login.InnerHtml.AppendHtml(loginLink);
        card.InnerHtml.AppendHtml(login);

        output.TagName = "section";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.Clear();
        output.Attributes.SetAttribute(
            "class",
            "flex items-center justify-center px-5 pb-12 sm:px-8 lg:px-12 lg:py-16");
        output.Content.SetHtmlContent(card);
    }

    private static TagBuilder Element(
        string tagName,
        string cssClass,
        string? text = null)
    {
        var element = new TagBuilder(tagName);
        element.AddCssClass(cssClass);

        if (text is not null)
        {
            element.InnerHtml.Append(text);
        }

        return element;
    }
}