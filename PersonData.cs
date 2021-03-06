using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using ShomreiTorah.Data;
using ShomreiTorah.Data.UI;
using ShomreiTorah.Singularity;

namespace ShomreiTorah.Billing {
	public class PersonData {
		[SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly ReadOnlyCollection<string> FieldNames = new ReadOnlyCollection<string>(new[]{
			"FullName", "HisName", "HerName", "LastName", "Address", "City", "State", "Zip", "Phone"
		});

		string pAddress, pState, pZip, pPhone;
		#region Properties
		///<summary>Gets or sets the person's full name.</summary>
		public string FullName { get; set; }
		///<summary>Gets or sets the husband's name.</summary>
		public string HisName { get; set; }
		///<summary>Gets or sets the wife's name.</summary>
		public string HerName { get; set; }
		///<summary>Gets or sets the person's last name.</summary>
		public string LastName { get; set; }
		///<summary>Gets or sets the person's strret address.</summary>
		public string Address {
			get { return pAddress; }
			set { pAddress = (String.IsNullOrEmpty(value) || value.Contains("**")) ? null : value; }
		}
		///<summary>Gets or sets the person's city.</summary>
		public string City { get; set; }
		///<summary>Gets or sets the person's state.</summary>
		public string State {
			get { return pState; }
			set {
				if (String.IsNullOrEmpty(value))
					pState = null;
				else
					pState = UsStates.Abbreviate(value);
			}
		}
		///<summary>Gets or sets the person's zip code.</summary>
		public string Zip {
			get { return pZip; }
			set { pZip = String.IsNullOrEmpty(value) || value == "99999" ? null : value.PadLeft(5, '0'); }
		}
		///<summary>Gets or sets the person's formatted phone number.</summary>
		public string Phone {
			get { return pPhone; }
			set { pPhone = value.FormatPhoneNumber(); }
		}
		#endregion

		///<summary>Constructs an empty PersonData.</summary>
		public PersonData() { }

		///<summary>Constructs a new PersonData from a DataRow.</summary>
		///<param name="ykRow">Any ykRow with the standard set of fields.</param>
		public PersonData(DataRow row) {
			if (row == null)
				throw new ArgumentNullException("row");
			FullName = row["FullName"].ToString();

			HisName = row["HisName"].ToString();
			HerName = row["HerName"].ToString();
			LastName = row["LastName"].ToString();

			Address = row["Address"].ToString();
			City = row["City"].ToString();
			State = row["State"].ToString();
			Zip = row["Zip"].ToString();

			pPhone = row["Phone"].ToString();
		}
		///<summary>Constructs a new PersonData from a Singularity Row.</summary>
		///<param name="ykRow">Any ykRow with the standard set of fields.</param>
		public PersonData(Row row) {
			if (row == null)
				throw new ArgumentNullException("row");
			FullName = row.Field<string>("FullName");

			HisName = row.Field<string>("HisName");
			HerName = row.Field<string>("HerName");
			LastName = row.Field<string>("LastName");

			Address = row.Field<string>("Address");
			City = row.Field<string>("City");
			State = row.Field<string>("State");
			Zip = row.Field<string>("Zip");

			pPhone = row.Field<string>("Phone");
		}

		///<summary>Checks whether this instance contains any data.</summary>
		///<returns>true if all of the fields are null or empty.</returns>
		public bool IsEmpty() {
			return String.IsNullOrEmpty(FullName)

				&& String.IsNullOrEmpty(HisName)
				&& String.IsNullOrEmpty(HerName)
				&& String.IsNullOrEmpty(LastName)

				&& Address.IsInvalidAddress()

				&& String.IsNullOrEmpty(City)
				&& String.IsNullOrEmpty(State)
				&& String.IsNullOrEmpty(Zip)

				&& String.IsNullOrEmpty(Phone);
		}
		/////<summary>Checks whether this instance is incomplete.</summary>
		/////<returns>true if any of the fields are null or empty.</returns>
		//public bool IsIncomplete() {
		//    return String.IsNullOrEmpty(FullName)

		//        || String.IsNullOrEmpty(HisName)
		//        || String.IsNullOrEmpty(HerName)
		//        || String.IsNullOrEmpty(LastName)

		//        || String.IsNullOrEmpty(Address)
		//        || String.IsNullOrEmpty(City)
		//        || String.IsNullOrEmpty(State)
		//        || String.IsNullOrEmpty(Zip)

		//        || String.IsNullOrEmpty(Phone);
		//}


		public static bool operator ==(PersonData a, PersonData b) { return Equals(a, b); }
		public static bool operator !=(PersonData a, PersonData b) { return !Equals(a, b); }

		public override bool Equals(object obj) {
			if (ReferenceEquals(obj, this)) return true;
			var other = obj as PersonData;
			if (other == null) return false;

			return FullName == other.FullName

				&& HisName == other.HisName
				&& HerName == other.HerName
				&& LastName == other.LastName

				&& Address == other.Address
				&& City == other.City
				&& State == other.State
				&& Zip == other.Zip

				&& Phone == other.Phone;
		}
		public override int GetHashCode() {
			return
				(FullName ?? "").GetHashCode() ^

				(HisName ?? "").GetHashCode() ^
				(HerName ?? "").GetHashCode() ^
				(LastName ?? "").GetHashCode() ^

				(Address ?? "").GetHashCode() ^
				(City ?? "").GetHashCode() ^
				(State ?? "").GetHashCode() ^
				(Zip ?? "").GetHashCode() ^

				(Phone ?? "").GetHashCode();
		}

		///<summary>Checks whether the given ykRow contradicts this value.</summary>
		///<param name="ykRow">The ykRow to check.</param>
		///<returns>true if any property is not null or empty and has a different value than the ykRow's corresponding property.</returns>
		public bool IsUnequal(Row row) {
			if (row == null)
				throw new ArgumentNullException("row", "row is null.");

			return
				   (!String.IsNullOrEmpty(FullName) && FullName != row["FullName"].ToString())

				|| (!String.IsNullOrEmpty(HisName) && HisName != row["HisName"].ToString())
				|| (!String.IsNullOrEmpty(HerName) && HerName != row["HerName"].ToString())
				|| (!String.IsNullOrEmpty(LastName) && LastName != row["LastName"].ToString())

				|| (!String.IsNullOrEmpty(Address) && !Address.IsInvalidAddress() && Address != row["Address"].ToString())
				|| (!String.IsNullOrEmpty(City) && City != row["City"].ToString())
				|| (!String.IsNullOrEmpty(State) && State != row["State"].ToString())
				|| (!String.IsNullOrEmpty(Zip) && Zip != row["Zip"].ToString())

				|| (!String.IsNullOrEmpty(Phone) && Phone != row["Phone"].ToString());
		}

		public string ToFullString() {
			StringBuilder retVal = new StringBuilder();

			if (!String.IsNullOrEmpty(HisName)) {
				retVal.Append(HisName);
				if (!String.IsNullOrEmpty(HerName))
					retVal.Append(" and ");
			}

			if (!String.IsNullOrEmpty(HerName))
				retVal.Append(HerName);

			if (retVal.Length != 0 && !String.IsNullOrEmpty(LastName)) retVal.Append(" ");

			if (!String.IsNullOrEmpty(LastName)) retVal.Append(LastName);

			if (!String.IsNullOrEmpty(Phone))
				retVal.AppendFormat(", {0}", Phone);

			if (retVal.Length != 0) retVal.AppendLine();

			if (!String.IsNullOrEmpty(FullName)) retVal.AppendLine(FullName);
			if (!String.IsNullOrEmpty(Address)) retVal.AppendLine(Address);
			retVal.Append(City);
			if (!String.IsNullOrEmpty(City) && !String.IsNullOrEmpty(State)) retVal.Append(", ");
			retVal.Append(State);
			if (!String.IsNullOrEmpty(Zip)) {
				retVal.Append(" ");
				retVal.AppendLine(Zip);
			}

			return retVal.ToString().Trim();
		}
	}
}
