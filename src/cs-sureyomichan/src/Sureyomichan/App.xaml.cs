using Prism.Ioc;
using Prism.Mvvm;
using Prism.Unity;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows;

namespace Haru.Kei.SureyomiChan;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : PrismApplication {

	protected override void OnStartup(StartupEventArgs e) {
		AppDomain.CurrentDomain.UnhandledException += (_, e) => {
			Utils.Logger.Instance.WriteFromUnhandledException(
				"！！！予期せぬエラーが発生しました！！！",
				e.ExceptionObject as Exception);
		};

		System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
		Reactive.Bindings.UIDispatcherScheduler.Initialize();

		Utils.Logger.Instance.Info($"スレ詠みちゃんが起動しました => build={SureyomiChanEnviroment.Build}");
		var r = Utils.Singleton.Instance.StartupSequence.Begin();
		if(!r.IsFirstStart) {
			Utils.Singleton.Instance.StartupSequence.End(0);

			if(Utils.Util.ParseCommandLine(Environment.CommandLine) is { }) {
				Interop.SendMessage(r.Window, Interop.WM_COPYDATA, 0, new Interop.COPYDATASTRUCT() {
					dwData = SureyomiChanEnviroment.CopyDataTypeCommandArgs,
					cbData = Environment.CommandLine.Length * 2 + 1,
					lpData = Environment.CommandLine,
				});
			}

			this.Shutdown();
		}
		base.OnStartup(e);
	}

	protected override Window CreateShell() {
		return Container.Resolve<Views.MainWindow>();
	}

	protected override void RegisterTypes(IContainerRegistry containerRegistry) {
		base.ConfigureViewModelLocator();

		ViewModelLocationProvider.Register<Views.MainWindow, ViewModels.MainWindowViewModel>();
		ViewModelLocationProvider.Register<Views.YomiageDialog, ViewModels.YomiageDialogViewModel>();
		containerRegistry.RegisterDialogWindow<Views.YomiageDialogWindow>(nameof(Views.YomiageDialogWindow));
		containerRegistry.RegisterDialog<Views.YomiageDialog>(typeof(Views.YomiageDialog).FullName);
		containerRegistry.RegisterInstance(this.Container);
	}
}
