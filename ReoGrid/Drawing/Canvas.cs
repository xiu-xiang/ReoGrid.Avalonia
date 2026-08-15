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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using unvell.ReoGrid.Graphics;
using unvell.ReoGrid.Interaction;
using unvell.ReoGrid.Rendering;
using unvell.ReoGrid.Views;

namespace unvell.ReoGrid.Drawing
{
	internal interface IDrawingCanvas : IDrawingContainer
	{

	}

	internal class DrawingCanvas : DrawingComponent, IDrawingCanvas
	{
		public DrawingCanvas()
		{

		}
	}

	internal class WorksheetDrawingCanvas : DrawingCanvas
	{
		internal Worksheet Worksheet { get; set; }

		/// <summary>当前正在拖拽的顶层浮动对象（图表/形状等）。</summary>
		private DrawingObject draggingObject;
		private Point dragMouseStart;
		private Point dragObjectStart;
		private bool isDragging;

		public WorksheetDrawingCanvas(Worksheet sheet)
		{
			this.Worksheet = sheet;
		}

		public override void Invalidate()
		{
			if (this.Worksheet != null)
			{
				this.Worksheet.RequestInvalidate();
			}
		}

		/// <summary>
		/// Worksheet Drawing Canvas alwayas keep transparent and doesn't draw anything from itself
		/// </summary>
		/// <param name="dc">Platform no-associated drawing context instance.</param>
		protected override void OnPaint(DrawingContext dc)
		{
			dc.Graphics.IsAntialias = true;

			base.DrawChildren(dc);

			dc.Graphics.IsAntialias = false;
		}

		internal WorksheetDrawingObjectCollection worksheetDrawingObjectCollection;

		public override IDrawingObjectCollection Children
		{
			get
			{
				if (this.worksheetDrawingObjectCollection == null)
				{
					this.worksheetDrawingObjectCollection = new WorksheetDrawingObjectCollection(this);
				}

				return this.worksheetDrawingObjectCollection;
			}
		}

		internal void Clear()
		{
			this.Children.Clear();
		}

		/// <summary>取消全部浮动对象选中并结束拖拽（点击单元格时调用）。</summary>
		internal void ClearFloatingSelection()
		{
			bool changed = this.isDragging;
			this.isDragging = false;
			this.draggingObject = null;

			var children = this.Children;
			if (children != null)
			{
				foreach (var child in children)
				{
					if (child is SelectableFloatingObject selectable && selectable.IsSelected)
					{
						selectable.IsSelected = false;
						changed = true;
					}
				}
			}

			if (changed)
				this.Invalidate();
		}

		#region 浮动对象选中与拖拽
		public override bool OnMouseDown(Point location, MouseButtons button)
		{
			if (button != MouseButtons.Left)
				return base.OnMouseDown(location, button);

			var children = this.Children;
			if (children == null || children.Count <= 0)
				return false;

			// 自上而下命中顶层浮动对象（图表等），不深入子控件以免拦截整体拖动
			for (int i = children.Count - 1; i >= 0; i--)
			{
				if (children[i] is not DrawingObject obj || !obj.Visible)
					continue;
				if (!obj.Bounds.Contains(location))
					continue;

				// 单选：清除其余对象选中状态
				foreach (var child in children)
				{
					if (child is SelectableFloatingObject selectable && !ReferenceEquals(selectable, obj))
						selectable.IsSelected = false;
				}

				obj.IsSelected = true;
				this.draggingObject = obj;
				this.dragMouseStart = location;
				this.dragObjectStart = obj.Location;
				this.isDragging = true;

				// 通知对象自身（相对坐标），便于外部订阅 MouseDown
				obj.OnMouseDown(new Point(location.X - obj.X, location.Y - obj.Y), button);
				this.Invalidate();
				return true;
			}

			// 点在空白处：取消全部选中
			foreach (var child in children)
			{
				if (child is SelectableFloatingObject selectable)
					selectable.IsSelected = false;
			}

			this.isDragging = false;
			this.draggingObject = null;
			this.Invalidate();
			return false;
		}

		public override bool OnMouseMove(Point location, MouseButtons buttons)
		{
			if (this.isDragging && this.draggingObject != null
				&& (buttons & MouseButtons.Left) == MouseButtons.Left)
			{
				RGFloat dx = location.X - this.dragMouseStart.X;
				RGFloat dy = location.Y - this.dragMouseStart.Y;
				this.draggingObject.Location = new Point(
					this.dragObjectStart.X + dx,
					this.dragObjectStart.Y + dy);
				this.Invalidate();
				return true;
			}

			return base.OnMouseMove(location, buttons);
		}

		public override bool OnMouseUp(Point location, MouseButtons buttons)
		{
			if (this.isDragging)
			{
				this.isDragging = false;
				this.draggingObject = null;
				this.Invalidate();
				return true;
			}

			return base.OnMouseUp(location, buttons);
		}
		#endregion // 浮动对象选中与拖拽
	}

	internal class WorksheetDrawingObjectCollection : DrawingObjectCollection
	{
		private WorksheetDrawingCanvas owner;

		internal WorksheetDrawingObjectCollection(WorksheetDrawingCanvas owner)
			: base(owner)
		{
			this.owner = owner;
		}

		public override void Add(IDrawingObject item)
		{
			base.Add(item);

			if (this.owner.Worksheet != null) this.owner.Worksheet.RequestInvalidate();
		}

		public override void AddRange(IEnumerable<IDrawingObject> drawingObjects)
		{
			base.AddRange(drawingObjects);

			if (this.owner.Worksheet != null) this.owner.Worksheet.RequestInvalidate();
		}

		public override bool Remove(IDrawingObject item)
		{
			bool ret = base.Remove(item);

			if (ret && this.owner.Worksheet != null) this.owner.Worksheet.RequestInvalidate();

			return ret;
		}

		public override void Clear()
		{
			base.Clear();

			if (this.owner.Worksheet != null) this.owner.Worksheet.RequestInvalidate();
		}

		public override IDrawingObject this[int index]
		{
			get
			{
				var ret = base[index];

				if (this.owner.Worksheet != null) this.owner.Worksheet.RequestInvalidate();

				return ret;
			}
		}
	}
}

#endif // DRAWING