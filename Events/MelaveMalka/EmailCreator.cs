using System.Globalization;
using System.IO;
using System.Net.Mail;
using System.Web;
using System.Web.Compilation;
using System.Web.Hosting;
using System.Web.UI;
using ShomreiTorah.Common;
using ShomreiTorah.Data;

namespace ShomreiTorah.Billing.Events.MelaveMalka {
	static class EmailCreator {
		public static EmailPage<TRow> CreatePage<TRow>(string virtualPath) {
			if (HttpRuntime.AppDomainAppVirtualPath == null) return null;
			return BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(EmailPage<TRow>)) as EmailPage<TRow>;
		}

		public static MailMessage CreateMessage<TRow>(TRow row, string virtualPath) {
			var page = CreatePage<TRow>(virtualPath);
			page.Row = row;

			return new MailMessage {
				Body = page.RenderPage().Trim(),
				IsBodyHtml = true,
				Subject = page.EmailSubject,
				SubjectEncoding = Email.DefaultEncoding,
				BodyEncoding = Email.DefaultEncoding
			};
		}
	}

	//For some reason, the hosted ASP.Net runtime cannot parse
	//generic base types.  To avoid this, I make a non-generic
	//inherited class for the ASPX pages to inherit.
	//TODO: Remove after switching to Razor
	public abstract class EmailPage<TRow> : Page {
		public TRow Row { get; internal set; }

		public abstract string EmailSubject { get; }

		internal string RenderPage() {
			var request = new SimpleWorkerRequest("", null, null);
			HttpContext.Current = new HttpContext(request);
			ProcessRequest(new HttpContext(request));
			using (var writer = new StringWriter(CultureInfo.InvariantCulture)) {
				using (var htmlWriter = new HtmlTextWriter(writer))
					RenderControl(htmlWriter);
				return writer.ToString();
			}
		}
	}
	public abstract class CallerEmailPage : EmailPage<Caller> { }
}