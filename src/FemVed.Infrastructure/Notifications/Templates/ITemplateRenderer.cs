namespace FemVed.Infrastructure.Notifications.Templates;

/// <summary>
/// Renders an HTML email template by name with Handlebars-style variable substitution.
/// Templates ship as embedded resources under <c>Notifications/Templates/*.html</c>.
/// Each template file may declare its subject line via a metadata comment on the first line:
/// <code>&lt;!--subject: Welcome to FemVed, {{firstName}}--&gt;</code>
/// </summary>
public interface ITemplateRenderer
{
    /// <summary>
    /// Loads the template with the given key, renders it with the provided data, and returns
    /// the resolved subject + HTML body.
    /// </summary>
    /// <param name="templateKey">Template name (without extension), e.g. <c>"welcome"</c>.</param>
    /// <param name="data">Variable dictionary used by Handlebars to interpolate.</param>
    /// <returns>Rendered subject and HTML body.</returns>
    /// <exception cref="TemplateNotFoundException">Thrown when no template file matches the key.</exception>
    RenderedTemplate Render(string templateKey, IDictionary<string, object> data);

    /// <summary>Returns <c>true</c> if a template file exists for the given key.</summary>
    bool TemplateExists(string templateKey);
}

/// <summary>The rendered subject + HTML body of a template after variable interpolation.</summary>
public sealed record RenderedTemplate(string Subject, string Html);

/// <summary>Thrown when a template key has no matching embedded HTML resource.</summary>
public sealed class TemplateNotFoundException(string templateKey)
    : Exception($"No email template found for key '{templateKey}'. Add a file at Notifications/Templates/{templateKey}.html.");
