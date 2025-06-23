namespace Volo.Abp.AspNetCore.Mvc.UI.Layout;

public interface IPageLayout
{
    ContentLayout Content { get; }

    /// <summary>
    /// If <keyword>false</keyword>, the menu, toolbar and footer will not be rendered.
    /// </summary>
    bool RenderLayout { get; set; }
}
