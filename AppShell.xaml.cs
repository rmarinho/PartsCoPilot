using PartsCopilot.Views;

namespace PartsCopilot;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute("search", typeof(SearchPage));
	}
}
