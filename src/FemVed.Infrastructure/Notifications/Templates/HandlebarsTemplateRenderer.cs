using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;
using HandlebarsDotNet;

namespace FemVed.Infrastructure.Notifications.Templates;

/// <summary>
/// Loads email templates from embedded HTML resources and renders them with Handlebars.Net.
///
/// File format: each <c>Notifications/Templates/&lt;key&gt;.html</c> may begin with a metadata
/// comment to define the subject line, then the HTML body:
/// <code>
/// &lt;!--subject: Welcome to FemVed, {{firstName}}--&gt;
/// &lt;!DOCTYPE html&gt;
/// ...
/// </code>
///
/// Compiled Handlebars templates are cached per key for the lifetime of the process.
/// </summary>
public sealed class HandlebarsTemplateRenderer : ITemplateRenderer
{
    private static readonly Regex SubjectRegex = new(
        @"^\s*<!--\s*subject:\s*(?<subject>.*?)\s*-->\s*(?<body>[\s\S]*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly ConcurrentDictionary<string, CompiledTemplate> _cache = new();
    private readonly Assembly _assembly = typeof(HandlebarsTemplateRenderer).Assembly;
    private readonly string _resourcePrefix = $"{typeof(HandlebarsTemplateRenderer).Namespace}.";

    /// <inheritdoc/>
    public RenderedTemplate Render(string templateKey, IDictionary<string, object> data)
    {
        var compiled = _cache.GetOrAdd(templateKey, Load);
        var subject = compiled.SubjectTemplate(data);
        var html    = compiled.BodyTemplate(data);
        return new RenderedTemplate(subject, html);
    }

    /// <inheritdoc/>
    public bool TemplateExists(string templateKey)
    {
        return _assembly.GetManifestResourceStream(ResourceName(templateKey)) is not null;
    }

    private CompiledTemplate Load(string templateKey)
    {
        var resourceName = ResourceName(templateKey);
        using var stream = _assembly.GetManifestResourceStream(resourceName)
            ?? throw new TemplateNotFoundException(templateKey);

        using var reader = new StreamReader(stream);
        var raw = reader.ReadToEnd();

        var match = SubjectRegex.Match(raw);
        var subjectSource = match.Success ? match.Groups["subject"].Value : string.Empty;
        var bodySource    = match.Success ? match.Groups["body"].Value    : raw;

        return new CompiledTemplate(
            SubjectTemplate: Handlebars.Compile(subjectSource),
            BodyTemplate:    Handlebars.Compile(bodySource));
    }

    private string ResourceName(string templateKey) => $"{_resourcePrefix}{templateKey}.html";

    private sealed record CompiledTemplate(
        HandlebarsTemplate<object, object> SubjectTemplate,
        HandlebarsTemplate<object, object> BodyTemplate);
}
