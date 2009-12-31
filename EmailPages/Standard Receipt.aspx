<%@ Page Language="C#" Inherits="ShomreiTorah.Billing.Export.EmailPage, ShomreiTorah.Billing" %>

<%@ Assembly Name="System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=B77A5C561934E089" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="ShomreiTorah.Billing.Export" %>

<script runat="server">
	public override string EmailSubject { get { return "Shomrei Torah Receipt"; } }
	public override BillKind Kind { get { return BillKind.Receipt; } }
</script>

<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html>

	<head>
		<title>Shomrei Torah Receipt</title>
		<style type="text/css">
			body {
				font-family: Verdana;
			}
			td {
				border-bottom: solid 1px gray;
			}
			.Total td {
				border-top: solid 1px black;
				border-bottom: solid 2px black;
				padding-top: 10px;
			}
		</style>
	</head>

	<body>
		<h1 style="font-size: large; text-align: center;">
			Congregation Shomrei Torah Annual Contributions Summary</h1>
		<p>
			On behalf of Rabbi Weinberger and Congregation Shomrei Torah of Passaic Clifton,
			I would like to express my sincere gratitude and appreciation for your generous
			contribution(s) to our shul. Below please review your
			<%=DateTime.Today.AddMonths(-2).Year %>
			annual contributions summary. If you have any questions, please reply to this email
			or call me at (732) 516 - 5583.</p>
		<p>
			May your support of our Shul bring you ברכה and הצלחה in all of your endeavors.
		</p>
		<p>
			No goods or services have been provided.</p>
		<table cellspacing="0" class="Payments">
			<%foreach (var account in Info.Accounts) {%>
			<thead>
				<tr>
					<th colspan="3" style="font-size: large; text-align: center; padding: 25px 0 7px 0;
						border-bottom: solid 2px black;">
						<%=Server.HtmlEncode(account.AccountName) %></th>
				</tr>
			</thead>
			<%foreach (var payment in account.Payments) {%>
			<tr>
				<td class="Date" style="padding-right: 8px;">
					<%=payment.Date.ToShortDateString() %></td>
				<td class="Description">
					<%=payment.Method.Replace("Unknown", "?")%>
					<%if (!payment.IsCheckNumberNull()) {%>#<%=payment.CheckNumber %><%} %></td>
				<td class="Amount" style="text-align: right;">
					<%=payment.Amount.ToString("c") %></td>
			</tr>
			<%} %>
			<tr class="Total">
				<td class="Description" colspan="2">Total:</td>
				<td class="Amount" style="text-align: right; font-weight: bold;">
					<%=account.Payments.Sum(p => p.Amount).ToString("c")%></td>
			</tr>
			<%} %>
		</table>
	</body>

</html>