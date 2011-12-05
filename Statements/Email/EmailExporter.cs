using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraGrid.Views.Base;
using ShomreiTorah.Billing.Controls;
using ShomreiTorah.Common;
using ShomreiTorah.Data;
using ShomreiTorah.Singularity;
using ShomreiTorah.Singularity.DataBinding;
using ShomreiTorah.Statements.Email;
using ShomreiTorah.WinForms.Forms;

namespace ShomreiTorah.Billing.Statements.Email {
	using Email = ShomreiTorah.Common.Email;//Force the name to not refer to this namespace

	partial class EmailExporter : XtraForm {
		static readonly StatementBuilder PageBuilder = new StatementBuilder(Path.Combine(Program.AppDirectory, @"Email Templates\Statements"), Path.Combine(Program.AppDirectory, @"Email Templates\Images"));

		readonly Person[] people;
		readonly EditorButton sendPreviewButton, showPreviewButton;

		public static void Execute(Form parent, params Person[] people) {
			if (people == null) throw new ArgumentNullException("people");
			var originalPeople = people;
			people = people.Where(r => r.EmailAddresses.Count > 0).Distinct().ToArray();

			if (people.Length == 0) {
				if (originalPeople.Length == 1)
					XtraMessageBox.Show(originalPeople[0].FullName + " do not have any email addresses.",
										"Shomrei Torah Billing", MessageBoxButtons.OK, MessageBoxIcon.Error);
				else
					XtraMessageBox.Show("None of the people you selected have email addresses.",
										"Shomrei Torah Billing", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			Program.Current.RefreshDatabase();

			var fileNames = PageBuilder.Templates.ToArray();
			if (fileNames.Length == 0) {
				XtraMessageBox.Show("There are no email templates.\r\nPlese contact Schabse.",
									"Shomrei Torah Billing", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			new EmailExporter(people, fileNames).Show(parent);
		}

		EmailExporter(Person[] people, string[] fileNames) {
			InitializeComponent();
			this.people = people;

			if (people.Length == 1)
				gridCaption.Text = "The following person will receive emails:";
			else
				gridCaption.Text = "The following " + people.Length.ToString(CultureInfo.CurrentCulture) + " people will receive emails:";

			sendPreviewButton = buttonEdit.Buttons[1];
			showPreviewButton = buttonEdit.Buttons[0];

			startDate.DateTime = new DateTime(DateTime.Today.AddDays(-80).Year, 1, 1);
			startDate.Properties.MaxValue = DateTime.Today;

			grid.DataSource = new RowListBinder(Program.Table<Person>(), (Row[])people);
			gridView.BestFitColumns();

			emailTemplate.Properties.Items.AddRange(fileNames);
			emailTemplate.Properties.DropDownRows = Math.Max(emailTemplate.Properties.Items.Count, 7);
			SetEnabled();
		}

		private void EditValueChanged(object sender, EventArgs e) { SetEnabled(); }
		void SetEnabled() {
			showPreviewButton.Enabled = sendBills.Enabled = emailTemplate.EditValue != null && startDate.EditValue != null;
			sendPreviewButton.Enabled = showPreviewButton.Enabled && previewAddress.Address != null;
		}

		void SendBills() {
			var exceptions = new List<KeyValuePair<Person, SmtpException>>();
			var successes = new List<StatementMessage>(people.Length);
			ProgressWorker.Execute(ui => {
				ui.Maximum = people.Length;
				foreach (var person in people) {
					ui.Caption = "Emailing " + person.VeryFullName;
					ui.Progress++;

					if (ui.WasCanceled) return;

					using (var message = PageBuilder.CreateMessage(person, emailTemplate.Text, startDate.DateTime)) {
						if (message == null) continue;

						message.To.AddRange(person.EmailAddresses.Select(e => e.MailAddress));
						try {
							Email.Hosted.Send(message);
							successes.Add(message);
						} catch (SmtpException ex) { exceptions.Add(new KeyValuePair<Person, SmtpException>(person, ex)); }
					}
				}
			}, true);

			foreach (var info in successes)	//The table can only be modified on the UI thread.
				info.LogStatement();

			if (exceptions.Any())
				XtraMessageBox.Show("The following errors occurred while sending the emails:\r\n\r\n  � "
					+ exceptions.Join("\r\n\r\n  � ", kvp => kvp.Key.FullName + ": " + kvp.Value.Message),
									"Shomrei Torah Billing", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
		private void buttonEdit_ButtonClick(object sender, ButtonPressedEventArgs e) {
			var row = (Person)gridView.GetFocusedRow();
			if (row == null) return;

			if (e.Button.Caption == sendPreviewButton.Caption) {
				using (var message = PageBuilder.CreateMessage(row, emailTemplate.Text, startDate.DateTime)) {
					if (message == null) {
						XtraMessageBox.Show(row.FullName + " do not have any relevant data and will not receive an email.",
											"Shomrei Torah Billing", MessageBoxButtons.OK, MessageBoxIcon.Information);
						return;
					}
					message.To.Add(previewAddress.Address);
					Email.Hosted.Send(message);
				}
			} else {
				var page = (StatementPage)PageBuilder.TemplateService.Resolve(emailTemplate.Text);
				page.ImageService = new LocalFileImageService(PageBuilder.ImagesPath);
				page.SetInfo(row, startDate.DateTime);

				string html = page.RenderPage();

				if (!page.ShouldSend) {
					XtraMessageBox.Show(row.FullName + " do not have any relevant data and will not receive an email.",
										"Shomrei Torah Billing", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}

				string subject = page.EmailSubject;

				var form = CopyableWebBrowser.CreatePreviewForm("Email Preview: " + subject, html);
				form.Icon = this.Icon;
				form.Show(this);
			}
		}

		private void gridView_CustomUnboundColumnData(object sender, CustomColumnDataEventArgs e) {
			if (e.Column == colEmails) {
				var row = (Person)gridView.GetRow(e.RowHandle);
				if (row != null)
					e.Value = row.EmailAddresses.Select(m => m.Email).Join(", ");
			}
		}

		private void sendBills_Click(object sender, EventArgs e) { SendBills(); }
		private void cancel_Click(object sender, EventArgs e) { Close(); }
	}
}