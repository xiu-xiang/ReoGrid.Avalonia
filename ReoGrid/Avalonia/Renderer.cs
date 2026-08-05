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
 * Copyright (c) 2012-2023 Jingwood <jingwood at unvell.com>
 * Copyright (c) 2012-2023 unvell inc. All rights reserved.
 * 
 ****************************************************************************/

#if AVALONIA
#define GRID_GUIDELINE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using unvell.Common;
using unvell.ReoGrid.Graphics;

using Point = unvell.ReoGrid.Graphics.Point;

using unvell.ReoGrid.Drawing.Text;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using System.Net;
using unvell.ReoGrid.AvaloniaPlatform;

namespace unvell.ReoGrid.Rendering
{
    #region Graphics
    internal class AvaloniaGraphics : IGraphics
    {
        protected ResourcePoolManager resourceManager = new ResourcePoolManager();

        public ResourcePoolManager ResourcePoolManager
        {
            get { return this.resourceManager; }
        }

        private PlatformGraphics g = null;

        public AvaloniaGraphics()
        {

        }

        public PlatformGraphics PlatformGraphics { get { return g; } set { this.g = value; } }

        #region Line
        public void DrawLine(Pen p, double x1, double y1, double x2, double y2)
        {
            DrawLine(p, new Point(x1, y1), new Point(x2, y2));
        }

        public void DrawLine(Pen p, Point startPoint, Point endPoint)
        {
            // Linux/Skia：空 Pen 或非法坐标会导致原生段错误
            if (g == null || p == null)
                return;
            if (double.IsNaN(startPoint.X) || double.IsNaN(startPoint.Y)
                || double.IsNaN(endPoint.X) || double.IsNaN(endPoint.Y)
                || double.IsInfinity(startPoint.X) || double.IsInfinity(startPoint.Y)
                || double.IsInfinity(endPoint.X) || double.IsInfinity(endPoint.Y))
                return;

#if !GRID_GUIDELINE
            double halfPenWidth = p.Thickness / 2;

            // Create a guidelines set
            Avalonia.Media.GuidelineSet guidelines = new Avalonia.Media.GuidelineSet();

            guidelines.GuidelinesX.Add(startPoint.X + halfPenWidth);
            guidelines.GuidelinesY.Add(startPoint.Y + halfPenWidth);

            g.PushGuidelineSet(guidelines);
#endif // GRID_GUIDELINE

            g.DrawLine(p, startPoint, endPoint);

#if !GRID_GUIDELINE
            g.Pop();
#endif // GRID_GUIDELINE
        }

        public void DrawLine(Point startPoint, Point endPoint, SolidColor color)
        {
            var pen = this.resourceManager.GetPen(color);
            if (pen == null) return;
            this.g.DrawLine(pen, (Point)startPoint, (Point)endPoint);
        }

        public void DrawLine(double x1, double y1, double x2, double y2, SolidColor color)
        {
            var pen = this.resourceManager.GetPen(color);
            if (pen == null) return;
            this.DrawLine(pen, x1, y1, x2, y2);
        }

        public void DrawLine(double x1, double y1, double x2, double y2, SolidColor color, double width, LineStyles style)
        {
            var p = this.resourceManager.GetPen(color, width, GetDashStyle(style));

            if (p == null)
                return;

            {
                g.DrawLine(p, new Avalonia.Point(x1, y1), new Avalonia.Point(x2, y2));
            }
        }

        public void DrawLine(Point startPoint, Point endPoint, SolidColor color, double width, LineStyles style)
        {
            var p = this.resourceManager.GetPen(color, width, GetDashStyle(style));

            if (p != null)
            {
                g.DrawLine(p, startPoint, endPoint);
            }
        }

        public void DrawLines(Point[] points, int start, int length, SolidColor color, double width, LineStyles style)
        {
            if (!color.IsTransparent && length > 1)
            {
                var p = this.resourceManager.GetPen(color, width, GetDashStyle(style));

                if (p != null)
                {
                    var geo = new PathGeometry();
                    geo.Figures.Add(new PathFigure()
                    {
                        StartPoint = points[start],
                        IsClosed = false,
                        Segments = [..points.Skip(start).Take(length).Select(p=>new LineSegment() { Point = p})]
                    });
                    //for (int i = 1, k = start + 1; i < length; i++, k++)
                    //{
                    //    geo.Figures.Add(new PathFigure() { Segments = { new LineSegment() { Point = points[k - 1] } } });
                    //}
                    g.DrawGeometry(null, p, geo);
                }
            }
        }

        public void DrawLine(SolidColor color, Point startPoint, Point endPoint, double width, LineStyles style, LineCapStyles startCap, LineCapStyles endCap)
        {
            var b = this.resourceManager.GetBrush(color);

            var p = new Pen(b, width);

            //if (startCap == LineCapStyles.Arrow)
            //{
            //    p.StartLineCap = PenLineCap.Triangle;
            //}

            //if (endCap == LineCapStyles.Arrow)
            //{
            //    p.EndLineCap = PenLineCap.Triangle;
            //}

            this.g.DrawLine(p, startPoint, endPoint);
        }
        #endregion // Line

        #region Rectangle
        public void DrawRectangle(Pen p, Rectangle rect)
        {
            if (p == null || !IsSafeDrawRect(rect))
                return;
            g.DrawRectangle(null, p, rect);
        }

        public void DrawRectangle(Pen p, double x, double y, double w, double h)
        {
            if (p == null || w <= 0 || h <= 0
                || double.IsNaN(x) || double.IsNaN(y) || double.IsNaN(w) || double.IsNaN(h))
                return;
            g.DrawRectangle(null, p, new Rect(x, y, w, h));
        }

        public void DrawRectangle(Rectangle rect, SolidColor color)
        {
            if (!IsSafeDrawRect(rect))
                return;
            var p = this.resourceManager.GetPen(color);
            if (p != null) this.g.DrawRectangle(null, p, (Rect)rect);
        }

        public void DrawRectangle(double x, double y, double width, double height, SolidColor color)
        {
            if (width <= 0 || height <= 0
                || double.IsNaN(x) || double.IsNaN(y) || double.IsNaN(width) || double.IsNaN(height))
                return;
            var p = this.resourceManager.GetPen(color);
            if (p != null)
                this.g.DrawRectangle(null, p, new Rect(x, y, width, height));
        }

        public void FillRectangle(HatchStyles style, SolidColor hatchColor, SolidColor bgColor, Rectangle rect)
        {
            // TODO
        }

        public void FillRectangle(HatchStyles style, SolidColor hatchColor, SolidColor bgColor, double x, double y, double width, double height)
        {
            // TODO
        }

        public void FillRectangle(Rectangle rect, IColor color)
        {
            if (!IsSafeDrawRect(rect))
                return;

            if (color is SolidColor)
            {
                this.g.DrawRectangle(this.resourceManager.GetBrush((SolidColor)color), null, (Rect)rect);
            }
        }

        public void FillRectangle(double x, double y, double width, double height, IColor color)
        {
            if (width <= 0 || height <= 0
                || double.IsNaN(x) || double.IsNaN(y) || double.IsNaN(width) || double.IsNaN(height))
                return;

            if (color is SolidColor)
            {
                this.g.DrawRectangle(this.resourceManager.GetBrush((SolidColor)color), null, new Rect(x, y, width, height));
            }
        }

        public void FillRectangle(RGBrush b, double x, double y, double width, double height)
        {
            if (b == null || width <= 0 || height <= 0
                || double.IsNaN(x) || double.IsNaN(y) || double.IsNaN(width) || double.IsNaN(height))
                return;

            this.g.DrawRectangle(b, null, new Rect(x, y, width, height));
        }

        public void FillRectangleLinear(SolidColor color1, SolidColor color2, double angle, Rectangle rect)
        {
            // Linux/Skia：非法矩形会导致 libSkiaSharp memmove 段错误
            if (g == null
                || rect.Width <= 0 || rect.Height <= 0
                || double.IsNaN(rect.X) || double.IsNaN(rect.Y)
                || double.IsNaN(rect.Width) || double.IsNaN(rect.Height)
                || double.IsInfinity(rect.Width) || double.IsInfinity(rect.Height))
            {
                return;
            }

            RelativePoint startPoint;
            RelativePoint endPoint;

            if (Math.Abs(angle - 0) < 1e-9)
            {
                startPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative);
                endPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative);
            }
            else if (Math.Abs(angle - 90) < 1e-9)
            {
                startPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative);
                endPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative);
            }
            else
            {
                // 未实现任意角度时回退垂直渐变，避免未初始化 Start/End 点
                startPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative);
                endPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative);
            }

            // 渲染路径使用不可变笔刷，降低跨线程/复用导致的原生崩溃风险
            var lgb = new ImmutableLinearGradientBrush(
                new[]
                {
                    new ImmutableGradientStop(0, color1),
                    new ImmutableGradientStop(1, color2),
                },
                startPoint: startPoint,
                endPoint: endPoint);

            g.DrawRectangle(lgb, null, (Rect)rect);
        }


        public void DrawRectangle(Rectangle rect, SolidColor color, double width, LineStyles lineStyle)
        {
            var p = this.resourceManager.GetPen(color, width, GetDashStyle(lineStyle));
            if (p != null)
            {
                //p = new Pen(Brushes.Black, 3);
                this.g.DrawRectangle(null, p, rect);
            }
        }

        public void DrawAndFillRectangle(Rectangle rect, SolidColor lineColor, IColor fillColor)
        {
            if (!IsSafeDrawRect(rect))
                return;

            // 填充与描边分开：笔刷/画笔任一可用时仍应绘制，避免缺 pen 时整块背景被跳过（图表透底）
            Avalonia.Media.IBrush b = null;
            if (fillColor is SolidColor solid)
                b = this.resourceManager.GetBrush(solid);
            else if (fillColor != null)
                b = this.resourceManager.GetBrush(fillColor.ToSolidColor());

            var p = this.resourceManager.GetPen(lineColor);
            if (b != null)
                this.g.DrawRectangle(b, null, (Rect)rect);
            if (p != null)
                this.g.DrawRectangle(null, p, (Rect)rect);
        }

        public void DrawAndFillRectangle(Rectangle rect, SolidColor lineColor, IColor fillColor, double width, LineStyles lineStyle)
        {
            if (!IsSafeDrawRect(rect))
                return;

            var b = fillColor != null ? this.resourceManager.GetBrush(fillColor.ToSolidColor()) : null;
            var p = this.resourceManager.GetPen(lineColor, width, GetDashStyle(lineStyle));

            // 先填后描，互不依赖；否则 pen 获取失败时白色底板整段丢失
            if (b != null)
                this.g.DrawRectangle(b, null, (Rect)rect);
            if (p != null)
                this.g.DrawRectangle(null, p, (Rect)rect);
        }
        #endregion // Rectangle

        public void DrawImage(RGImage image, double x, double y, double width, double height)
        {
            if (image != null)
            {
                g.DrawImage(image, new Rect(x, y, width, height));
            }
        }

        public void DrawImage(RGImage image, Rectangle bounds)
        {
            g.DrawImage(image, (Rect)bounds);
        }

        public void FillPolygon(Point[] points, SolidColor startColor, SolidColor endColor, double angle, Rectangle rect)
        {

            var polylineGeometry = new PolylineGeometry([.. points], true);


            var lgb = new ConicGradientBrush();
            lgb.GradientStops.Add(new GradientStop(startColor, 0));
            lgb.GradientStops.Add(new GradientStop(endColor, 1));
            lgb.Angle = angle;
            g.DrawGeometry(lgb, null, polylineGeometry);
        }

        #region Text

        public void DrawText(string text, string fontName, double size, SolidColor color, Rectangle rect)
        {
            DrawText(text, fontName, size, color, rect, ReoGridHorAlign.Left, ReoGridVerAlign.Top);
        }

        public void DrawText(string text, string fontName, double size, SolidColor color, Rectangle rect, ReoGridHorAlign halign, ReoGridVerAlign valign)
        {
            if (rect.Width > 0 && rect.Height > 0 && !string.IsNullOrEmpty(text))
            {
                FormattedText ft = new FormattedText(text, System.Threading.Thread.CurrentThread.CurrentCulture,
                     FlowDirection.LeftToRight, this.resourceManager.GetTypeface(fontName),
                     size * PlatformUtility.GetDPI() / 72.0,
                     this.resourceManager.GetBrush(color));

                ft.MaxTextWidth = rect.Width;
                ft.MaxTextHeight = rect.Height;

                switch (halign)
                {
                    default:
                        break;

                    case ReoGridHorAlign.Left:
                        ft.TextAlignment = TextAlignment.Left;
                        break;

                    case ReoGridHorAlign.Center:
                        ft.TextAlignment = TextAlignment.Center;
                        break;

                    case ReoGridHorAlign.Right:
                        ft.TextAlignment = TextAlignment.Right;
                        break;
                }

                switch (valign)
                {
                    default:
                        break;

                    case ReoGridVerAlign.Middle:
                        rect.Y += (rect.Height - ft.Height) / 2;
                        break;

                    case ReoGridVerAlign.Bottom:
                        rect.Y += (rect.Height - ft.Height);
                        break;
                }

                g.DrawText(ft, rect.Location);
            }
        }

        public Graphics.Size MeasureText(string text, string fontName, double fontSize, Graphics.Size displayArea)
        {
            // in WPF environment do not measure text, use FormattedText instead
            return new Graphics.Size(0, 0);
        }

        #endregion // Text

        #region Clip
        private Stack<PlatformGraphics.PushedState?> clipsStack = new Stack<PlatformGraphics.PushedState?>();

        /// <summary>
        /// 判断矩形是否可安全交给 Skia 裁剪/填充（Linux 上非法尺寸易 SIGSEGV）。
        /// </summary>
        protected static bool IsSafeDrawRect(Rectangle rect)
        {
            return rect.Width > 0 && rect.Height > 0
                && !double.IsNaN(rect.X) && !double.IsNaN(rect.Y)
                && !double.IsNaN(rect.Width) && !double.IsNaN(rect.Height)
                && !double.IsInfinity(rect.X) && !double.IsInfinity(rect.Y)
                && !double.IsInfinity(rect.Width) && !double.IsInfinity(rect.Height);
        }

        public void PushClip(Rectangle clipRect)
        {
            // 非法裁剪矩形：压入 null 占位，保证与 PopClip 配对，且不把坏 Rect 交给 Skia
            if (g == null || !IsSafeDrawRect(clipRect))
            {
                clipsStack.Push(null);
                return;
            }

            clipsStack.Push(g.PushClip((Rect)clipRect));
        }

        public void PopClip()
        {
            if (clipsStack.Count == 0)
                return;

            clipsStack.Pop()?.Dispose();
        }
        #endregion // Clip

        #region Transform
        private Stack<(Matrix matrix, PlatformGraphics.PushedState state)> transformStack = new();

        public void PushTransform()
        {
            this.PushTransform(Matrix.Identity);
        }

        public void PushTransform(Matrix m)
        {
            this.transformStack.Push((m, this.g.PushTransform(m)));
        }

        Matrix IGraphics.PopTransform()
        {
            return this.PopTransform();
        }

        public Matrix PopTransform()
        {
            var (m, state) = transformStack.Pop();
            state.Dispose();
            return m;
        }

        public void TranslateTransform(double x, double y)
        {
            if (transformStack.Count > 0)
            {
                var m = PopTransform();
                var m2 = Matrix.CreateTranslation(x, y);
                PushTransform(m * m2);
            }
        }

        public void ScaleTransform(double x, double y)
        {
            if (x != 0 && y != 0
                && x != 1 && y != 1
                && transformStack.Count > 0)
            {
                var m = PopTransform();
                var m2 = Matrix.CreateScale(x, y);
                PushTransform(m * m2);
            }
        }

        public void RotateTransform(double angle)
        {
            if (transformStack.Count > 0)
            {
                var m2 = Matrix.CreateRotation(angle);
                var m = PopTransform();
                PushTransform(m * m2);
            }
        }

        public void ResetTransform()
        {
            if (transformStack.Count > 0)
            {
                PopTransform();
                PushTransform(Matrix.Identity);
            }
        }
        #endregion // Transform

        #region Ellipse

        public void DrawEllipse(SolidColor color, Rectangle rectangle)
        {
            var p = this.resourceManager.GetPen(color);
            if (p != null)
            {
                this.g.DrawEllipse(null, p, new Point(rectangle.X + rectangle.Width / 2,
                        rectangle.Y + rectangle.Height / 2), rectangle.Width, rectangle.Height);
            }
        }

        public void DrawEllipse(SolidColor color, double x, double y, double width, double height)
        {
            var p = this.resourceManager.GetPen(color);
            if (p != null)
            {
                this.g.DrawEllipse(null, p, new Point(x, y), width, height);
            }
        }

        public void DrawEllipse(RGPen pen, Rectangle rectangle)
        {
            this.g.DrawEllipse(null, pen, rectangle.Location, rectangle.Width, rectangle.Height);
        }

        public void FillEllipse(RGBrush b, Rectangle rectangle)
        {
            this.g.DrawEllipse(b, null, rectangle.Location, rectangle.Width, rectangle.Height);
        }

        public void FillEllipse(RGBrush b, double x, double y, double width, double height)
        {
            this.g.DrawEllipse(b, null, new Point(x, y), width, height);
        }

        #endregion // Ellipse

        #region Polygon
        public void DrawPolygon(SolidColor color, double width, LineStyles style, params Graphics.Point[] points)
        {
            this.DrawLines(points, 0, points.Length, color, width, style);
        }

        public void FillPolygon(IColor color, params Graphics.Point[] points)
        {
            // 原实现未设置 StartPoint/IsClosed，几何无效；Linux Skia 上可能导致原生崩溃
            if (g == null || color.IsTransparent || points == null || points.Length < 3)
                return;

            var figure = new PathFigure
            {
                StartPoint = points[0],
                IsClosed = true,
                IsFilled = true,
            };
            for (int i = 1; i < points.Length; i++)
            {
                figure.Segments.Add(new LineSegment { Point = points[i] });
            }

            var geo = new PathGeometry();
            geo.Figures.Add(figure);
            g.DrawGeometry(new SolidColorBrush(color.ToSolidColor()), null, geo);
        }
        #endregion // Polygon

        #region Utility
        public bool IsAntialias { get { return true; } set { } }

        public void Reset()
        {
            // 上一帧 DrawingContext 已失效：不可 Dispose 旧 PushedState（会踩已释放原生资源）。
            // 必须清空 clips/transform，避免下一帧 Pop 到跨帧残留导致 Linux Skia 段错误。
            clipsStack.Clear();
            transformStack.Clear();
        }

        internal void SetPlatformGraphics(PlatformGraphics dc)
        {
            this.g = dc;
        }
        #endregion // Utility

        internal RGDashStyle GetDashStyle(LineStyles style) => style switch
        {
            LineStyles.Dot => DashStyles.Dot,
            LineStyles.Dash => DashStyles.Dash,
            LineStyles.DashDot => DashStyles.DashDot,
            LineStyles.DashDotDot => DashStyles.DashDotDot,
            _ => DashStyles.Solid,
        };

        #region Path
        public void FillPath(IColor color, Geometry graphicsPath)
        {
            var b = this.resourceManager.GetBrush(color.ToSolidColor());
            if (b != null) this.g.DrawGeometry(b, null, graphicsPath);
        }

        public void DrawPath(SolidColor color, Geometry graphicsPath)
        {
            var p = this.resourceManager.GetPen(color);
            if (p != null) this.g.DrawGeometry(null, p, graphicsPath);
        }
        #endregion // Path



        public void FillEllipse(IColor fillColor, Rectangle rect)
        {
            var b = this.resourceManager.GetBrush(fillColor.ToSolidColor());

            if (b != null)
            {
                this.g.DrawEllipse(b, null, rect.Origin, rect.Width, rect.Height);
            }
        }

    }
    #endregion // Graphics

    #region Renderer
    internal class AvaloniaRenderer : AvaloniaGraphics, IRenderer
    {
        protected Avalonia.Media.Typeface? headerTextTypeface;

        internal AvaloniaRenderer()
        {
            this.headerTextTypeface = PlatformUtility.GetFontDefaultTypeface(FontManager.Current.SystemFonts.First());
        }

        public Graphics.Size MeasureCellText(Cell cell, DrawMode drawMode, double scale)
        {
            if (cell == null)
                return Graphics.Size.Zero;

            // 粘贴后可能仍残留旧 FormattedText，或 FontDirty 未刷；与 DrawCellText 一致先更新。
            if (cell.formattedText == null || cell.FontDirty)
            {
                var sheet = cell.Worksheet;
                if (sheet == null)
                    return Graphics.Size.Zero;
                sheet.UpdateCellFont(cell);
                if (cell.formattedText == null)
                    return Graphics.Size.Zero;
            }

            if (cell.InnerStyle != null && cell.InnerStyle.RotationAngle != 0)
            {
                Matrix m = Matrix.Identity;

                double hw = cell.formattedText.Width * 0.5, hh = cell.formattedText.Height * 0.5;
                Avalonia.Point p1 = new(-hw, -hh), p2 = new(hw, hh);
                //m.Rotate(cell.InnerStyle.RotationAngle);
                m = Matrix.CreateRotation(cell.InnerStyle.RotationAngle);
                p1 *= m; p2 *= m;
                return new Graphics.Size(Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
            }
            else
            {
                return new Graphics.Size(cell.formattedText.Width, cell.formattedText.Height);
            }
        }

        public void DrawCellText(Cell cell, SolidColor textColor, DrawMode drawMode, double scale)
        {
            var sheet = cell?.Worksheet;

            if (sheet == null) return;

            // FontDirty 时即使已有 FormattedText 也要重建，否则粘贴数值不刷新显示。
            if (cell.formattedText == null || cell.FontDirty)
            {
                sheet.UpdateCellFont(cell);
            }

            if (cell.formattedText == null)
                return;

            // Linux/Skia：非法坐标的 DrawText 可能原生崩溃
            var loc = cell.TextBounds.Location;
            if (double.IsNaN(loc.X) || double.IsNaN(loc.Y)
                || double.IsInfinity(loc.X) || double.IsInfinity(loc.Y))
                return;

            if (cell.InnerStyle != null && cell.InnerStyle.RotationAngle != 0)
            {
                Matrix m = Avalonia.Matrix.Identity;
                //m.Rotate(cell.InnerStyle.RotationAngle);
                //m.Translate(cell.Bounds.OriginX * sheet.ScaleFactor, cell.Bounds.OriginY * sheet.ScaleFactor);
                var m1 = Matrix.CreateRotation(cell.InnerStyle.RotationAngle);
                var m2 = Matrix.CreateTranslation(cell.Bounds.OriginX * sheet.ScaleFactor, cell.Bounds.OriginY * sheet.ScaleFactor);

                this.PushTransform(m1 * m2);
                this.PlatformGraphics.DrawText(cell.formattedText, new RGPointF(-cell.formattedText.Width * 0.5, -cell.formattedText.Height * 0.5));
                this.PopTransform();
            }
            else
            {
                this.PlatformGraphics.DrawText(cell.formattedText, loc);
            }
        }

        private static Color DecideTextColor(Cell cell)
        {
            var sheet = cell.Worksheet;
            var controlStyle = sheet.controlAdapter.ControlStyle;
            SolidColor textColor;

            if (!cell.RenderColor.IsTransparent)
            {
                textColor = cell.RenderColor;
            }
            else if (cell.InnerStyle.HasStyle(PlainStyleFlag.TextColor))
            {
                // cell text color, specified by SetRangeStyle
                textColor = cell.InnerStyle.TextColor;
            }
            else if (!controlStyle.TryGetColor(ControlAppearanceColors.GridText, out textColor))
            {
                // default cell text color
                textColor = SolidColor.Black;
            }

            return textColor;
        }

        public void UpdateCellRenderFont(Cell cell, Core.UpdateFontReason reason)
        {
            var sheet = cell.Worksheet;
            if (sheet == null || sheet.controlAdapter == null) return;

            double dpi = PlatformUtility.GetDPI();
            double fontSize = cell.InnerStyle.FontSize * sheet.renderScaleFactor * dpi / 72.0;

            if (cell.formattedText == null || cell.formattedText.ToString() != (cell.InnerDisplay ?? string.Empty))
            {
                SolidColor textColor = DecideTextColor(cell);

                // InnerDisplay 在粘贴空合并区时可能为 null。
                cell.formattedText = new Avalonia.Media.FormattedText(cell.InnerDisplay ?? string.Empty,
                    System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    Typeface.Default,
                    fontSize,
                    base.resourceManager.GetBrush(textColor));

                // 新建 FormattedText 后立即套用单元格字体名（勿停留在 Typeface.Default）
                if (!string.IsNullOrEmpty(cell.InnerStyle.FontName))
                    cell.formattedText.SetFontFamily(cell.InnerStyle.FontName);
            }
            else if (reason == Core.UpdateFontReason.FontChanged || reason == Core.UpdateFontReason.ScaleChanged)
            {
                cell.formattedText.SetFontFamily(cell.InnerStyle.FontName);
                cell.formattedText.SetFontSize(fontSize);
            }
            else if (reason == Core.UpdateFontReason.TextColorChanged)
            {
                SolidColor textColor = DecideTextColor(cell);
                cell.formattedText.SetForegroundBrush(resourceManager.GetBrush(textColor));
            }

            var ft = cell.formattedText;

            // 同步自动换行：NoWrap 必须用 Infinity，切勿设为 0（Avalonia 会按行宽 0 排版导致文字不可见）
            if (cell.InnerStyle.TextWrapMode != TextWrapMode.NoWrap)
            {
                double cellWidth = Math.Max(1, cell.Bounds.Width * sheet.renderScaleFactor - 4);
                ft.MaxTextWidth = cellWidth;
                ft.Trimming = TextTrimming.None;
            }
            else
            {
                ft.MaxTextWidth = double.PositiveInfinity;
            }

            if (reason == Core.UpdateFontReason.FontChanged || reason == Core.UpdateFontReason.ScaleChanged)
            {
                ft.SetFontWeight(
                    cell.InnerStyle.Bold ? FontWeight.Bold : FontWeight.Normal);

                ft.SetFontStyle(PlatformUtility.ToAvaloniaFontStyle(cell.InnerStyle.fontStyles));

                ft.SetTextDecorations(PlatformUtility.ToWPFFontDecorations(cell.InnerStyle.fontStyles));
            }
        }

        public void DrawRunningFocusRect(double x, double y, double w, double h, SolidColor color, int runningOffset)
        {

        }

        private Pen capLinePen = null;

        public void BeginCappedLine(LineCapStyles startCap, Graphics.Size startSize, LineCapStyles endCap, Graphics.Size endSize,
            SolidColor color, double width)
        {
            capLinePen = new Pen(new SolidColorBrush(color), width);
            //capLinePen.StartLineCap = PlatformUtility.ToWPFLineCap(startCap);
            //capLinePen.EndLineCap = PlatformUtility.ToWPFLineCap(endCap);
        }

        private LineCap lineCap;

        public void DrawCappedLine(double x1, double y1, double x2, double y2)
        {
            if (this.capLinePen != null)
            {
                base.DrawLine(this.capLinePen, x1, y1, x2, y2);
            }
        }

        public void EndCappedLine()
        {
            this.capLinePen = null;
        }

        private Pen cachePen = null;

        public void BeginDrawLine(double width, SolidColor color)
        {
            cachePen = new Pen(new SolidColorBrush(color), width);
        }

        public void DrawLine(double x1, double y1, double x2, double y2)
        {
            base.DrawLine(this.cachePen, new RGPointF(x1, y1), new RGPointF(x2, y2));
        }

        public void EndDrawLine()
        {
        }

        public void DrawLeadHeadArrow(Rectangle bounds, SolidColor startColor, SolidColor endColor)
        {
        }

        public Pen GetPen(SolidColor color)
        {
            return this.resourceManager.GetPen(color);
        }

        public void ReleasePen(Pen pen) { }

        public RGBrush GetBrush(SolidColor color)
        {
            return this.resourceManager.GetBrush(color);
        }

        public RGFont GetFont(string name, double size, Drawing.Text.FontStyles style)
        {
            return this.resourceManager.GetTypeface(name, FontWeight.Normal, FontStyle.Normal, FontStretch.Normal);
        }

        private double headerTextScale = 9d;

        public void BeginDrawHeaderText(double scale)
        {
            this.headerTextScale = 9d * scale;
        }

        public void DrawHeaderText(string text, RGBrush brush, Rectangle rect)
        {
            // Linux/Skia：非法表头矩形或空画笔时跳过，避免原生崩溃
            if (this.PlatformGraphics == null || brush == null || string.IsNullOrEmpty(text) || !IsSafeDrawRect(rect))
                return;

            double fontSize = headerTextScale / 72d * 96d;
            if (fontSize < 0.5 || double.IsNaN(fontSize) || double.IsInfinity(fontSize))
                return;

            var ft = new Avalonia.Media.FormattedText(text,
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                Typeface.Default, fontSize, brush);

            double x = rect.X + (rect.Width - ft.Width) / 2;
            double y = rect.Y + (rect.Height - ft.Height) / 2;
            if (double.IsNaN(x) || double.IsNaN(y) || double.IsInfinity(x) || double.IsInfinity(y))
                return;

            this.PlatformGraphics.DrawText(ft, new Point(x, y));
        }

        public ResourcePoolManager GetResourcePoolManager
        {
            get { return this.resourceManager; }
        }
    }

    #endregion // Renderer

}

#endif // WPF