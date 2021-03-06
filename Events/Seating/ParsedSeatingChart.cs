using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using ShomreiTorah.Data;

namespace ShomreiTorah.Billing.Events.Seating {
	///<summary>Holds information parsed from a seating chart.</summary>
	class ParsedSeatingChart {
		public ParsedSeatingChart(IEnumerable<SeatRow> rows) {
			Rows = new ReadOnlyCollection<SeatRow>(rows.ToArray());
		}

		public ReadOnlyCollection<SeatRow> Rows { get; private set; }

		public IEnumerable<SeatGroup> AllSeats { get { return Rows.SelectMany(r => r.Seats); } }
	}
	class SeatRow {
		public SeatRow(IEnumerable<SeatGroup> seats) { Seats = new ReadOnlyCollection<SeatGroup>(seats.ToArray()); }
		public ReadOnlyCollection<SeatGroup> Seats { get; private set; }

		public override string ToString() { return "Row - " + Seats.Count + " people; " + Seats.Sum(s => s.SeatCount) + " seats"; }
	}
	abstract class SeatGroup {
		static readonly Regex SpaceTrimmer = new Regex(@"\s+");
		protected SeatGroup(string name, int seatCount, float seatWidth) {
			Name = SpaceTrimmer.Replace(name, " ");
			SeatCount = seatCount;
			SeatWidth = seatWidth;
		}

		public string Name { get; private set; }
		public int SeatCount { get; private set; }
		public float SeatWidth { get; private set; }

		public bool CheckWidth() {
			return Math.Abs((SeatWidth - SeatCount)) < .5;
		}

		public abstract void Select();

		public override string ToString() { return Name + " - " + SeatCount; }

		public virtual bool Matches(Person person) {
			if (String.IsNullOrEmpty(person.LastName))
				return false;
			if (String.IsNullOrEmpty(person.HisName))
				return person.LastName == Name;
			return person.HisName[0] == Name[0] && person.LastName == Name.Substring(2);
		}
	}
}
