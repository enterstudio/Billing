using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Timers;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using ShomreiTorah.Common;
using ShomreiTorah.Common.Updates;
using ShomreiTorah.WinForms.Forms;
using DevExpress.LookAndFeel;

namespace ShomreiTorah.Billing {
	static class Updater {
		public static readonly UpdateChecker Checker = new UpdateChecker();
		static readonly System.Timers.Timer timer = new System.Timers.Timer(TimeSpan.FromMinutes(30).TotalMilliseconds) { AutoReset = false };

		public static void RunBackground() {
			timer.Elapsed += timer_Elapsed;
			timer.Start();
		}

		static void timer_Elapsed(object sender, ElapsedEventArgs e) {
			RunCheck();
			timer.Start();
		}

		public static void RunCheck() {
			UpdateInfo update = null;
			if (Program.UIInvoker != null && Program.UIInvoker.InvokeRequired) {
				update = Checker.FindUpdate();
			} else {	//If we're running on the UI thread
				ProgressWorker.Execute(ui => {
					ui.Caption = "Searching for updates...";
					update = Checker.FindUpdate();
				}, false);
			}
			if (update != null)
				UIInvoke(delegate { ApplyUpdate(update); });
		}
		///<summary>Called on the UI thread to download and apply an update.</summary>
		///<returns>True if the update was downloaded.</returns>
		public static bool ApplyUpdate(UpdateInfo update) {
			UserLookAndFeel.Default.SkinName = "Lilian";	//This must be set here in case we're on the splash thread at launch time.
			if (DialogResult.No == XtraMessageBox.Show("An update is available.  Do you want to install it?\r\n\r\n" + update.Description,
													   "Shomrei Torah Billing", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
				return false;

			string updatePath;
			try {
				if (!ProgressWorker.Execute(ui => {
					ui.Caption = "Downloading update...";
					updatePath = update.ExtractFiles(ui);
					ui.Caption = "Applying update";
				}, true))
					return false;
			} catch (TargetInvocationException tex) {
				Exception ex = tex;
				while (ex.InnerException != null) ex = ex.InnerException;
				if (!Debugger.IsAttached)
					Email.Warn("Billing Update Error", "New Version: " + update.NewVersion + "\r\nDescription: \r\n" + update.Description + "\r\n\r\n" + ex);

				XtraMessageBox.Show("An error occurred while downloading the update.\r\n" + ex,
									"Shomrei Torah Billing", MessageBoxButtons.OK, MessageBoxIcon.Error);

				return false;
			}
			//TODO: UpdateApplier
			return true;
		}

		static void UIInvoke(Action method) {
			if (Program.UIInvoker != null && Program.UIInvoker.InvokeRequired)
				Program.UIInvoker.Invoke(method, null);
			else
				method();
		}
	}
}
