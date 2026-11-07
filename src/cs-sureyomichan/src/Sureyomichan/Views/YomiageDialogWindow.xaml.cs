using Prism.Dialogs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Haru.Kei.SureyomiChan.Views {
	/// <summary>
	/// DialogWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class YomiageDialogWindow : MahApps.Metro.Controls.MetroWindow, IDialogWindow {
		public YomiageDialogWindow() {
			InitializeComponent();
			this.Loaded += (_, _) => {
				this.Owner = null;
			};
		}

		public IDialogResult Result { get; set; } = new Prism.Dialogs.DialogResult();
	}
}
