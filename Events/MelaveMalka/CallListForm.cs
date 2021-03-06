using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using DevExpress.Data.Filtering;
using DevExpress.XtraBars;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using ShomreiTorah.Data;
using ShomreiTorah.Data.UI.DisplaySettings;
using ShomreiTorah.Singularity;
using ShomreiTorah.WinForms;

namespace ShomreiTorah.Billing.Events.MelaveMalka {
	partial class CallListForm : XtraForm {
		readonly int year;

		readonly FilteredTable<MelaveMalkaInvitation> dataSource;
		public CallListForm(int year) {
			Program.LoadTable<MelaveMalkaInvitation>();	//Includes Callers as a dependency
			InitializeComponent();

			this.year = year;
			Text = "Melave Malka " + year + " Call List";

			grid.DataMember = null;
			listSearch.Properties.DataSource = grid.DataSource = dataSource
				= Program.Table<MelaveMalkaInvitation>().Filter(mmi => mmi.Year == year);

			EditorRepository.PersonOwnedLookup.Apply(listSearch.Properties);

			gridView.ActiveFilterCriteria = new OperandProperty("AdAmount") == 0;
			new AdvancedColumnsBehavior("ad amounts", fieldNames: new[] { "AdAmount" }).Apply(gridView);
			CheckableGridController.Handle(colShouldCall);

			Program.Table<Caller>().RowAdded += CallersTable_Changed;
			Program.Table<Caller>().RowRemoved += CallersTable_Changed;
			BindCallerDropDown();
		}
		private void showCallers_ItemClick(object sender, ItemClickEventArgs e) { new CallerList(year).Show(MdiParent); }

		///<summary>Releases the unmanaged resources used by the CallListForm and optionally releases the managed resources.</summary>
		///<param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		protected override void Dispose(bool disposing) {
			if (disposing) {
				Program.Table<Caller>().RowAdded -= CallersTable_Changed;
				Program.Table<Caller>().RowRemoved -= CallersTable_Changed;
				dataSource.Dispose();
				if (components != null) components.Dispose();
			}
			base.Dispose(disposing);
		}
		void CallersTable_Changed(object sender, RowListEventArgs<Caller> e) { BindCallerDropDown(); }
		void BindCallerDropDown() {
			callerEdit.BeginUpdate();
			callerEdit.Items.Clear();
			foreach (var caller in Program.Table<Caller>().Rows.Where(c => c.Year == year))
				callerEdit.Items.Add(caller);
			callerEdit.EndUpdate();
		}

		private void gridView_InvalidValueException(object sender, InvalidValueExceptionEventArgs e) {
			//Deleting the text in the combobox will
			//try to set the cell value to an empty
			//string, and fail with an invalid cast.
			//I handle this case and actually set it
			//to null.
			if (gridView.FocusedColumn == colCaller) {
				var str = e.Value as string;	//If the value is a non-null but empty string
				if (str != null && str.Trim().Length == 0) {
					e.ExceptionMode = ExceptionMode.NoAction;
					gridView.SetFocusedRowCellValue(colCaller, null);
				}
			}
		}

		private void listSearch_EditValueChanged(object sender, EventArgs e) {
			var row = listSearch.EditValue as MelaveMalkaInvitation;
			if (row == null) return;
			var handle = gridView.GetRowHandle(dataSource.Rows.IndexOf(row));
			if (handle < 0)
				Dialog.ShowError(row.Person.FullName + " do not meet the current filter");
			else
				gridView.SetSelection(handle, makeVisible: true);
			listSearch.EditValue = null;
		}

		private void autoAssign_ItemClick(object sender, ItemClickEventArgs e) {
			var callers = Program.Table<Caller>().Rows.Where(c => c.Year == year).OrderBy(c => c.Person.LastName).ToList();
			if (callers.Count == 0) {
				new CallerList(year).Show(MdiParent);
				Dialog.ShowError("Please select some callers");
				return;
			}

			var newCallees = dataSource.Rows
				.Where(c => c.ShouldCall && c.Caller == null && c.AdAmount == 0)
				.OrderBy(c => c.Person.LastName)
				.ToList();
			if (newCallees.Count < 2) {
				Dialog.ShowError("This feature is to be used when there are multiple unassigned callers.\r\nMake sure that there are people who are checked, but do not have a caller.");
				return;
			}

			if (callers.Count == 1) {
				if (!Dialog.Warn("Are you sure you want to assign " + newCallees.Count + " people to a single caller?"))
					return;
				foreach (var callee in newCallees)
					callee.Caller = callers[0];
			} else {		//Multiple callers
				if (!Dialog.Warn("Are you sure you want to assign " + newCallees.Count + " people to " + callers.Count + " callers?"))
					return;

				//First, assign each caller to call himself, if he needs to be called
				foreach (var selfCaller in callers.Join(newCallees, c => c.Person, c => c.Person, (Caller, Callee) => new { Caller, Callee }).ToList()) {
					selfCaller.Callee.Caller = selfCaller.Caller;
					newCallees.Remove(selfCaller.Callee);
				}

				int callerIndex = 0;
				foreach (var callee in newCallees) {
					callee.Caller = callers[callerIndex];
					callerIndex = (callerIndex + 1) % callers.Count;
				}
			}
		}

		private void exportGlobalList_ItemClick(object sender, ItemClickEventArgs e) {
			string path;
			using (var dialog = new SaveFileDialog {
				Filter = "Excel 2003 Spreadsheet|*.xls|Excel 2007 Spreadsheet (*.xlsx)|*.xlsx",
				FileName = "Melave Malka " + year + " Call List.xls",
				Title = "Save Call List"
			}) {
				if (dialog.ShowDialog(MdiParent) != DialogResult.OK) return;
				path = dialog.FileName;
			}
			ExcelExporter.CreateGlobalCallList(year, path);
			Process.Start(path);
		}

		private void clearCallers_ItemClick(object sender, ItemClickEventArgs e) {
			var callees = gridView.GetSelectedRows()
								  .Select(gridView.GetRow)
								  .Cast<MelaveMalkaInvitation>()
								  .Where(i => i.Caller != null)
								  .ToList();
			if (callees.Count == 0)
				return;
			if (callees.Count > 1) {
				if (!Dialog.Confirm("Would you like to clear the caller field for " + callees.Count + " people?"))
					return;
			} else {
				if (!Dialog.Confirm("Would you like to clear the caller field for " + callees[0].Person.FullName + "?"))
					return;
			}
			foreach (var person in callees)
				person.Caller = null;
		}
	}
}