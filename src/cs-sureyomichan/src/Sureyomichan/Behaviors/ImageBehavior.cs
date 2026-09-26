using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Haru.Kei.SureyomiChan.Behaviors;

class ImageBehavior : Behavior<Image> {
	public static readonly DependencyProperty SourceProperty =
		DependencyProperty.Register(
			nameof(Source),
			typeof(Models.Bindables.ImageObject),
			typeof(ImageBehavior),
			new PropertyMetadata(null));

	public Models.Bindables.ImageObject Source {
		get => (Models.Bindables.ImageObject)this.GetValue(SourceProperty);
		set {
			this.SetValue(SourceProperty, value);
		}
	}

	private readonly Storyboard storyboard = new();

	protected override void OnAttached() {
		base.OnAttached();
		this.AssociatedObject.Loaded += OnLoaded; ;
		this.AssociatedObject.Unloaded += OnUnloaded;
	}

	protected override void OnDetaching() {
		base.OnDetaching();
		this.AssociatedObject.Loaded -= OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e) {
		static Timeline nop(object source) {
			var animation = new ObjectAnimationUsingKeyFrames();
			animation.KeyFrames.Add(new DiscreteObjectKeyFrame() {
				KeyTime = TimeSpan.Zero,
				Value = source,
			});
			animation.Duration = new(TimeSpan.FromMicroseconds(1));

			var storyboard = new Storyboard();
			Storyboard.SetTargetProperty(
				animation,
				new PropertyPath(System.Windows.Controls.Image.SourceProperty));
			storyboard.Children.Add(animation);
			return storyboard;
		}

		void apply(Timeline tl) {
			Storyboard.SetTarget(this.AssociatedObject, tl);
			Storyboard.SetTargetProperty(tl, new PropertyPath(Image.SourceProperty));

			storyboard.Children.Add(tl);
			storyboard.Begin(this.AssociatedObject);
		}

		// 以前のものがある場合削除
		if(0 < this.storyboard.Children.Count) {
			this.storyboard.Stop();
			this.storyboard.Children.Clear();
		}

		if(this.Source == null) {
			return;
		}

		// アニメがない場合ダミーを入れる
		if(this.Source.AnimationSource is null){
			apply(nop(this.Source.ImageSource));
			return;
		}

		if(this.Source.AnimationSource is Timeline tl) {
			apply(tl);
			return;
		}
	}

	private void OnUnloaded(object sender, RoutedEventArgs e) {
		if(sender is Image img) {
			img.Unloaded -= OnUnloaded;
			this.storyboard.Stop();
			this.storyboard.Children.Clear();
		}
	}
}