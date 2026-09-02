/*****************************************************************************
 * 
 * ReoGrid - .NET Spreadsheet Control
 * 
 * https://reogrid.net/
 *
 * THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
 * KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
 * PURPOSE.
 *
 * Author: Jingwood <jingwood at unvell.com>
 *
 * Copyright (c) 2012-2025 Jingwood <jingwood at unvell.com>
 * Copyright (c) 2012-2025 UNVELL Inc. All rights reserved.
 * 
 ****************************************************************************/

#if DRAWING

using System;

#if WINFORM || ANDROID
using RGFloat = System.Single;
#elif WPF
using RGFloat = System.Double;
#endif // WPF

using unvell.ReoGrid.Drawing;
using unvell.ReoGrid.Graphics;
using unvell.ReoGrid.Rendering;

namespace unvell.ReoGrid.Chart
{
	/// <summary>
	/// Represents chart legend view.
	/// </summary>
	public class ChartLegend : DrawingComponent
	{
		/// <summary>
		/// Get the instance of owner chart.
		/// </summary>
		public virtual IChart Chart { get; protected set; }

		/// <summary>
		/// Create chart legend view.
		/// </summary>
		/// <param name="chart">Instance of owner chart.</param>
		public ChartLegend(IChart chart)
		{
			this.Chart = chart;

			this.LineColor = SolidColor.Transparent;
			this.FillColor = SolidColor.Transparent;
			this.FontSize *= 0.8f;
			// 图例文字使用深色，避免透明/白字在白底上不可见
			this.ForeColor = SolidColor.Black;
			// 默认图例置于图表上方（饼图等在 CreateChartLegend 中另行覆盖）
			this.legendPosition = LegendPosition.Top;
		}

		/// <summary>
		/// Get or set type of legend.
		/// </summary>
		public LegendType LegendType { get; set; }

		private LegendPosition legendPosition;

		/// <summary>
		/// Get or set the display position of legend.
		/// </summary>
		public LegendPosition LegendPosition
		{
			get { return this.legendPosition; }
			set
			{
				if (this.legendPosition != value)
				{
					this.legendPosition = value;

					if (this.Chart is Chart)
					{
						var chart = (Chart)this.Chart;
						chart.DirtyLayout();
					}
				}
			}
		}

		/*
		/// <summary>
		/// Render chart legend view.
		/// </summary>
		/// <param name="dc">Platform no-associated drawing context instance.</param>
		protected override void OnPaint(DrawingContext dc)
		{
			base.OnPaint(dc);
		
			var g = dc.Graphics;
			//var ds = this.Chart.DataSource;
			var clientRect = this.ClientBounds;

			int dataCount = this.Chart.GetSerialCount();

			Rectangle itemRect = new Rectangle(0, 0, this.ItemSize.Width, ItemSize.Height);
			Size smybolSize = this.GetSymbolSize();

			for (int index = 0; index < dataCount; index++)
			{
				string itemTitle = this.Chart.GetSerialName(index);

				Rectangle symbolRect = new Rectangle(itemRect.Left + 3, itemRect.Top + (itemRect.Height - smybolSize.Height) / 2,
					smybolSize.Width, smybolSize.Height);

				this.DrawSymbol(dc, index, symbolRect);

				if (itemTitle != null)
				{
					Rectangle textRect = new Rectangle(symbolRect.Right + 3, itemRect.Top,
						itemRect.Width - symbolRect.Width - 3, itemRect.Height);

					g.DrawText(itemTitle, this.FontName, this.FontSize, this.ForeColor, textRect, ReoGridHorAlign.Left, ReoGridVerAlign.Middle);
				}

				itemRect.X += itemRect.Width;

				if (itemRect.X + itemRect.Width > clientRect.Right)
				{
					itemRect.X = 0;
					itemRect.Y += ItemSize.Height;
				}
			}
		}
		*/
		
		/// <summary>
		/// Get default symbol size of chart legend.
		/// </summary>
		/// <param name="index">Index of serial in data source.</param>
		/// <returns>Symbol size of chart legend.</returns>
		protected virtual Size GetSymbolSize(int index)
		{
			return new Size(14, 14);
		}

		/// <summary>
		/// Measure serial label size.
		/// </summary>
		/// <param name="index">Index of serial in data source.</param>
		/// <returns>Measured size for serial label.</returns>
		protected virtual Size GetLabelSize(int index)
		{
			var ds = this.Chart.DataSource;

			if (ds == null) return Size.Zero;

			string label = ds[index].Label;
			if (string.IsNullOrWhiteSpace(label))
			{
				label = $"系列 {index + 1}";
			}

			// 与坐标轴标签一致的字号换算，保证测量与 DrawText 绘制匹配
			RGFloat fontHeight = (RGFloat)(this.FontSize * PlatformUtility.GetDPI() / 72.0) + 4;
			var size = PlatformUtility.MeasureText(null, label, this.FontName, this.FontSize, this.FontStyles);

			if (size.Width <= 0)
			{
				size.Width = Math.Max(label.Length * this.FontSize * 0.55f, 24);
			}

			if (size.Height <= 0)
			{
				size.Height = fontHeight;
			}
			else
			{
				size.Height = Math.Max(size.Height, fontHeight);
			}

			return size;
		}

		private Size layoutedSize = Size.Zero;

		/// <summary>
		/// Get measured legend view size.
		/// </summary>
		/// <returns>Measured size of legend view.</returns>
		public override Size GetPreferredSize()
		{
			return this.layoutedSize;
		}

		/// <summary>
		/// Layout all legned items.
		/// </summary>
		public virtual void MeasureSize(Rectangle parentClientRect)
		{
			var ds = this.Chart.DataSource;
			if (ds == null) return;

			int dataCount = ds.SerialCount;

			this.Children.Clear();

			RGFloat maxSymbolWidth = 0, maxSymbolHeight = 0, maxLabelWidth = 0, maxLabelHeight = 0;

			#region Measure Sizes
			for (int index = 0; index < dataCount; index++)
			{
				var legendItem = new ChartLegendItem(this, index);

				var symbolSize = this.GetSymbolSize(index);

				if (maxSymbolWidth < symbolSize.Width) maxSymbolWidth = symbolSize.Width;
				if (maxSymbolHeight < symbolSize.Height) maxSymbolHeight = symbolSize.Height;

				legendItem.SymbolBounds = new Rectangle(new Point(0, 0), symbolSize);

				var labelSize = this.GetLabelSize(index);

				// should +6, don't know why
				labelSize.Width += 6;

				if (maxLabelWidth < labelSize.Width) maxLabelWidth = labelSize.Width;
				if (maxLabelHeight < labelSize.Height) maxLabelHeight = labelSize.Height;

				legendItem.LabelBounds = new Rectangle(new Point(0, 0), labelSize);

				this.Children.Add(legendItem);
			}
			#endregion // Measure Sizes

			#region Layout
			const RGFloat symbolLabelSpacing = 4;

			var itemWidth = maxSymbolWidth + symbolLabelSpacing + maxLabelWidth;
			var itemHeight = Math.Max(maxSymbolHeight, maxLabelHeight);

			var clientRect = parentClientRect;
			RGFloat x = 0, y = 0, right = 0, bottom = 0;
			RGFloat maxRight = Math.Max(clientRect.Width, itemWidth);
			const RGFloat itemSpacing = 10;

			for (int index = 0; index < dataCount; index++)
			{
				var legendItem = this.Children[index] as ChartLegendItem;

				if (legendItem != null)
				{
					// 当前行放不下则换行，避免图例横向超出图表
					if (x > 0 && x + itemWidth > clientRect.Width)
					{
						x = 0;
						y += itemHeight + itemSpacing;
					}

					legendItem.SetSymbolLocation(0, (itemHeight - legendItem.SymbolBounds.Height) / 2);
					legendItem.SetLabelLocation(maxSymbolWidth + symbolLabelSpacing, (itemHeight - legendItem.LabelBounds.Height) / 2);

					legendItem.Bounds = new Rectangle(x, y, itemWidth, itemHeight);

					if (right < legendItem.Right) right = legendItem.Right;
					if (bottom < legendItem.Bottom) bottom = legendItem.Bottom;
				}

				x += itemWidth;

				if (this.LegendPosition == LegendPosition.Left || this.LegendPosition == LegendPosition.Right)
				{
					x = 0;
					y += itemHeight + itemSpacing;
				}
				else
				{
					x += itemSpacing;

					if (x + itemWidth > clientRect.Width && index + 1 < dataCount)
					{
						x = 0;
						y += itemHeight + itemSpacing;
					}
				}
			}
			#endregion // Layout

			this.layoutedSize = new Size(Math.Min(right + 10, maxRight), bottom);

			// 裁剪子项，防止绘制超出图例区域
			this.ClipBounds = new Rectangle(0, 0, this.layoutedSize.Width, this.layoutedSize.Height);
		}

		/// <summary>
		/// 在图例本地坐标系绘制系列名称（与坐标轴同路径，避免子项嵌套导致文字不显示）。
		/// </summary>
		protected virtual void DrawLegendLabels(DrawingContext dc)
		{
			var ds = this.Chart?.DataSource;
			if (ds == null) return;

			var g = dc.Graphics;
			var textColor = this.ForeColor;
			if (textColor.A == 0 || textColor.Equals(SolidColor.Transparent) || textColor.Equals(SolidColor.White))
			{
				textColor = SolidColor.Black;
			}

			RGFloat fontHeight = (RGFloat)(this.FontSize * PlatformUtility.GetDPI() / 72.0) + 4;

			for (int i = 0; i < this.Children.Count; i++)
			{
				if (this.Children[i] is not ChartLegendItem item) continue;

				string title = ds[item.LegendIndex].Label;
				if (string.IsNullOrWhiteSpace(title))
				{
					title = $"系列 {item.LegendIndex + 1}";
				}

				var lb = item.LabelBounds;
				// 文字绘制区域：色块右侧至图例项右缘，高度取图例项全高
				var textRect = new Rectangle(
					item.X + lb.X,
					item.Y,
					Math.Max(lb.Width, item.Width - lb.X),
					Math.Max(item.Height, fontHeight));

				g.DrawText(title, this.FontName, this.FontSize, textColor, textRect,
					ReoGridHorAlign.Left, ReoGridVerAlign.Middle);
			}
		}

		/// <summary>
		/// 先绘制子项色块，再在图例层统一绘制文字。
		/// </summary>
		protected override void OnPaint(DrawingContext dc)
		{
			base.OnPaint(dc);
			this.DrawLegendLabels(dc);
		}

	}

	/// <summary>
	/// Represents chart legend item.
	/// </summary>
	public class ChartLegendItem : DrawingObject
	{
		private Rectangle symbolBounds;
		public virtual Rectangle SymbolBounds { get { return this.symbolBounds; } set { this.symbolBounds = value; } }

		private Rectangle labelBounds;
		public virtual Rectangle LabelBounds { get { return this.labelBounds; } set { this.labelBounds = value; } }

		public virtual void SetSymbolLocation(RGFloat x, RGFloat y)
		{
			this.symbolBounds.X = x;
			this.symbolBounds.Y = y;
		}

		public virtual void SetLabelLocation(RGFloat x, RGFloat y)
		{
			this.labelBounds.X = x;
			this.labelBounds.Y = y;
		}

		public virtual ChartLegend ChartLegend { get; protected set; }

		public ChartLegendItem(ChartLegend chartLegend, int legendIndex)
		{
			this.ChartLegend = chartLegend;
			this.LegendIndex = legendIndex;

			// 与父图例保持同一字体/颜色，避免测量与绘制字号不一致导致文字被裁切
			if (chartLegend != null)
			{
				this.FontName = chartLegend.FontName;
				this.FontSize = chartLegend.FontSize;
				this.ForeColor = chartLegend.ForeColor;
			}
		}

		/// <summary>
		/// 解析图例文字颜色：透明或与白底相同时回退黑色。
		/// </summary>
		private SolidColor GetLegendTextColor()
		{
			var color = this.ChartLegend?.ForeColor ?? this.ForeColor;

			if (color.A == 0
				|| color.Equals(SolidColor.Transparent)
				|| color.Equals(SolidColor.White))
			{
				return SolidColor.Black;
			}

			return color;
		}

		public virtual int LegendIndex { get; set; }

		protected override void OnPaint(DrawingContext dc)
		{
#if DEBUG
			//dc.Graphics.FillRectangle(this.ClientBounds, SolidColor.LightSteelBlue);
#endif // DEBUG

			if (this.symbolBounds.Width > 0 && this.symbolBounds.Height > 0)
			{
				this.OnPaintSymbol(dc);
			}

			// 文字改由 ChartLegend.DrawLegendLabels 统一绘制
		}

		/// <summary>
		/// Draw chart legend symbol.
		/// </summary>
		/// <param name="dc">Platform no-associated drawing context instance.</param>
		public virtual void OnPaintSymbol(DrawingContext dc)
		{
			var g = dc.Graphics;

			if (this.ChartLegend != null)
			{
				var legend = this.ChartLegend;

				if (legend.Chart != null)
				{
					var dss = legend.Chart.DataSerialStyles;

					if (dss != null)
					{
						var dsStyle = dss[LegendIndex];

						g.DrawAndFillRectangle(this.symbolBounds, dsStyle.LineColor, dsStyle.FillColor);
					}
				}
			}
		}

		/// <summary>
		/// Draw chart legend label.
		/// </summary>
		/// <param name="dc">Platform no-associated drawing context instance.</param>
		public virtual void OnPaintLabel(DrawingContext dc)
		{
			if (this.ChartLegend != null)
			{
				var legend = this.ChartLegend;

				if (legend.Chart != null && legend.Chart.DataSource != null)
				{
					var ds = legend.Chart.DataSource;

					string itemTitle = ds[LegendIndex].Label;
					// 无单元格/缓存标签时使用默认系列名，保证图例始终显示名称
					if (string.IsNullOrEmpty(itemTitle))
					{
						itemTitle = $"系列 {LegendIndex + 1}";
					}

#if DEBUG
					//dc.Graphics.FillRectangle(this.labelBounds, SolidColor.LightCoral);
#endif // DEBUG

					dc.Graphics.DrawText(itemTitle,
						this.ChartLegend?.FontName ?? this.FontName,
						this.ChartLegend?.FontSize ?? this.FontSize,
						this.GetLegendTextColor(), this.labelBounds,
						ReoGridHorAlign.Left, ReoGridVerAlign.Middle);
				}
			}
		}
	}

	/// <summary>
	/// Legend type.
	/// </summary>
	public enum LegendType
	{
		/// <summary>
		/// Primary legend.
		/// </summary>
		PrimaryLegend,

		/// <summary>
		/// Secondary legend.
		/// </summary>
		SecondaryLegend,
	}

	/// <summary>
	/// Legend position.
	/// </summary>
	public enum LegendPosition
	{
		/// <summary>
		/// Right
		/// </summary>
		Right,

		/// <summary>
		/// Bottom
		/// </summary>
		Bottom,

		/// <summary>
		/// Left
		/// </summary>
		Left,

		/// <summary>
		/// Top
		/// </summary>
		Top,
	}
}

#endif // DRAWING