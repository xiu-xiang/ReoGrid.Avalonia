/*****************************************************************************
 * 
 * ReoGrid - .NET Spreadsheet Control
 * 
 * Avalonia 列筛选下拉面板（本地修复版）
 * - Flyout 保证可点击
 * - 值筛选：列出当前列所有值供勾选（含计数）
 * - 文本筛选：等于/包含等条件
 * 
 ****************************************************************************/

#if AVALONIA

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using unvell.ReoGrid.Graphics;
using unvell.ReoGrid.Interaction;
using Point = unvell.ReoGrid.Graphics.Point;

namespace unvell.ReoGrid.AvaloniaPlatform
{
	/// <summary>值筛选列表项（值 + 出现次数）。</summary>
	internal sealed class FilterValueItem : INotifyPropertyChanged
	{
		private bool _isChecked = true;

		public string Value { get; init; } = string.Empty;
		public int Count { get; init; }
		public string DisplayText => Count > 0 ? $"{Value} ({Count})" : Value;

		public bool IsChecked
		{
			get => _isChecked;
			set
			{
				if (_isChecked == value) return;
				_isChecked = value;
				OnPropertyChanged();
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged([CallerMemberName] string name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	/// <summary>列头筛选下拉面板（Flyout）。</summary>
	public class ColumnFilterContextMenu
	{
		private readonly Border _root;
		private readonly StackPanel _mainPanel;
		private readonly StackPanel _valueFilterPanel;
		private readonly ListBox _valueList;
		private readonly TextBox _searchBox;
		private readonly CheckBox _selectAllCheck;
		private readonly TextBlock _selectAllLabel;
		private readonly Button _sortAzButton;
		private readonly Button _sortZaButton;
		private readonly Button _textFilterButton;
		private readonly Button _valueFilterButton;
		private readonly Button _okButton;
		private readonly Button _cancelButton;
		private readonly Button _valueOkButton;
		private readonly Button _valueCancelButton;
		private readonly Button _sortByNameButton;
		private readonly Button _sortByCountButton;
		private Flyout _flyout;
		private bool _handlersAttached;
		private Control _host;
		private readonly ObservableCollection<FilterValueItem> _allValues = new();
		private readonly ObservableCollection<FilterValueItem> _visibleValues = new();
		private bool _suppressSelectAllSync;
		private enum ValueSortMode { NameAsc, CountDesc }
		private ValueSortMode _valueSortMode = ValueSortMode.NameAsc;

		/// <summary>关联的列筛选表头主体。</summary>
		public Data.AutoColumnFilter.AutoColumnFilterBody HeaderBody { get; set; }

		/// <summary>兼容旧字段名。</summary>
		public Flyout Popup => _flyout;

		public ColumnFilterContextMenu()
		{
			_sortAzButton = CreateMenuButton("从 A 到 Z 排序");
			_sortZaButton = CreateMenuButton("从 Z 到 A 排序");
			_textFilterButton = CreateMenuButton("文本筛选 ▶");
			_valueFilterButton = CreateMenuButton("值筛选 ▶");

			_okButton = new Button { Content = "确定", MinWidth = 72, Margin = new Thickness(0, 0, 8, 0) };
			_cancelButton = new Button { Content = "取消", MinWidth = 72 };

			_mainPanel = new StackPanel
			{
				Spacing = 2,
				Width = 260,
				Children =
				{
					_sortAzButton,
					_sortZaButton,
					new Separator { Margin = new Thickness(0, 4) },
					_textFilterButton,
					_valueFilterButton,
					new StackPanel
					{
						Orientation = Orientation.Horizontal,
						HorizontalAlignment = HorizontalAlignment.Right,
						Margin = new Thickness(0, 10, 0, 0),
						Children = { _okButton, _cancelButton },
					},
				},
			};

			// —— 值筛选子页：搜索 + 全选/反选 + 带计数勾选列表 ——
			_searchBox = new TextBox
			{
				PlaceholderText = "同时含所有关键字，空格分隔",
				Margin = new Thickness(0, 0, 0, 4),
			};
			_sortByNameButton = new Button { Content = "名称 ↑", MinWidth = 56, Margin = new Thickness(0, 0, 4, 0), Padding = new Thickness(6, 2) };
			_sortByCountButton = new Button { Content = "计数 ↓", MinWidth = 56, Padding = new Thickness(6, 2) };

			_selectAllCheck = new CheckBox { IsChecked = true, VerticalAlignment = VerticalAlignment.Center };
			_selectAllLabel = new TextBlock { Text = "全选 (0)", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 12, 0) };
			var invertLink = CreateLinkButton("反选");
			var dupLink = CreateLinkButton("重复项");
			var uniqueLink = CreateLinkButton("唯一项");

			_valueList = new ListBox
			{
				MinHeight = 180,
				MaxHeight = 280,
				Width = 280,
				ItemsSource = _visibleValues,
				ItemTemplate = new FuncDataTemplate<FilterValueItem>((item, _) =>
				{
					if (item == null) return new TextBlock();
					var cb = new CheckBox
					{
						Margin = new Thickness(2, 1),
						VerticalAlignment = VerticalAlignment.Center,
					};
					cb.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(FilterValueItem.IsChecked))
					{
						Mode = BindingMode.TwoWay,
						Source = item,
					});
					cb.Bind(ContentControl.ContentProperty, new Binding(nameof(FilterValueItem.DisplayText))
					{
						Source = item,
					});
					return cb;
				}, true),
			};

			_valueOkButton = new Button { Content = "确定", MinWidth = 72, Margin = new Thickness(0, 0, 8, 0) };
			_valueCancelButton = new Button { Content = "取消", MinWidth = 72 };

			_valueFilterPanel = new StackPanel
			{
				Spacing = 4,
				Width = 300,
				IsVisible = false,
				Children =
				{
					new TextBlock { Text = "值筛选", FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 0, 0, 4) },
					new Grid
					{
						ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
						Children =
						{
							_searchBox,
							Named(_sortByNameButton, 1),
							Named(_sortByCountButton, 2),
						},
					},
					new StackPanel
					{
						Orientation = Orientation.Horizontal,
						Spacing = 2,
						Children =
						{
							_selectAllCheck,
							_selectAllLabel,
							invertLink,
							dupLink,
							uniqueLink,
						},
					},
					_valueList,
					new StackPanel
					{
						Orientation = Orientation.Horizontal,
						HorizontalAlignment = HorizontalAlignment.Right,
						Margin = new Thickness(0, 6, 0, 0),
						Children = { _valueOkButton, _valueCancelButton },
					},
				},
			};

			// 快捷链接事件
			invertLink.Click += (_, _) => InvertVisibleChecks();
			dupLink.Click += (_, _) => CheckByCount(c => c > 1);
			uniqueLink.Click += (_, _) => CheckByCount(c => c == 1);

			_root = new Border
			{
				Background = Brushes.White,
				BorderBrush = new SolidColorBrush(Color.FromRgb(0xAB, 0xAB, 0xAB)),
				BorderThickness = new Thickness(1),
				Padding = new Thickness(10),
				IsHitTestVisible = true,
				Child = new Panel
				{
					Children = { _mainPanel, _valueFilterPanel },
				},
			};

			_flyout = new Flyout
			{
				Content = _root,
				Placement = PlacementMode.BottomEdgeAlignedLeft,
				ShowMode = FlyoutShowMode.Transient,
			};
		}

		private static Control Named(Control c, int col)
		{
			Grid.SetColumn(c, col);
			return c;
		}

		private static Button CreateMenuButton(string text) => new Button
		{
			Content = text,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Left,
			Background = Brushes.Transparent,
			BorderThickness = new Thickness(0),
			Padding = new Thickness(6, 4),
			Margin = new Thickness(0, 0, 0, 1),
			Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
		};

		private static Button CreateLinkButton(string text) => new Button
		{
			Content = text,
			Background = Brushes.Transparent,
			BorderThickness = new Thickness(0),
			Padding = new Thickness(4, 2),
			Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x73, 0xE8)),
			Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
			VerticalAlignment = VerticalAlignment.Center,
		};

		private void EnsureHandlers()
		{
			if (_handlersAttached) return;
			_handlersAttached = true;

			_sortAzButton.Click += (_, _) => SortColumn(SortOrder.Ascending);
			_sortZaButton.Click += (_, _) => SortColumn(SortOrder.Descending);
			_textFilterButton.Click += async (_, _) => await ShowTextFilterMenuAsync();
			_valueFilterButton.Click += (_, _) => ShowValueFilterPage();
			_okButton.Click += (_, _) => Close();
			_cancelButton.Click += (_, _) => Close();

			_searchBox.TextChanged += (_, _) => ApplySearchFilter();
			_sortByNameButton.Click += (_, _) =>
			{
				_valueSortMode = ValueSortMode.NameAsc;
				ApplySearchFilter();
			};
			_sortByCountButton.Click += (_, _) =>
			{
				_valueSortMode = ValueSortMode.CountDesc;
				ApplySearchFilter();
			};
			_selectAllCheck.IsCheckedChanged += (_, _) =>
			{
				if (_suppressSelectAllSync) return;
				bool on = _selectAllCheck.IsChecked == true;
				foreach (var item in _visibleValues)
					item.IsChecked = on;
			};
			_valueOkButton.Click += (_, _) => ApplyValueFilterAndClose();
			_valueCancelButton.Click += (_, _) =>
			{
				ShowMainPage();
			};
		}

		private void Close()
		{
			ShowMainPage();
			try { _flyout?.Hide(); } catch { /* ignore */ }
		}

		private void ShowMainPage()
		{
			_mainPanel.IsVisible = true;
			_valueFilterPanel.IsVisible = false;
		}

		/// <summary>打开值筛选页：列出当前列所有值供勾选。</summary>
		private void ShowValueFilterPage()
		{
			LoadColumnValues();
			_searchBox.Text = string.Empty;
			_valueSortMode = ValueSortMode.NameAsc;
			ApplySearchFilter();
			_mainPanel.IsVisible = false;
			_valueFilterPanel.IsVisible = true;
		}

		/// <summary>扫描当前列，统计每个唯一值出现次数。</summary>
		private void LoadColumnValues()
		{
			_allValues.Clear();
			if (HeaderBody?.ColumnHeader?.Worksheet == null) return;

			var worksheet = HeaderBody.ColumnHeader.Worksheet;
			int col = HeaderBody.ColumnHeader.Index;
			if (col < 0 || col >= worksheet.ColumnCount) return;

			// 优先用筛选作用区；无效时回退整列已用行，避免列表为空
			var range = HeaderBody.autoFilter?.ApplyRange ?? RangePosition.Empty;
			int startRow = 0;
			int endRow = worksheet.MaxContentRow;
			if (!range.IsEmpty && range.Rows > 0)
			{
				startRow = Math.Max(0, range.Row);
				endRow = Math.Max(endRow, range.EndRow);
			}
			if (endRow < startRow) endRow = startRow;
			endRow = Math.Min(endRow, worksheet.RowCount - 1);

			var counts = new Dictionary<string, int>(StringComparer.CurrentCulture);
			int blankCount = 0;
			string blankLabel = LanguageResource.Filter_Blanks;

			for (int r = startRow; r <= endRow; r++)
			{
				var cell = worksheet.Cells[r, col];
				// 跳过被合并覆盖的无效格，避免重复计入
				if (cell != null && !cell.IsValidCell) continue;

				string str = GetCellFilterText(cell);
				// 空白：仅当本行在筛选区内其它列有内容时才计入（排除尾部空行误报）
				if (str == blankLabel)
				{
					if (RowHasOtherContent(worksheet, range, r, col))
						blankCount++;
					continue;
				}

				if (counts.TryGetValue(str, out int n))
					counts[str] = n + 1;
				else
					counts[str] = 1;
			}

			if (blankCount > 0)
				counts[blankLabel] = blankCount;

			bool selectAll = HeaderBody.IsSelectAll != false && HeaderBody.selectedTextItems.Count == 0;
			var selected = new HashSet<string>(HeaderBody.selectedTextItems ?? Enumerable.Empty<string>(),
				StringComparer.CurrentCulture);

			// 空白项排在最前，其余按名称
			IEnumerable<KeyValuePair<string, int>> ordered = counts
				.OrderBy(x => x.Key == blankLabel ? 0 : 1)
				.ThenBy(x => x.Key, StringComparer.CurrentCulture);

			foreach (var kv in ordered)
			{
				bool check = selectAll || HeaderBody.IsSelectAll == true;
				if (HeaderBody.IsSelectAll == false && selected.Count > 0)
					check = selected.Contains(kv.Key);
				else if (!selectAll && selected.Count > 0)
					check = selected.Contains(kv.Key);

				var entry = new FilterValueItem
				{
					Value = kv.Key,
					Count = kv.Value,
					IsChecked = check,
				};
				entry.PropertyChanged += (_, e) =>
				{
					if (e.PropertyName == nameof(FilterValueItem.IsChecked))
						SyncSelectAllCheckState();
				};
				_allValues.Add(entry);
			}
		}

		/// <summary>本行在筛选区内除当前列外是否还有内容（用于判断是否为真实空白格）。</summary>
		private static bool RowHasOtherContent(Worksheet worksheet, RangePosition range, int row, int filterCol)
		{
			int c0 = range.IsEmpty ? 0 : range.Col;
			int c1 = range.IsEmpty ? worksheet.MaxContentCol : range.EndCol;
			c1 = Math.Min(c1, worksheet.ColumnCount - 1);

			for (int c = c0; c <= c1; c++)
			{
				if (c == filterCol) continue;
				var cell = worksheet.Cells[row, c];
				if (cell == null || !cell.IsValidCell) continue;
				if (!string.IsNullOrEmpty(cell.DisplayText)) return true;
				if (cell.Data != null && !string.IsNullOrEmpty(Convert.ToString(cell.Data))) return true;
			}

			return false;
		}

		/// <summary>取筛选用显示文本（优先 DisplayText，其次 Data）；空值用中文「(空白)」。</summary>
		private static string GetCellFilterText(Cell cell)
		{
			if (cell == null) return LanguageResource.Filter_Blanks;

			string str = cell.DisplayText;
			if (string.IsNullOrEmpty(str) && cell.Data != null)
				str = Convert.ToString(cell.Data);

			if (string.IsNullOrEmpty(str))
				return LanguageResource.Filter_Blanks;

			return str;
		}

		private void ApplySearchFilter()
		{
			string raw = _searchBox.Text?.Trim() ?? string.Empty;
			string[] keys = string.IsNullOrEmpty(raw)
				? Array.Empty<string>()
				: raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			IEnumerable<FilterValueItem> q = _allValues;
			if (keys.Length > 0)
			{
				q = q.Where(item =>
				{
					string v = item.Value ?? string.Empty;
					return keys.All(k => v.IndexOf(k, StringComparison.CurrentCultureIgnoreCase) >= 0);
				});
			}

			q = _valueSortMode == ValueSortMode.CountDesc
				? q.OrderByDescending(x => x.Count).ThenBy(x => x.Value, StringComparer.CurrentCulture)
				: q.OrderBy(x => x.Value, StringComparer.CurrentCulture);

			_visibleValues.Clear();
			foreach (var item in q)
				_visibleValues.Add(item);

			SyncSelectAllCheckState();
			_selectAllLabel.Text = $"全选 ({_allValues.Count})";
		}

		private void SyncSelectAllCheckState()
		{
			_suppressSelectAllSync = true;
			try
			{
				if (_visibleValues.Count == 0)
				{
					_selectAllCheck.IsChecked = false;
					return;
				}

				int checkedCount = _visibleValues.Count(x => x.IsChecked);
				if (checkedCount == 0)
					_selectAllCheck.IsChecked = false;
				else if (checkedCount == _visibleValues.Count)
					_selectAllCheck.IsChecked = true;
				else
					_selectAllCheck.IsChecked = null; // 部分选中
			}
			finally
			{
				_suppressSelectAllSync = false;
			}
		}

		private void InvertVisibleChecks()
		{
			foreach (var item in _visibleValues)
				item.IsChecked = !item.IsChecked;
			SyncSelectAllCheckState();
		}

		private void CheckByCount(Func<int, bool> countPred)
		{
			foreach (var item in _allValues)
				item.IsChecked = countPred(item.Count);
			ApplySearchFilter();
		}

		private void ApplyValueFilterAndClose()
		{
			if (HeaderBody == null) return;

			var checkedItems = _allValues.Where(x => x.IsChecked).Select(x => x.Value).ToList();
			bool isSelectAll = checkedItems.Count == _allValues.Count && _allValues.Count > 0;

			HeaderBody.IsSelectAll = isSelectAll;
			HeaderBody.selectedTextItems.Clear();
			HeaderBody.ContainsBlank = false;

			if (!isSelectAll)
			{
				foreach (var v in checkedItems)
				{
					HeaderBody.SelectedTextItems.Add(v);
					if (v == LanguageResource.Filter_Blanks || string.IsNullOrEmpty(v))
						HeaderBody.ContainsBlank = true;
				}
			}
			else
			{
				HeaderBody.ContainsBlank = true;
			}

			try
			{
				HeaderBody.autoFilter.Apply();
			}
			catch (Exception ex)
			{
				HeaderBody.ColumnHeader?.Worksheet?.NotifyExceptionHappen(ex);
			}

			Close();
		}

		private void SortColumn(SortOrder order)
		{
			if (HeaderBody?.ColumnHeader?.Worksheet == null) return;
			var worksheet = HeaderBody.ColumnHeader.Worksheet;
			try
			{
				worksheet.SortColumn(HeaderBody.ColumnHeader.Index, HeaderBody.autoFilter.ApplyRange, order);
				Close();
			}
			catch (Exception ex)
			{
				string tip = ex.Message ?? string.Empty;
				if (tip.Contains("same colspan", StringComparison.OrdinalIgnoreCase)
				    || tip.Contains("Cannot change a part of range", StringComparison.OrdinalIgnoreCase))
				{
					tip = "排序区域存在合并单元格，无法直接排序。请先取消相关合并后再试。";
				}
				worksheet.NotifyExceptionHappen(new InvalidOperationException(tip, ex));
			}
		}

		private async Task ShowTextFilterMenuAsync()
		{
			if (_host == null) return;
			var menu = new MenuFlyout();
			void Add(string header, Func<string, string, bool> pred)
			{
				var item = new MenuItem { Header = header };
				item.Click += async (_, _) =>
				{
					string kw = await PromptAsync(_host, header, "请输入文本：");
					if (kw == null) return;
					ApplyTextPredicate(s => pred(s ?? string.Empty, kw));
				};
				menu.Items.Add(item);
			}

			Add("等于...", (s, k) => string.Equals(s, k, StringComparison.CurrentCultureIgnoreCase));
			Add("不等于...", (s, k) => !string.Equals(s, k, StringComparison.CurrentCultureIgnoreCase));
			Add("开头是...", (s, k) => s.StartsWith(k, StringComparison.CurrentCultureIgnoreCase));
			Add("结尾是...", (s, k) => s.EndsWith(k, StringComparison.CurrentCultureIgnoreCase));
			Add("包含...", (s, k) => s.IndexOf(k, StringComparison.CurrentCultureIgnoreCase) >= 0);
			Add("不包含...", (s, k) => s.IndexOf(k, StringComparison.CurrentCultureIgnoreCase) < 0);

			menu.ShowAt(_textFilterButton, true);
			await Task.CompletedTask;
		}

		private void ApplyTextPredicate(Func<string, bool> predicate)
		{
			if (HeaderBody == null) return;

			var items = HeaderBody.GetDistinctItems() ?? new List<string>();
			HeaderBody.IsSelectAll = false;
			HeaderBody.selectedTextItems.Clear();
			HeaderBody.ContainsBlank = false;

			foreach (var item in items)
			{
				bool isBlank = string.IsNullOrEmpty(item) || item == LanguageResource.Filter_Blanks;
				string test = isBlank ? string.Empty : item;
				if (!predicate(test)) continue;
				HeaderBody.selectedTextItems.Add(item);
				if (isBlank) HeaderBody.ContainsBlank = true;
			}

			try { HeaderBody.autoFilter.Apply(); }
			catch (Exception ex) { HeaderBody.ColumnHeader?.Worksheet?.NotifyExceptionHappen(ex); }

			Close();
		}

		private static async Task<string> PromptAsync(Control host, string title, string label)
		{
			var top = TopLevel.GetTopLevel(host) as Window;
			var box = new TextBox { MinWidth = 220, Margin = new Thickness(0, 8, 0, 12) };
			var ok = new Button { Content = "确定", MinWidth = 72, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
			var cancel = new Button { Content = "取消", MinWidth = 72, IsCancel = true };
			string result = null;
			bool confirmed = false;

			var dialog = new Window
			{
				Title = title,
				Width = 320,
				SizeToContent = SizeToContent.Height,
				WindowStartupLocation = WindowStartupLocation.CenterOwner,
				CanResize = false,
				Content = new Border
				{
					Padding = new Thickness(16),
					Child = new StackPanel
					{
						Children =
						{
							new TextBlock { Text = label },
							box,
							new StackPanel
							{
								Orientation = Orientation.Horizontal,
								HorizontalAlignment = HorizontalAlignment.Right,
								Children = { ok, cancel },
							},
						},
					},
				},
			};

			ok.Click += (_, _) => { confirmed = true; result = box.Text ?? string.Empty; dialog.Close(); };
			cancel.Click += (_, _) => { confirmed = false; result = null; dialog.Close(); };

			if (top != null)
				await dialog.ShowDialog(top);
			else
				dialog.Show();

			return confirmed ? result : null;
		}

		/// <summary>在列头筛选按钮处弹出面板。</summary>
		internal static void ShowFilterPanel(Data.AutoColumnFilter.AutoColumnFilterBody headerBody, Point point)
		{
			if (headerBody?.ColumnHeader?.Worksheet == null) return;

			var worksheet = headerBody.ColumnHeader.Worksheet;
			Control host = worksheet.ControlAdapter?.ControlInstance as Control;
			if (host == null) return;

			if (headerBody.ContextMenu == null)
				headerBody.ContextMenu = new ColumnFilterContextMenu();

			var filterPanel = headerBody.ContextMenu;
			filterPanel.HeaderBody = headerBody;
			filterPanel._host = host;
			filterPanel.EnsureHandlers();
			filterPanel.ShowMainPage();
			headerBody.DataDirty = false;

			Dispatcher.UIThread.Post(() =>
			{
				try
				{
					filterPanel._flyout.ShowAt(host, true);
				}
				catch (Exception ex)
				{
					worksheet.NotifyExceptionHappen(ex);
				}
			}, DispatcherPriority.Input);
		}
	}
}
#endif
