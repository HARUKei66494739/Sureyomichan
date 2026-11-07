using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading.Tasks;

using Prism.Events;
using ViewModel = Haru.Kei.SureyomiChan.ViewModels.MainWindowViewModel;

namespace Haru.Kei.SureyomiChan.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : MahApps.Metro.Controls.MetroWindow {
	public MainWindow() {
		InitializeComponent();

		Utils.Singleton.Instance.PrismMessenger.GetEvent<PubSubEvent<Models.WindowShowMessage>>()
			.Subscribe(x => {
				if(object.ReferenceEquals(this, x.Token)) {
					// 最大化は考慮していない
					this.WindowState = WindowState.Normal;
				}
			});
		Utils.Singleton.Instance.PrismMessenger.GetEvent<PubSubEvent<Models.WindowMinimizeMessage>>()
			.Subscribe(x => {
				if(object.ReferenceEquals(this, x.Token)) {
					this.WindowState = WindowState.Minimized;
				}
			});
	}
}