using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using ShomreiTorah.Common;
using ShomreiTorah.Common.Calendar;

namespace ShomreiTorah.Billing.Events.Auctions {
	partial class AuctionGroupSelector : XtraUserControl {
		public AuctionGroupSelector() {
			InitializeComponent();

			var today = HebrewDate.Today;
			SetSelection(today.HebrewYear, AuctionInfo.Groups.Last(g => g.Auctions[0].Date.GetDate(today.HebrewYear) <= today));
		}

		[Browsable(false)]
		public AuctionGroup SelectedGroup { get; private set; }
		[Browsable(false)]
		public int SelectedYear { get; private set; }

		private void buttonEdit_ButtonClick(object sender, ButtonPressedEventArgs e) {
			MoveBy(e.Button.IsLeft ? -1 : +1);
		}
		private void buttonEdit_Properties_Spin(object sender, SpinEventArgs e) {
			MoveBy(e.IsSpinUp ? +1 : -1);
			e.Handled = true;
		}
		public void MoveBy(int direction) {
			if (direction < 0 && SelectedGroup == AuctionInfo.Groups.First())
				SetSelection(SelectedYear - 1, AuctionInfo.Groups.Last());
			else if (direction > 0 && SelectedGroup == AuctionInfo.Groups.Last())
				SetSelection(SelectedYear + 1, AuctionInfo.Groups.First());
			else {
				var groupIndex = AuctionInfo.Groups.IndexOf(SelectedGroup);
				SetSelection(SelectedYear, AuctionInfo.Groups[groupIndex + direction]);
			}
		}

		public void SetSelection(int year, AuctionGroup group) {
			if (group == null) throw new ArgumentNullException("group");
			SelectedYear = year;
			SelectedGroup = group;

			buttonEdit.Text = group.Name + ", " + (year - 5000).ToHebrewString(HebrewNumberFormat.LetterQuoted);
			OnSelectionChanged();
		}

		///<summary>Occurs when the selected AuctionGroup changes.</summary>
		[Description("Occurs when the selected AuctionGroup changes")]
		[Category("Action")]
		public event EventHandler SelectionChanged;
		///<summary>Raises the SelectionChanged event.</summary>
		internal protected virtual void OnSelectionChanged() { OnSelectionChanged(EventArgs.Empty); }
		///<summary>Raises the SelectionChanged event.</summary>
		///<param name="e">An EventArgs object that provides the event data.</param>
		internal protected virtual void OnSelectionChanged(EventArgs e) {
			if (SelectionChanged != null)
				SelectionChanged(this, e);
		}

		private void buttonEdit_KeyDown(object sender, KeyEventArgs e) {
			switch (e.KeyCode) {
				case Keys.Left:
				case Keys.Down:
					MoveBy(-1);
					break;
				case Keys.Right:
				case Keys.Up:
					MoveBy(+1);
					break;
				case Keys.PageDown:
					SetSelection(SelectedYear - 1, SelectedGroup);
					break;
				case Keys.PageUp:
					SetSelection(SelectedYear + 1, SelectedGroup);
					break;

				case Keys.Home:
					SetSelection(SelectedYear, AuctionInfo.Groups.First());
					break;
				case Keys.End:
					SetSelection(SelectedYear, AuctionInfo.Groups.Last());
					break;
			}
		}

	}
}
