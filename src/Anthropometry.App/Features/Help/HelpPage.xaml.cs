namespace Anthropometry.App.Features.Help;

public partial class HelpPage : ContentPage
{
    private const int LastPageIndex = 1;
    private int _pageIndex;

    public HelpPage()
    {
        InitializeComponent();
        ShowPage(0);
    }

    private void OnPreviousClicked(object? sender, EventArgs e)
    {
        ShowPage(_pageIndex - 1);
    }

    private void OnNextClicked(object? sender, EventArgs e)
    {
        ShowPage(_pageIndex + 1);
    }

    private void ShowPage(int pageIndex)
    {
        _pageIndex = Math.Clamp(pageIndex, 0, LastPageIndex);
        OverviewPage.IsVisible = _pageIndex == 0;
        TransferPage.IsVisible = _pageIndex == 1;
        PreviousButton.IsEnabled = _pageIndex > 0;
        NextButton.IsEnabled = _pageIndex < LastPageIndex;
        PageIndicator.Text = $"{_pageIndex + 1} / {LastPageIndex + 1}";
    }
}
