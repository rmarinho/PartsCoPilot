using PartsCopilot.Views;

namespace PartsCopilot;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute("part-details", typeof(PartDetailsPage));
		Routing.RegisterRoute("manual-viewer", typeof(ManualViewerPage));
		Routing.RegisterRoute("compare-parts", typeof(ComparePartsPage));
	}
}
