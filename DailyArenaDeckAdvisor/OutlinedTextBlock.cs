using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;

namespace DailyArena.DeckAdvisor
{
	/// <summary>
	/// Class for an Outlined Text Block item in the WPF presentation.
	/// </summary>
	[ContentProperty("Text")]
	public class OutlinedTextBlock : FrameworkElement
	{
		/// <summary>
		/// Update this Text Block's Pen.
		/// </summary>
		private void UpdatePen()
		{
			_Pen = new Pen(Stroke, StrokeThickness)
			{
				DashCap = PenLineCap.Round,
				EndLineCap = PenLineCap.Round,
				LineJoin = PenLineJoin.Round,
				StartLineCap = PenLineCap.Round
			};

			InvalidateVisual();
		}

		/// <summary>
		/// The Text Block's Fill property.
		/// </summary>
		public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
		  "Fill",
		  typeof(Brush),
		  typeof(OutlinedTextBlock),
		  new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

		/// <summary>
		/// The Text Block's Stroke property.
		/// </summary>
		public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
		  "Stroke",
		  typeof(Brush),
		  typeof(OutlinedTextBlock),
		  new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender, StrokePropertyChangedCallback));

		/// <summary>
		/// Callback that is called when a Text Block's Stroke property changes.
		/// </summary>
		/// <param name="dependencyObject">The Text Block whose Stroke property changed.</param>
		/// <param name="dependencyPropertyChangedEventArgs">Arguments regarding the Stroke property change event.</param>
		private static void StrokePropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
		{
			(dependencyObject as OutlinedTextBlock)?.UpdatePen();
		}

		/// <summary>
		/// The Text Block's StrokeThickness property.
		/// </summary>
		public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
		  "StrokeThickness",
		  typeof(double),
		  typeof(OutlinedTextBlock),
		  new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsRender, StrokePropertyChangedCallback));

		/// <summary>
		/// The Text Block's FontFamily property.
		/// </summary>
		public static readonly DependencyProperty FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner(
		  typeof(OutlinedTextBlock),
		  new FrameworkPropertyMetadata(OnFormattedTextUpdated));

		/// <summary>
		/// The Text Block's FontSize property.
		/// </summary>
		public static readonly DependencyProperty FontSizeProperty = TextElement.FontSizeProperty.AddOwner(
		  typeof(OutlinedTextBlock),
		  new FrameworkPropertyMetadata(OnFormattedTextUpdated));

		/// <summary>
		/// The Text Block's FontStretch property.
		/// </summary>
		public static readonly DependencyProperty FontStretchProperty = TextElement.FontStretchProperty.AddOwner(
		  typeof(OutlinedTextBlock),
		  new FrameworkPropertyMetadata(OnFormattedTextUpdated));

		/// <summary>
		/// The Text Block's FontStyle property.
		/// </summary>
		public static readonly DependencyProperty FontStyleProperty = TextElement.FontStyleProperty.AddOwner(
		  typeof(OutlinedTextBlock),
		  new FrameworkPropertyMetadata(OnFormattedTextUpdated));

		/// <summary>
		/// The Text Block's FontWeight property.
		/// </summary>
		public static readonly DependencyProperty FontWeightProperty = TextElement.FontWeightProperty.AddOwner(
		  typeof(OutlinedTextBlock),
		  new FrameworkPropertyMetadata(OnFormattedTextUpdated));

		/// <summary>
		/// The Text Block's Text property.
		/// </summary>
		public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
		  "Text",
		  typeof(string),
		  typeof(OutlinedTextBlock),
		  new FrameworkPropertyMetadata(OnFormattedTextInvalidated));

		/// <summary>
		/// The Text Block's TextAlignment property.
		/// </summary>
		public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register(
		  "TextAlignment",
		  typeof(TextAlignment),
		  typeof(OutlinedTextBlock),
		  new FrameworkPropertyMetadata(OnFormattedTextUpdated));

		/// <summary>
		/// The Text Block's TextDecoration property.
		/// </summary>
		public static readonly DependencyProperty TextDecorationsProperty = DependencyProperty.Register(
		  "TextDecorations",
		  typeof(TextDecorationCollection),
		  typeof(OutlinedTextBlock),
		  new FrameworkPropertyMetadata(OnFormattedTextUpdated));

		/// <summary>
		/// The Text Block's TextTrimming property.
		/// </summary>
		public static readonly DependencyProperty TextTrimmingProperty = DependencyProperty.Register(
		  "TextTrimming",
		  typeof(TextTrimming),
		  typeof(OutlinedTextBlock),
		  new FrameworkPropertyMetadata(OnFormattedTextUpdated));

		/// <summary>
		/// The Text Block's TextWrapping property.
		/// </summary>
		public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(
		  "TextWrapping",
		  typeof(TextWrapping),
		  typeof(OutlinedTextBlock),
		  new FrameworkPropertyMetadata(TextWrapping.NoWrap, OnFormattedTextUpdated));

		/// <summary>
		/// The Text Block's formatted text.
		/// </summary>
		private FormattedText _FormattedText;

		/// <summary>
		/// The geometry of the Text Block's formatted text.
		/// </summary>
		private Geometry _TextGeometry;

		/// <summary>
		/// The Pen used to render the Text Block's outline.
		/// </summary>
		private Pen _Pen;

		/// <summary>
		/// Gets or sets the Text Block's Fill Property.
		/// </summary>
		public Brush Fill
		{
			get { return (Brush)GetValue(FillProperty); }
			set { SetValue(FillProperty, value); }
		}

		/// <summary>
		/// Gets or sets the Text Block's FontFamily Property.
		/// </summary>
		public FontFamily FontFamily
		{
			get { return (FontFamily)GetValue(FontFamilyProperty); }
			set { SetValue(FontFamilyProperty, value); }
		}

		/// <summary>
		/// Gets or sets the Text Block's FontSize Property.
		/// </summary>
		[TypeConverter(typeof(FontSizeConverter))]
		public double FontSize
		{
			get { return (double)GetValue(FontSizeProperty); }
			set { SetValue(FontSizeProperty, value); }
		}

		/// <summary>
		/// Gets or sets the Text Block's FontStretch Property.
		/// </summary>
		public FontStretch FontStretch
		{
			get { return (FontStretch)GetValue(FontStretchProperty); }
			set { SetValue(FontStretchProperty, value); }
		}

		/// <summary>
		/// Gets or sets the Text Block's FontStyle Property.
		/// </summary>
		public FontStyle FontStyle
		{
			get { return (FontStyle)GetValue(FontStyleProperty); }
			set { SetValue(FontStyleProperty, value); }
		}

		/// <summary>
		/// Gets or sets the Text Block's FontWeight Property.
		/// </summary>
		public FontWeight FontWeight
		{
			get { return (FontWeight)GetValue(FontWeightProperty); }
			set { SetValue(FontWeightProperty, value); }
		}

		/// <summary>
		/// Gets or sets the Text Block's Stroke Property.
		/// </summary>
		public Brush Stroke
		{
			get { return (Brush)GetValue(StrokeProperty); }
			set { SetValue(StrokeProperty, value); }
		}

		/// <summary>
		/// Gets or sets the Text Block's StrokeThickness Property.
		/// </summary>
		public double StrokeThickness
		{
			get { return (double)GetValue(StrokeThicknessProperty); }
			set { SetValue(StrokeThicknessProperty, value); }
		}

		/// <summary>
		/// Gets or sets the Text Block's Text Property.
		/// </summary>
		public string Text
		{
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

		/// <summary>
		/// Gets or sets the Text Block's TextAlignment Property.
		/// </summary>
		public TextAlignment TextAlignment
		{
			get { return (TextAlignment)GetValue(TextAlignmentProperty); }
			set { SetValue(TextAlignmentProperty, value); }
		}

		/// <summary>
		/// Gets or sets the Text Block's TextDecorations Property.
		/// </summary>
		public TextDecorationCollection TextDecorations
		{
			get { return (TextDecorationCollection)GetValue(TextDecorationsProperty); }
			set { SetValue(TextDecorationsProperty, value); }
		}

		/// <summary>
		/// Gets or sets the Text Block's TextTrimming Property.
		/// </summary>
		public TextTrimming TextTrimming
		{
			get { return (TextTrimming)GetValue(TextTrimmingProperty); }
			set { SetValue(TextTrimmingProperty, value); }
		}

		/// <summary>
		/// Gets or sets the Text Block's TextWrapping Property.
		/// </summary>
		public TextWrapping TextWrapping
		{
			get { return (TextWrapping)GetValue(TextWrappingProperty); }
			set { SetValue(TextWrappingProperty, value); }
		}

		/// <summary>
		/// Default constructor.
		/// </summary>
		public OutlinedTextBlock()
		{
			UpdatePen();
			TextDecorations = new TextDecorationCollection();
		}

		/// <summary>
		/// The callback that is called when the Text Block is rendered.
		/// </summary>
		/// <param name="drawingContext">The context on which the Text Block is to be drawn.</param>
		protected override void OnRender(DrawingContext drawingContext)
		{
			EnsureGeometry();

			drawingContext.DrawGeometry(null, _Pen, _TextGeometry);
			drawingContext.DrawGeometry(Fill, null, _TextGeometry);
		}

		/// <summary>
		/// Override the size of the Text Block.
		/// </summary>
		/// <param name="availableSize">The size available to render the Text Block.</param>
		/// <returns>The Size required to render the Text Block.</returns>
		protected override Size MeasureOverride(Size availableSize)
		{
			EnsureFormattedText();

			// constrain the formatted text according to the available size

			double w = availableSize.Width;
			double h = availableSize.Height;

			// the Math.Min call is important - without this constraint (which seems arbitrary, but is the maximum allowable text width), things blow up when availableSize is infinite in both directions
			// the Math.Max call is to ensure we don't hit zero, which will cause MaxTextHeight to throw
			_FormattedText.MaxTextWidth = Math.Min(3579139, w);
			_FormattedText.MaxTextHeight = Math.Max(0.0001d, h);

			// return the desired size
			return new Size(Math.Ceiling(_FormattedText.Width), Math.Ceiling(_FormattedText.Height));
		}

		/// <summary>
		/// Override the arranged size of the Text Block.
		/// </summary>
		/// <param name="finalSize">The final size available to arrange the Text Block.</param>
		/// <returns>The size required to arrange the Text Block.</returns>
		protected override Size ArrangeOverride(Size finalSize)
		{
			EnsureFormattedText();

			// update the formatted text with the final size
			_FormattedText.MaxTextWidth = finalSize.Width;
			_FormattedText.MaxTextHeight = Math.Max(0.0001d, finalSize.Height);

			// need to re-generate the geometry now that the dimensions have changed
			_TextGeometry = null;

			return finalSize;
		}

		/// <summary>
		/// Callback called when the formatted text is invalidated.
		/// </summary>
		/// <param name="dependencyObject">The Text Block whose formatted text was invalidated.</param>
		/// <param name="e">Arguments pertaining to the formatted text property invalidation event.</param>
		private static void OnFormattedTextInvalidated(DependencyObject dependencyObject,
		  DependencyPropertyChangedEventArgs e)
		{
			var outlinedTextBlock = (OutlinedTextBlock)dependencyObject;
			outlinedTextBlock._FormattedText = null;
			outlinedTextBlock._TextGeometry = null;

			outlinedTextBlock.InvalidateMeasure();
			outlinedTextBlock.InvalidateVisual();
		}

		/// <summary>
		/// Callback called when the formatted text is updated.
		/// </summary>
		/// <param name="dependencyObject">The Text Block whose formatted text was updated.</param>
		/// <param name="e">Arguments pertaining to the formatted text property update event.</param>
		private static void OnFormattedTextUpdated(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
		{
			var outlinedTextBlock = (OutlinedTextBlock)dependencyObject;
			outlinedTextBlock.UpdateFormattedText();
			outlinedTextBlock._TextGeometry = null;

			outlinedTextBlock.InvalidateMeasure();
			outlinedTextBlock.InvalidateVisual();
		}

		/// <summary>
		/// Ensure that the formatted text object is created.
		/// </summary>
		private void EnsureFormattedText()
		{
			if (_FormattedText != null)
			{
				return;
			}

			_FormattedText = new FormattedText(
			  Text ?? "",
			  CultureInfo.CurrentUICulture,
			  FlowDirection,
			  new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
			  FontSize,
			  Brushes.Black,
			  VisualTreeHelper.GetDpi(this).PixelsPerDip);

			UpdateFormattedText();
		}

		/// <summary>
		/// Update the formatted text object.
		/// </summary>
		private void UpdateFormattedText()
		{
			if (_FormattedText == null)
			{
				return;
			}

			_FormattedText.MaxLineCount = TextWrapping == TextWrapping.NoWrap ? 1 : int.MaxValue;
			_FormattedText.TextAlignment = TextAlignment;
			_FormattedText.Trimming = TextTrimming;

			_FormattedText.SetFontSize(FontSize);
			_FormattedText.SetFontStyle(FontStyle);
			_FormattedText.SetFontWeight(FontWeight);
			_FormattedText.SetFontFamily(FontFamily);
			_FormattedText.SetFontStretch(FontStretch);
			_FormattedText.SetTextDecorations(TextDecorations);
		}

		/// <summary>
		/// Ensure that the text geometry is created.
		/// </summary>
		private void EnsureGeometry()
		{
			if (_TextGeometry != null)
			{
				return;
			}

			EnsureFormattedText();
			_TextGeometry = _FormattedText.BuildGeometry(new Point(0, 0));
		}
	}
}
