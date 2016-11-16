﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ShomreiTorah.Common;
using ShomreiTorah.Data;
using ShomreiTorah.Data.UI;
using ShomreiTorah.WinForms.Forms;

namespace ShomreiTorah.Billing.PaymentImport {
	[Export]
	public class ViewModel : INotifyPropertyChanged {
		///<summary>All payments from the source (including already-imported payments).</summary>
		IReadOnlyCollection<PaymentInfo> allPayments;

		Dictionary<string, ImportingPayment> importingPayments = new Dictionary<string, ImportingPayment>();
		ImportingPayment currentImport;

		///<summary>Stores user input for a single payment that is in the process of being imported.</summary>
		class ImportingPayment {
			// All properties of the current import must be stored here.
			// They must only be mutated through the wrapper properties,
			// so that we always raise PropertyChanged for data-binding.
			public ReadOnlyCollection<Person> matchingPeople;
			public Person person;
			public string comments;
			public bool createPledge;
			public decimal pledgeAmount;
			public string pledgeType;
			public string pledgeSubType;
			public string pledgeNote;

			///<summary>Matches found directly from the master directory.  User-selected people are concatenated to this.</summary>
			public readonly IReadOnlyCollection<Person> directMatches;

			public ImportingPayment(PaymentInfo payment) {
				comments = "\n" + payment.Comments;
				// TODO: Infer type from payment comments & amount
				pledgeAmount = payment.Amount;

				directMatches = matchingPeople = Matcher.FindMatches(payment).ToList().AsReadOnly();
				person = directMatches.Count == 1 ? directMatches.First() : null;
			}
		}


		ReadOnlyCollection<PaymentInfo> availablePayments;
		///<summary>Gets all payments that are ready to import.</summary>
		public ReadOnlyCollection<PaymentInfo> AvailablePayments {
			get { return availablePayments; }
			private set { availablePayments = value; OnPropertyChanged(); }
		}


		///<summary>Gets existing people that may match the current payment.</summary>
		public ReadOnlyCollection<Person> MatchingPeople {
			get { return currentImport?.matchingPeople; }
			private set { currentImport.matchingPeople = value; OnPropertyChanged(); }
		}


		PaymentInfo currentPayment;
		///<summary>Gets or sets the payment currently being imported.</summary>
		public PaymentInfo CurrentPayment {
			get { return currentPayment; }
			set {
				currentPayment = value;
				if (value != null) {
					// If we haven't started importing this payment already, initialize its backing object.
					if (!importingPayments.TryGetValue(value.Id, out currentImport)) {
						currentImport = new ImportingPayment(value);
						importingPayments.Add(value.Id, currentImport);
						OnNewPaymentSelected();
					}
					OnPropertyChanged(nameof(Comments));
					OnPropertyChanged(nameof(CreatePledge));
					OnPropertyChanged(nameof(PledgeType));
					OnPropertyChanged(nameof(PledgeSubType));
					OnPropertyChanged(nameof(PledgeNote));
					OnPropertyChanged(nameof(PledgeAmount));
					OnPropertyChanged(nameof(Person));
					OnPropertyChanged(nameof(MatchingPeople));
				}
				OnPropertyChanged();
			}
		}
		///<summary>Occurs when a new payment is selected for import, allowing the caller to preset properties.</summary>
		public event EventHandler NewPaymentSelected;
		///<summary>Raises the NewPaymentSelected event.</summary>
		internal protected virtual void OnNewPaymentSelected() => OnNewPaymentSelected(EventArgs.Empty);
		///<summary>Raises the NewPaymentSelected event.</summary>
		///<param name="e">An EventArgs object that provides the event data.</param>
		protected internal virtual void OnNewPaymentSelected(EventArgs e) => NewPaymentSelected?.Invoke(this, e);


		///<summary>Gets or sets the person who will own the created payment.</summary>
		public Person Person {
			get { return currentImport?.person; }
			set {
				currentImport.person = value;
				var people = currentImport.directMatches.ToList();
				if (value != null && !currentImport.directMatches.Contains(value))
					people.Add(value);
				MatchingPeople = people.AsReadOnly();
				OnPropertyChanged();
			}
		}


		///<summary>Gets or sets the comments field of the payment to create.</summary>
		public string Comments {
			get { return currentImport?.comments; }
			set { currentImport.comments = value; OnPropertyChanged(); }
		}

		///<summary>Gets or sets whether to also create a linked pledge when importing the payment.</summary>
		public bool CreatePledge {
			get { return currentImport?.createPledge ?? false; }
			set { currentImport.createPledge = value; OnPropertyChanged(); }
		}
		///<summary>Gets or sets the amount of the pledge to create.</summary>
		public decimal PledgeAmount {
			get { return currentImport?.pledgeAmount ?? 0; }
			set { currentImport.pledgeAmount = value; OnPropertyChanged(); }
		}

		///<summary>Gets or sets the Type field of the pledge to create.</summary>
		public string PledgeType {
			get { return currentImport?.pledgeType; }
			set { currentImport.pledgeType = value; OnPropertyChanged(); }
		}

		///<summary>Gets or sets the SubType field of the pledge to create.</summary>
		public string PledgeSubType {
			get { return currentImport?.pledgeSubType; }
			set { currentImport.pledgeSubType = value; OnPropertyChanged(); }
		}
		///<summary>Gets or sets the Note field of the pledge to create.</summary>
		public string PledgeNote {
			get { return currentImport?.pledgeNote; }
			set { currentImport.pledgeNote = value; OnPropertyChanged(); }
		}



		readonly IPaymentSource source;

		[ImportingConstructor]
		public ViewModel([ImportMany] IEnumerable<IPaymentSource> sources) {
			source = sources.Single(s => s.Name == Config.ReadAttribute("Billing", "PaymentImport", "Source", "Name"));
		}

		///<summary>Loads payments to import from the current source.</summary>
		public void LoadPayments(DateTime start) {
			AppFramework.LoadTables(ImportedPayment.Schema);
			if (!ProgressWorker.Execute(p => {
				p.Caption = "Loading payments after " + start.ToShortDateString();
				allPayments = source.GetPayments(start).OrderBy(pi => pi.Date).ToList();
			}, cancellable: true))
				return;
			RefreshPayments();
		}

		private void RefreshPayments() {
			if (!allPayments.Any()) {
				AvailablePayments = new ReadOnlyCollection<PaymentInfo>(new PaymentInfo[0]);
				return;
			}
			var start = allPayments.First().Date;
			var alreadyImported = new HashSet<string>(
				AppFramework.Table<ImportedPayment>().Rows
					.Where(ip => ip.Source == source
					.Name && ip.Payment.Date >= start)
					.Select(ip => ip.ExternalId));
			AvailablePayments = allPayments.Where(p => !alreadyImported.Contains(p.Id)).ToList().AsReadOnly();
		}



		public void Import() {
			if (!AppFramework.Table<EmailAddress>().Rows
					.Any(r => r.Email.Equals(CurrentPayment.Email, StringComparison.OrdinalIgnoreCase))) {
				AppFramework.Table<EmailAddress>().Rows.Add(new EmailAddress {
					Email = CurrentPayment.Email,
					Name = CurrentPayment.FirstName + " " + CurrentPayment.LastName,
					DateAdded = CurrentPayment.Date,
					Person = Person
				});
			}

			var payment = new Payment {
				Account = Names.DefaultAccount,
				CheckNumber = CurrentPayment.FinalFour,
				Amount = CurrentPayment.Amount,
				Comments = Comments.Trim(),
				Date = CurrentPayment.Date,
				Method = "Credit Card",
				Person = Person,
			};
			AppFramework.Table<Payment>().Rows.Add(payment);
			AppFramework.Table<ImportedPayment>().Rows.Add(new ImportedPayment {
				DateImported = DateTime.Now,
				ExternalId = CurrentPayment.Id,
				ImportingUser = Environment.UserName,
				Payment = payment,
				Source = source.Name
			});
			if (CreatePledge) {
				var pledge = new Pledge {
					Account = payment.Account,
					Amount = PledgeAmount,
					Comments = "Created for credit card payment:\n" + payment.Comments,
					Date = payment.Date,
					Person = payment.Person,
					Type = PledgeType,
					SubType = PledgeSubType ?? "",
					Note = PledgeNote
				};

				AppFramework.Table<PledgeLink>().Rows.Add(new PledgeLink {
					Amount = pledge.Amount,
					Pledge = pledge,
					Payment = payment
				});
				AppFramework.Table<Pledge>().Rows.Add(pledge);
			}
			RefreshPayments();
		}

		///<summary>Occurs when a property value is changed.</summary>
		public event PropertyChangedEventHandler PropertyChanged;
		///<summary>Raises the PropertyChanged event.</summary>
		///<param name="name">The name of the property that changed.</param>
		protected virtual void OnPropertyChanged([CallerMemberName] string name = null) => OnPropertyChanged(new PropertyChangedEventArgs(name));
		///<summary>Raises the PropertyChanged event.</summary>
		///<param name="e">An EventArgs object that provides the event data.</param>
		protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, e);
	}
}

