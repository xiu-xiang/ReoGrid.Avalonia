/*****************************************************************************
 * 
 * ReoGrid - Opensource .NET Spreadsheet Control
 * 
 * https://reogrid.net/
 *
 * THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
 * KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
 * PURPOSE.
 *
 * Thank you to all contributors!
 * 
 * (c) 2012-2025 Jingwood, UNVELL Inc. <jingwood at unvell.com>
 * 
 ****************************************************************************/

using System.Diagnostics;

namespace unvell.ReoGrid.Actions
{
	/// <summary>
	/// Action to set partial grid.
	/// </summary>
	public class SetPartialGridAction : WorksheetReusableAction
	{
		private PartialGrid data;
		private PartialGrid backupData;
		// 选择性粘贴标志（全部 / 仅数值 / 仅格式等）
		private readonly PartialGridCopyFlag flag;

		/// <summary>
		/// Create action to set partial grid.
		/// </summary>
		/// <param name="range">target range to set partial grid.</param>
		/// <param name="data">partial grid to be set.</param>
		public SetPartialGridAction(RangePosition range, PartialGrid data)
			: this(range, data, PartialGridCopyFlag.All)
		{
		}

		/// <summary>
		/// Create action to set partial grid with copy flags (paste special).
		/// </summary>
		/// <param name="range">target range to set partial grid.</param>
		/// <param name="data">partial grid to be set.</param>
		/// <param name="flag">which parts to paste.</param>
		public SetPartialGridAction(RangePosition range, PartialGrid data, PartialGridCopyFlag flag)
			: base(range)
		{
			this.data = data;
			this.flag = flag;
		}

		public override WorksheetReusableAction Clone(RangePosition range)
		{
			return new SetPartialGridAction(range, data, flag);
		}

		/// <summary>
		/// Do action to set partial grid.
		/// </summary>
		public override void Do()
		{
			backupData = Worksheet.GetPartialGrid(base.Range, flag, ExPartialGridCopyFlag.BorderOutsideOwner);
			Debug.Assert(backupData != null);
			base.Range = base.Worksheet.SetPartialGridRepeatly(base.Range, data, flag);
			Worksheet.SelectRange(base.Range);
		}

		/// <summary>
		/// Undo action to restore setting partial grid.
		/// </summary>
		public override void Undo()
		{
			Debug.Assert(backupData != null);
			base.Worksheet.SetPartialGrid(base.Range, backupData, flag, ExPartialGridCopyFlag.BorderOutsideOwner);
		}

		/// <summary>
		/// Get friendly name of this action.
		/// </summary>
		/// <returns>Friendly name of this action.</returns>
		public override string GetName()
		{
			return "Set Partial Grid";
		}
	}
}
