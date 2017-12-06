using System;
using System.Collections;

namespace Framework
{
	/// <summary>
	/// Summary description for PickLists.
	/// </summary>
	public sealed class PickLists
	{
		private static Hashtable mAreaQuadrants = null;
		private static Hashtable mNameSuffixes = null;
		private static Hashtable mStates = null;
		private static Hashtable mStreetTypes = null;
		private static Hashtable mSSAStates = null;
		private static Hashtable mReverseStates = null;

		private PickLists()
		{
			//
			// private to prevent instantiation
			//
		}

		public static Hashtable AreaQuadrants
		{
			get 
			{
				if ( mAreaQuadrants == null )
				{
					mAreaQuadrants = new Hashtable();

					mAreaQuadrants.Add("", "(None)");
					mAreaQuadrants.Add("NE", "Northeast");
					mAreaQuadrants.Add("NW", "Northwest");
					mAreaQuadrants.Add("SE", "Southeast");
					mAreaQuadrants.Add("SW", "Southwest");
				}

				return mAreaQuadrants;
			}
		}

		public static Hashtable NameSuffixes
		{
			get 
			{
				if ( mNameSuffixes == null )
				{
					mNameSuffixes = new Hashtable();

					mNameSuffixes.Add("", "(None)");
					mNameSuffixes.Add("JR", "Junior");
					mNameSuffixes.Add("SR", "Senior");
					mNameSuffixes.Add("ST", "First (I)");
					mNameSuffixes.Add("ND", "Second (II)");
					mNameSuffixes.Add("RD", "Third (III)");
					mNameSuffixes.Add("TH", "Fourth (IV)");
				}

				return mNameSuffixes;
			}
		}

		// added for reverse lookup support
		public static Hashtable ReverseStates
		{
			get
			{
				if ( mReverseStates == null )
				{
					Hashtable states = States;
					mReverseStates = new Hashtable(states.Count);

					foreach (object key in states.Keys)
					{
						mReverseStates.Add(states[key], key);
					}
				}

				return mReverseStates;
			}
		}

		public static Hashtable States
		{
			get
			{
				if ( mStates == null )
				{
					mStates = new Hashtable(63);

					mStates.Add("", "(None)");
					mStates.Add("AL", "Alabama");
					mStates.Add("AK", "Alaska");
					mStates.Add("AS", "American Samoa");
					mStates.Add("AZ", "Arizona");
					mStates.Add("AR", "Arkansas");
					mStates.Add("CA", "California");
					mStates.Add("CO", "Colorado");
					mStates.Add("CT", "Connecticut");
					mStates.Add("DE", "Delaware");
					mStates.Add("DC", "District of Columbia");
					mStates.Add("FM", "Federal State of Micronesia Island");
					mStates.Add("FL", "Florida");
					mStates.Add("GA", "Georgia");
					mStates.Add("GU", "Guam");
					mStates.Add("HI", "Hawaii");
					mStates.Add("IA", "Iowa");
					mStates.Add("ID", "Idaho");
					mStates.Add("IL", "Illinois");
					mStates.Add("IN", "Indiana");
					mStates.Add("KS", "Kansas");
					mStates.Add("KY", "Kentucky");
					mStates.Add("LA", "Louisiana");
					mStates.Add("ME", "Maine");
					mStates.Add("MH", "Marshall Islands");
					mStates.Add("MD", "Maryland");
					mStates.Add("MA", "Massachusetts");
					mStates.Add("MI", "Michigan");
					mStates.Add("MN", "Minnesota");
					mStates.Add("MS", "Mississippi");
					mStates.Add("MO", "Missouri");
					mStates.Add("MT", "Montana");
					mStates.Add("NE", "Nebraska");
					mStates.Add("NV", "Nevada");
					mStates.Add("NH", "New Hampshire");
					mStates.Add("NJ", "New Jersey");
					mStates.Add("NM", "New Mexico");
					mStates.Add("NY", "New York");
					mStates.Add("NC", "North Carolina");
					mStates.Add("ND", "North Dakota");
					mStates.Add("MP", "North Mariana Islands");
					mStates.Add("OH", "Ohio");
					mStates.Add("OK", "Oklahoma");
					mStates.Add("OR", "Oregon");
					mStates.Add("PW", "Palan Island");
					mStates.Add("PA", "Pennsylvania");
					mStates.Add("PR", "Puerto Rico");
					mStates.Add("RI", "Rhode Island");
					mStates.Add("SC", "South Carolina");
					mStates.Add("SD", "South Dakota");
					mStates.Add("TN", "Tennessee");
					mStates.Add("TX", "Texas");
					mStates.Add("UT", "Utah");
					mStates.Add("VT", "Vermont");
					mStates.Add("VA", "Virginia");
					mStates.Add("VI", "Virgin Islands (U.S.)");
					mStates.Add("WA", "Washington");
					mStates.Add("WV", "West Virgina");
					mStates.Add("WI", "Wisconsin");
					mStates.Add("WY", "Wyoming");
					mStates.Add("AA", "AA - APO/FPO Military Location");
					mStates.Add("AE", "AE - APO/FPO Military Location");
					mStates.Add("AP", "AP - APO/FPO Military Location");
				}

				return mStates;
			}
		}

		public static Hashtable SSAStates
		{
			get 
			{
				if ( mSSAStates == null )
				{
					mSSAStates = new Hashtable(56);

					mSSAStates.Add("", "");
					mSSAStates.Add("01", "AL");
					mSSAStates.Add("02", "AK");
					mSSAStates.Add("03", "AZ");
					mSSAStates.Add("04", "AR");
					mSSAStates.Add("05", "CA");
					mSSAStates.Add("06", "CO");
					mSSAStates.Add("07", "CT");
					mSSAStates.Add("08", "DE");
					mSSAStates.Add("09", "DC");
					mSSAStates.Add("10", "FL");
					mSSAStates.Add("11", "GA");
					mSSAStates.Add("12", "HI");
					mSSAStates.Add("13", "ID");
					mSSAStates.Add("14", "IL");
					mSSAStates.Add("15", "IN");
					mSSAStates.Add("16", "IA");
					mSSAStates.Add("17", "KS");
					mSSAStates.Add("18", "KY");
					mSSAStates.Add("19", "LA");
					mSSAStates.Add("20", "ME");
					mSSAStates.Add("21", "MD");
					mSSAStates.Add("22", "MA");
					mSSAStates.Add("23", "MI");
					mSSAStates.Add("24", "MN");
					mSSAStates.Add("25", "MS");
					mSSAStates.Add("26", "MO");
					mSSAStates.Add("27", "MT");
					mSSAStates.Add("28", "NE");
					mSSAStates.Add("29", "NV");
					mSSAStates.Add("30", "NH");
					mSSAStates.Add("31", "NJ");
					mSSAStates.Add("32", "NM");
					mSSAStates.Add("33", "NY");
					mSSAStates.Add("34", "NC");
					mSSAStates.Add("35", "ND");
					mSSAStates.Add("36", "OH");
					mSSAStates.Add("37", "OK");
					mSSAStates.Add("38", "OR");
					mSSAStates.Add("39", "PA");
					mSSAStates.Add("40", "PR");
					mSSAStates.Add("41", "RI");
					mSSAStates.Add("42", "SC");
					mSSAStates.Add("43", "SD");
					mSSAStates.Add("44", "TN");
					mSSAStates.Add("45", "TX");
					mSSAStates.Add("46", "UT");
					mSSAStates.Add("47", "VT");
					mSSAStates.Add("48", "VI");
					mSSAStates.Add("49", "VA");
					mSSAStates.Add("50", "WA");
					mSSAStates.Add("51", "WV");
					mSSAStates.Add("52", "WI");
					mSSAStates.Add("53", "WY");
					mSSAStates.Add("64", "AS");
					mSSAStates.Add("65", "GU");
				}

				return mSSAStates;
			}
		}

		public static Hashtable StreetTypes
		{
			get 
			{
				if ( mStreetTypes == null )
				{
					mStreetTypes = new Hashtable();

					mStreetTypes.Add("", "(None)");
					mStreetTypes.Add("AX", "Annex");
					mStreetTypes.Add("AV", "Avenue");
					mStreetTypes.Add("BV", "Boulevard");
					mStreetTypes.Add("BD", "Building");
					mStreetTypes.Add("CW", "Causeway");
					mStreetTypes.Add("CN", "Center");
					mStreetTypes.Add("CH", "Chemin");
					mStreetTypes.Add("CR", "Circle");
					mStreetTypes.Add("CO", "Concourse");
					mStreetTypes.Add("CT", "Court");
					mStreetTypes.Add("CV", "Cove");
					mStreetTypes.Add("CS", "Crescent");
					mStreetTypes.Add("XG", "Crossing");
					mStreetTypes.Add("DR", "Drive");
					mStreetTypes.Add("HT", "Heights");
					mStreetTypes.Add("HY", "Highway");
					mStreetTypes.Add("LN", "Lane");
					mStreetTypes.Add("MT", "Montee");
					mStreetTypes.Add("LP", "Loop");
					mStreetTypes.Add("PK", "Park");
					mStreetTypes.Add("PY", "Parkway");
					mStreetTypes.Add("PL", "Place");
					mStreetTypes.Add("PZ", "Plaza");
					mStreetTypes.Add("RG", "Range");
					mStreetTypes.Add("RD", "Road");
					mStreetTypes.Add("RU", "Rue");
					mStreetTypes.Add("RL", "Ruelle");
					mStreetTypes.Add("SQ", "Square");
					mStreetTypes.Add("TA", "Station");
					mStreetTypes.Add("ST", "Street");
					mStreetTypes.Add("TC", "Terrace");
					mStreetTypes.Add("TP", "Turnpike");
					mStreetTypes.Add("TR", "Trail");
					mStreetTypes.Add("VW", "View");
					mStreetTypes.Add("WY", "Way");
				}

				return mStreetTypes;
			}
		}

	}
}
