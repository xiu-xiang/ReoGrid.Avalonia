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

using System;
using System.Collections.Generic;

#if WINFORM || ANDROID
using RGFloat = System.Single;
using RGPoint = unvell.ReoGrid.Graphics.Point;
#elif WPF
using RGFloat = System.Double;
using RGPoint = unvell.ReoGrid.Graphics.Point;
using System.Windows.Controls;
#elif iOS
using RGFloat = System.Double;
#endif // WINFORM

using unvell.ReoGrid.Graphics;
using unvell.ReoGrid.Rendering;
using unvell.ReoGrid.Interaction;
using System.Runtime.CompilerServices;

namespace unvell.ReoGrid.Data
{
	#region Auot Filter
	/// <summary>
	/// Built-in auto column filter
	/// </summary>
	public class AutoColumnFilter
	{
		//internal Worksheet innerWorksheet { get; set; }

		/// <summary>
		/// Get instance of current attached worksheet.
		/// </summary>
		public Worksheet Worksheet { get; private set; }

		private AutoColumnFilterUI columnFilterUIFlag;

		public RangePosition ApplyRange { get; private set; }

		//internal RangePosition ApplyRange
		//{
		//	get
		//	{
		//		return new RangePosition(this.titleRows, this.startCol, -1, this.endCol - this.startCol + 1);
		//	}
		//}

		internal AutoColumnFilter(Worksheet worksheet, /*int startCol, int endCol, int titleRows = 1*/ RangePosition range)
		{
//#if DEBUG
//			Debug.Assert(endCol >= startCol);
//#endif

//			if (startCol < 0) throw new ArgumentOutOfRangeException("start number of column out of spreadsheet");
//			if (startCol >= worksheet.ColumnCount) throw new ArgumentOutOfRangeException("start number of column out of spreadsheet");

//			if (endCol < 0) throw new ArgumentOutOfRangeException("end number of column out of spreadsheet");
//			if (endCol >= worksheet.ColumnCount) throw new ArgumentOutOfRangeException("end number of column out of spreadsheet");

//			if (endCol < startCol) throw new ArgumentOutOfRangeException("end number of column must be greater than start number of column");

			this.Worksheet = worksheet;
			this.ApplyRange = range;
			//this.startCol = startCol;
			//this.endCol = endCol;
			//this.titleRows = titleRows;
		}

		private FilterColumnCollection columnCollection;

		/// <summary>
		/// Get the columns of this filter.
		/// </summary>
		public FilterColumnCollection Columns
		{
			get
			{
				if (this.columnCollection == null) this.columnCollection = new FilterColumnCollection(this);
				return this.columnCollection;
			}
		}

		/// <summary>
		/// Apply filter to update worksheet.
		/// </summary>
		public void Apply()
		{
			if (this.Worksheet == null) return;

			//int endRow = this.Worksheet.MaxContentRow;

			this.Worksheet.DoFilter(this.ApplyRange, r =>
			{
				for (int c = this.ApplyRange.Col; c <= this.ApplyRange.EndCol;)
				{
					var columnHeader = this.Worksheet.RetrieveColumnHeader(c);
					if (columnHeader == null) { c++; continue; }

					var columnFilterBody = columnHeader.Body as AutoColumnFilterBody;
					if (columnFilterBody == null || columnFilterBody.IsSelectAll == true)
					{
						c++; continue;
					}

					var cell = this.Worksheet.GetCell(r, c);
          if (cell == null)
          {
            if (!columnFilterBody.ContainsBlank)
            {
              return false;
            }

            c++;
            continue;
          }

          var text = cell.DisplayText;
          if (string.IsNullOrEmpty(text) && !columnFilterBody.ContainsBlank)
          {
            return false;
          }

					if (!columnFilterBody.SelectedTextItems.Contains(text))
					{
						// hide the row
						return false;
					}

          c += cell == null ? 1 : cell.Colspan;
        }

				// show the row
				return true;
			});
		}

		#region FilterColumnCollection
		/// <summary>
		/// Collection of column filters
		/// </summary>
		public class FilterColumnCollection : IEnumerable<AutoColumnFilterBody>
		{
			private AutoColumnFilter filter;

			internal FilterColumnCollection(AutoColumnFilter filter)
			{
				this.filter = filter;
			}

			/// <summary>
			/// Get the column filter by specified index.
			/// </summary>
			/// <param name="index">number of column to get column filter.</param>
			/// <returns>instance of column filter, which contains the candidates list and selected items by user.</returns>
			public AutoColumnFilterBody this[int index]
			{
				get
				{
					if (index < this.filter.ApplyRange.Col || index > this.filter.ApplyRange.EndCol
						|| index < 0 || index >= this.filter.Worksheet.ColumnCount)
						throw new ArgumentOutOfRangeException("index", "Number of column to find the filter out of the valid range");

					return this.filter.Worksheet.RetrieveColumnHeader(index).Body as AutoColumnFilterBody;
				}
			}

			/// <summary>
			/// Get the column filter by specified address code of column (A, TZ, etc.)
			/// </summary>
			/// <param name="columnCode">the alphabet of address code used to locate a column in spreadsheet</param>
			/// <returns>instance of column filter, which contains the candidates list and selected items by user</returns>
			public AutoColumnFilterBody this[string columnCode]
			{
				get
				{
					int index = RGUtility.GetNumberOfChar(columnCode);
					return this[index];
				}
			}

			private IEnumerator<AutoColumnFilterBody> GetEnum()
			{
				for (int i = this.filter.ApplyRange.Col; i <= this.filter.ApplyRange.EndCol; i++)
				{
					var header = this.filter.Worksheet.RetrieveColumnHeader(i);
					if (header == null) continue;
					var body = header.Body as AutoColumnFilterBody;
					if (body == null) continue;
					yield return body;
				}
			}

			/// <summary>
			/// Get all column filter header body from this filter.
			/// </summary>
			/// <returns></returns>
			public IEnumerator<AutoColumnFilterBody> GetEnumerator()
			{
				return this.GetEnum();
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return this.GetEnum();
			}
		}
		#endregion // FilterColumnCollection

		#region IColumnFilter Members

		#endregion

		#region AutoColumnFilterBody
		public class AutoColumnFilterBody : unvell.ReoGrid.CellTypes.IHeaderBody
		{
			internal AutoColumnFilter autoFilter;

			/// <summary>
			/// Column header instance
			/// </summary>
			public ColumnHeader ColumnHeader { get; private set; }

			internal AutoColumnFilterBody(AutoColumnFilter autoFilter, ColumnHeader header)
			{
				this.autoFilter = autoFilter;
				this.ColumnHeader = header;
				this.DataDirty = true;
				this.IsSelectAll = true;
			}

			internal bool IsDropdown { get; set; }

			/// <summary>
			/// Repaint filter header body
			/// </summary>
			/// <param name="dc">ReoGrid drawing context</param>
			/// <param name="headerSize">Header size</param>
			public void OnPaint(CellDrawingContext dc, Size headerSize)
			{
				var controlStyle = dc.Worksheet.controlAdapter.ControlStyle;

				if (this.autoFilter.columnFilterUIFlag == AutoColumnFilterUI.DropdownButton
					|| this.autoFilter.columnFilterUIFlag == AutoColumnFilterUI.DropdownButtonAndPanel)
				{
					Rectangle bounds = GetColumnFilterButtonRect(headerSize);

					// Linux/Skia：宽高非正则跳过绘制，避免 libSkiaSharp 段错误
					if (bounds.Width < 1 || bounds.Height < 1)
						return;

          SolidColor color1 = controlStyle.GetColHeadStartColor(isHover: false, isInvalid: false,
            isSelected: IsDropdown, isFullSelected: IsSelectAll != true);

          SolidColor color2 = controlStyle.GetColHeadEndColor(isHover: false, isInvalid: false,
            isSelected: IsDropdown, isFullSelected: IsSelectAll != true);

          var g = dc.Graphics;

					g.FillRectangleLinear(color1, color2, 90f, bounds);

					g.DrawRectangle(bounds, unvell.ReoGrid.Rendering.StaticResources.SystemColor_ControlDark);

					var triSize = Math.Min(7 * dc.Worksheet.renderScaleFactor, 7.0);
					var center = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

#if AVALONIA
					// Avalonia：用 Path 几何绘制，避免逐像素 DrawLine 在 Linux Skia 上崩溃
					PaintFilterGlyph(g, center, triSize, IsSelectAll == true);
#else
          if (IsSelectAll == true)
            Common.GraphicsToolkit.FillTriangle(dc.Graphics.PlatformGraphics,
              triSize, center, Common.GraphicsToolkit.TriangleDirection.Down);
          else
            Common.GraphicsToolkit.FillTriangle(dc.Graphics.PlatformGraphics,
              triSize, center, Common.GraphicsToolkit.TriangleDirection.DownFilter);
#endif
        }
			}

#if AVALONIA
			/// <summary>
			/// 绘制列头筛选箭头/漏斗（Avalonia 路径，规避 Skia 原生崩溃）。
			/// </summary>
			private static void PaintFilterGlyph(IGraphics g, Point center, double size, bool selectAll)
			{
				if (g == null || size < 2)
					return;

				if (selectAll)
				{
					// 向下实心三角
					g.FillPolygon(SolidColor.Black,
						new Point(center.X - size / 2, center.Y - size / 4),
						new Point(center.X + size / 2, center.Y - size / 4),
						new Point(center.X, center.Y + size / 4));
				}
				else
				{
					// 漏斗示意：上宽下窄 + 短颈
					double topW = size;
					double bottomW = Math.Max(1, size / 4);
					double funnelH = size / 2;
					double neckH = size / 2;
					double left = center.X - topW / 2;
					double top = center.Y - size / 2;
					double neckLeft = left + (topW - bottomW) / 2;
					double neckRight = left + (topW + bottomW) / 2;
					double funnelBottom = top + funnelH;

					g.DrawPolygon(SolidColor.Black, 1, LineStyles.Solid,
						new Point(left, top),
						new Point(left + topW, top),
						new Point(neckRight, funnelBottom),
						new Point(neckRight, funnelBottom + neckH),
						new Point(neckLeft, funnelBottom + neckH),
						new Point(neckLeft, funnelBottom));
				}
			}
#endif

			/// <summary>
			/// Handling mouse-down process
			/// </summary>
			/// <param name="headerSize">Header size</param>
			/// <param name="e">Argument of mouse-down event</param>
			/// <returns>True if event has been handled; otherwise return false</returns>
			public bool OnMouseDown(Size headerSize, Events.WorksheetMouseEventArgs e)
			{
				if (this.autoFilter == null
					|| this.autoFilter.columnFilterUIFlag == AutoColumnFilterUI.NoGUI
					|| this.ColumnHeader == null || this.ColumnHeader.Worksheet == null) return false;

				if (IsMouseInButton(headerSize, e.RelativePosition))
				{
					return this.autoFilter.RaiseFilterButtonPress(this, e.AbsolutePosition);
				}
				else
					return false;
			}

			/// <summary>
			/// Handling mouse-move process
			/// </summary>
			/// <param name="headerSize">Header size</param>
			/// <param name="e">Argument of mouse-move event</param>
			/// <returns>True if event has been handled, otherwise return false</returns>
			public bool OnMouseMove(Size headerSize, Events.WorksheetMouseEventArgs e)
			{
				if (this.autoFilter == null
					|| this.autoFilter.columnFilterUIFlag == AutoColumnFilterUI.NoGUI
					|| this.ColumnHeader == null || this.ColumnHeader.Worksheet == null) return false;

				if (IsMouseInButton(headerSize, e.RelativePosition))
				{
					e.CursorStyle = CursorStyle.Hand;
					return true;
				}
				else
					return false;
			}

			private bool IsMouseInButton(Size headerSize, Point position)
			{
				Rectangle bounds = GetColumnFilterButtonRect(headerSize);
				//Console.WriteLine(bounds.ToString() + " ---- " + position.ToString());
				return bounds.Contains(position);
			}

			internal Rectangle GetColumnFilterButtonRect(Size size)
			{
				var sheet = this.ColumnHeader.Worksheet;

				RGFloat scale = sheet.renderScaleFactor;

				// 钳制为正尺寸，防止窄列时 Width/Height 为负传入 Skia
				RGFloat btnW = Math.Min(Math.Min(Math.Max(0, size.Width - 2), 18f * scale), 20);
				RGFloat btnH = Math.Min(Math.Min(Math.Max(0, size.Height - 2), 18 * scale), 20);
				if (btnW < 1 || btnH < 1)
					return new Rectangle(0, 0, 0, 0);

				Rectangle bounds = new Rectangle(0, 0, btnW, btnH);
				bounds.X = size.Width - bounds.Width - 2;
				bounds.Y = (size.Height - bounds.Height) / 2 - 1;

				return bounds;
			}

#if WINFORM
			/// <summary>
			/// Get or set the context menu strip of column filter.
			/// </summary>
			public System.Windows.Forms.ContextMenuStrip ContextMenuStrip { get; set; }
#elif WPF
      /// <summary>
      /// Get or set the context menu of column filter.
      /// </summary>
      public ContextMenu ContextMenuStrip { get; set; }

#elif AVALONIA
			public unvell.ReoGrid.AvaloniaPlatform.ColumnFilterContextMenu ContextMenu { get; set; }
#endif

      internal readonly List<string> selectedTextItems = new List<string>();

			/// <summary>
			/// Gets or sets the SelectAll state for this column filter.
			/// When <c>true</c>, all items are selected and this column is ignored during filtering;
			/// when <c>false</c>, no items are selected; when <c>null</c>, the column is partially selected.
			/// </summary>
			public bool? IsSelectAll { get; set; }

      public bool ContainsBlank { get; set; }

      private TextFilterCollection textItemsCollection;

			/// <summary>
			/// Collection of selected items
			/// </summary>
			public TextFilterCollection SelectedTextItems
			{
				get
				{
					if (this.textItemsCollection == null)
					{
						this.textItemsCollection = new TextFilterCollection(this);
					}

					return this.textItemsCollection;
				}
			}

			#region TextFilterCollection
			/// <summary>
			/// Collection of selected items for column filter
			/// </summary>
			public class TextFilterCollection : ICollection<string>
			{
				private AutoColumnFilterBody columnFilter;

				internal TextFilterCollection(AutoColumnFilterBody column)
				{
					this.columnFilter = column;
				}

				/// <summary>
				/// Check whether or not a specified item is selected by user.
				/// </summary>
				/// <param name="item">Item to be checked.</param>
				/// <returns>True if item is selected by user; Otherwise return false;</returns>
				public bool this[string item]
				{
					get
					{
						return columnFilter.selectedTextItems.Contains(item);
					}
					set
					{
						if (value)
						{
							Add(item);
						}
						else
						{
							Remove(item);
						}
					}
				}

				/// <summary>
				/// Get the enumeration of items from this filter.
				/// </summary>
				/// <returns>Enumeration of items from this filter.</returns>
				public IEnumerator<string> GetEnumerator()
				{
					return columnFilter.selectedTextItems.GetEnumerator();
				}

				System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
				{
					return columnFilter.selectedTextItems.GetEnumerator();
				}

				/// <summary>
				/// Add selected item
				/// </summary>
				/// <param name="item">item to be added</param>
				public void Add(string item)
				{
					if (!this.columnFilter.selectedTextItems.Contains(item))
					{
						this.columnFilter.selectedTextItems.Add(item);
						//this.columnFilter.IsSelectAll = false;
					}
				}

				/// <summary>
				/// Clear all selected items
				/// </summary>
				public void Clear()
				{
					this.columnFilter.selectedTextItems.Clear();
					//columnFilter.IsSelectAll = false;
				}

				/// <summary>
				/// Check whether the specified item is contained in selected items
				/// </summary>
				/// <param name="item">item to be checked</param>
				/// <returns>true if specified item has been contained in selected items, otherwise return false</returns>
				public bool Contains(string item)
				{
					return this.columnFilter.selectedTextItems.Contains(item);
				}

				/// <summary>
				/// Copy all selected items into specified array
				/// </summary>
				/// <param name="array">array to be filled</param>
				/// <param name="arrayIndex">number of element start to copy</param>
				public void CopyTo(string[] array, int arrayIndex)
				{
					this.columnFilter.selectedTextItems.CopyTo(array, arrayIndex);
				}

				/// <summary>
				/// Get number of selected items
				/// </summary>
				public int Count
				{
					get { return this.columnFilter.selectedTextItems.Count; }
				}

				/// <summary>
				/// Check whether or not the collection of selection items is read-only
				/// </summary>
				public bool IsReadOnly
				{
					get { return false; }
				}

				/// <summary>
				/// Remove specified item from selected items
				/// </summary>
				/// <param name="item">item to be removed</param>
				/// <returns>true if item exist and has been removed successfully</returns>
				public bool Remove(string item)
				{
					//this.columnFilter.IsSelectAll = false;
					return this.columnFilter.selectedTextItems.Remove(item);
				}

				/// <summary>
				/// Add entire specified array or enumerable list into selected items
				/// </summary>
				/// <param name="items">list, array or other enumerable collection to be added</param>
				public void AddRange(IEnumerable<string> items)
				{
					this.columnFilter.selectedTextItems.AddRange(items);
					//this.columnFilter.IsSelectAll = false;
				}
			}
			#endregion // TextFilterCollection

			/// <summary>
			/// Get distinct items from spreadsheet on current column
			/// </summary>
			/// <returns></returns>
			public List<string> GetDistinctItems()
			{
				if (this.ColumnHeader == null || this.ColumnHeader.Worksheet == null) return null;

				List<string> items = new List<string>();
				var worksheet = this.ColumnHeader.Worksheet;
				int col = this.ColumnHeader.Index;
				int startRow = Math.Max(0, this.autoFilter.ApplyRange.Row);
				int endRow = Math.Max(worksheet.MaxContentRow, this.autoFilter.ApplyRange.EndRow);
				endRow = Math.Min(endRow, worksheet.RowCount - 1);

				for (int r = startRow; r <= endRow; r++)
				{
					var cell = worksheet.Cells[r, col];
					if (cell != null && !cell.IsValidCell) continue;

					string str = cell == null ? string.Empty : cell.DisplayText;
					if (string.IsNullOrEmpty(str) && cell?.Data != null)
						str = Convert.ToString(cell.Data);
					if (string.IsNullOrEmpty(str))
					{
						// 仅当同行其它列有内容时才加入「(空白)」，避免尾部空行
						bool hasOther = false;
						int c0 = this.autoFilter.ApplyRange.Col;
						int c1 = this.autoFilter.ApplyRange.EndCol;
						for (int oc = c0; oc <= c1 && !hasOther; oc++)
						{
							if (oc == col) continue;
							var ocCell = worksheet.Cells[r, oc];
							if (ocCell == null || !ocCell.IsValidCell) continue;
							if (!string.IsNullOrEmpty(ocCell.DisplayText)
							    || (ocCell.Data != null && !string.IsNullOrEmpty(Convert.ToString(ocCell.Data))))
								hasOther = true;
						}
						if (!hasOther) continue;
						str = LanguageResource.Filter_Blanks;
					}

					if (!items.Contains(str))
						items.Add(str);
				}

				items.Sort();
				return items;
			}

			internal bool DataDirty { get; set; }

			/// <summary>
			/// Invoked when spreadsheet data changed on this column
			/// </summary>
			/// <param name="startRow">zero-based number of first row that data has been changed</param>
			/// <param name="endRow">zero-based number of last row that data has been changed</param>
			public void OnDataChange(int startRow, int endRow)
			{
				this.DataDirty = true;
			}
		}
		#endregion // AutoColumnFilterBody

		/// <summary>
		/// Event raised when column filter button has been clicked
		/// </summary>
		public event EventHandler FilterButtonPressed;

		internal bool RaiseFilterButtonPress(AutoColumnFilterBody headerBody, RGPoint point)
		{
			if (headerBody.ColumnHeader == null) return false;

			if (FilterButtonPressed != null)
			{
				var arg = new FilterButtonPressedEventArgs(headerBody.ColumnHeader);

				FilterButtonPressed(this, arg);

				if (arg.IsCancelled) return true;
			}

			if (this.columnFilterUIFlag == AutoColumnFilterUI.DropdownButtonAndPanel)
			{
#if WINFORM
				unvell.ReoGrid.WinForm.ColumnFilterContextMenu.ShowFilterPanel(headerBody, (System.Drawing.Point)point);
#elif WPF
        unvell.ReoGrid.WPF.ColumnFilterContextMenu.ShowFilterPanel(headerBody, point);
				var ctx = new System.Windows.Controls.ContextMenu();
				ctx.Items.Add(new System.Windows.Controls.MenuItem() { Header = "Item" });
				ctx.IsOpen = true;
#elif AVALONIA
				unvell.ReoGrid.AvaloniaPlatform.ColumnFilterContextMenu.ShowFilterPanel(headerBody, point);
#endif // WPF
        return true;
			}

			return false;
		}

		private void CreateFilterHeaders(int start, int end)
		{
			try
			{
				this.Worksheet.ControlAdapter.ChangeCursor(CursorStyle.Busy);

				for (int i = start; i <= end; i++)
				{
					var header = this.Worksheet.RetrieveColumnHeader(i);
					header.Body = new AutoColumnFilterBody(this, header);
				}
			}
			finally
			{
				this.Worksheet.ControlAdapter.ChangeCursor(CursorStyle.PlatformDefault);
			}
		}

		private void RemoveFilterHeader(int start, int end)
		{
			if (this.Worksheet.cols.Count <= 0)
			{
				return;
			}

			for (int i = start; i <= end; i++)
			{
				var header = this.Worksheet.RetrieveColumnHeader(i);
				var body = header.Body as AutoColumnFilterBody;

				if (body != null)
				{
#if WINFORM
					if (body.ContextMenuStrip != null)
					{
						body.ContextMenuStrip.Dispose();
					}
#elif WPF
						// todo
#elif AVALONIA

#endif

					header.Body = null;
				}
			}
		}

		/// <summary>
		/// Attach filter to specified worksheet
		/// </summary>
		/// <param name="worksheet">instance of worksheet to be attached</param>
		/// <param name="uiFlag">Flags to decide which styles of GUI to be dispalyed (default is DropdownButtonAndPanel style)</param>
		public void Attach(Worksheet worksheet, AutoColumnFilterUI uiFlag = AutoColumnFilterUI.DropdownButtonAndPanel)
		{
			if (worksheet == null)
			{
				throw new ArgumentNullException("cannot attach to null worksheet", "worksheet");
			}

			this.Worksheet = worksheet;
			this.columnFilterUIFlag = uiFlag;

			CreateFilterHeaders(this.ApplyRange.Col, this.ApplyRange.EndCol);

			worksheet.ColumnsInserted += worksheet_ColumnsInserted;
		}

		/// <summary>
		/// Detach filter from specified worksheet
		/// </summary>
		public void Detach()
		{
			if (this.Worksheet != null)
			{
				this.Worksheet.ColumnsInserted -= worksheet_ColumnsInserted;

				this.Worksheet.ShowRows(this.ApplyRange.Row, this.ApplyRange.Rows);

				RemoveFilterHeader(this.ApplyRange.Col, this.ApplyRange.EndCol);

				this.Worksheet.RequestInvalidate();
				this.Worksheet = null;
			}
		}

		void worksheet_ColumnsInserted(object sender, Events.ColumnsInsertedEventArgs e)
		{
			if (e.Index < this.ApplyRange.Col)
			{
				//RemoveFilterHeader(this.startCol, e.Index - 1);
				this.ApplyRange.Offset(0, e.Count);
			}
			else if (e.Index > this.ApplyRange.Col && e.Index <= this.ApplyRange.EndCol)
			{
				this.CreateFilterHeaders(e.Index, e.Index + e.Count - 1);
				this.ApplyRange.SetCols(this.ApplyRange.Cols + e.Count);
			}
		}
	}
	
	#endregion // Auto Filter

	/// <summary>
	/// Flag to create UI of column filter
	/// </summary>
	public enum AutoColumnFilterUI
	{
		/// <summary>
		/// Do not create any GUI 
		/// </summary>
		NoGUI,

		/// <summary>
		/// Only create a dropdown button on header
		/// </summary>
		DropdownButton,

		/// <summary>
		/// Create both dropdown button and built-in panel
		/// </summary>
		DropdownButtonAndPanel,
	}

	/// <summary>
	/// Event raised when auto filter button was pressed by user
	/// </summary>
	public class FilterButtonPressedEventArgs : EventArgs
	{
		/// <summary>
		/// Get the instance of column header
		/// </summary>
		public ColumnHeader ColumnHeader { get; private set; }

		/// <summary>
		/// Set this flag to prevent open the built-in popup menu
		/// </summary>
		public bool IsCancelled { get; set; }

		/// <summary>
		/// Create filter button pressed event arguments with instance of column header
		/// </summary>
		/// <param name="columnHeader"></param>
		public FilterButtonPressedEventArgs(ColumnHeader columnHeader)
		{
			this.ColumnHeader = columnHeader;
		}
	}
}
