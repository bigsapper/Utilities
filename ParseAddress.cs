using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

// TODO: Identify and correct other non-parsing edge cases
// TODO: Improve efficency with generics and other

// DONE: Parse addresses with house numbers that have leading non-numeric characters
// DONE: Remove parsing of City/State/Zip
// DONE: Preserve unit number and unit type when parsing
// DONE: Parse PO Box/RR addresses
// DONE: Add check to see if Address1 & Address2 fields are reversed

///<summary>
/// 
/// The GeoStreetAddressUS class is a port
/// of the PERL module Geo::StreetAddress::US
/// 
/// SYNOPSIS
///
/// parser = new GeoStreetAddressUS.Parser() ;
/// 
/// (string) locType = parser.ParsedAddress_type("Mission Street at Valencia Street, San Francisco, CA");
/// if ( locType == 'Intersection' ) {
///    exit;        // don't need no intersections
/// }
/// 
/// ParsedAddress = parser.parse_address ("1005 Gravenstein Hwy N, Sebastopol CA 95472" );
/// 
/// address  = parser.parse_address("1600 Pennsylvania Ave, Washington, DC" );
/// 
///</summary>
namespace Framework
{
	/// <summary>
	/// Parser class
	/// </summary>
	public sealed class ParseAddress
	{
		#region Public HashTables

		/// <summary>
		/// Directional - a hash table to convert directional names to abbreviations
		/// </summary>
		public Dictionary<string, string> Directional;
		/// <summary>
		/// Inverse conversion, from code to long name
		/// </summary>
		public Dictionary<string, string> DirectionCode;
		/// <summary>
		/// Street Type conversion table
		/// </summary>
		public Dictionary<string, string> StreetType;
		/// <summary>
		/// All possible street type codes
		/// </summary>
		public Dictionary<string, int> StreetTypeList;
		/// <summary>
		/// Unit Type conversion table
		/// </summary>
		public Dictionary<string, string> UnitType;

		#endregion

		// made public for use by other routines
		public const string REGEX_UNIT = @"(?<type>\W+(?:(?:su?i?te|p\W*[om]\W*b(?:ox)?|dept|ro*m|fl(?:oor)?|apt|unit|box|lot|tr(?:ai)?l(?:e)?r)\W*|#\W*))(?<number>[\w-]+)?";
		public const string REGEX_HIGHWAY = @"\b(US|STATE|HIGHWAY|HWY|ROUTE|COUNTY|FM)\b";
		public const string REGEX_POBOX = @"^(?<street>(((P\s?(O|0)\s?)?|((R\s?(R|T)))\s\d+\s)(BOX))|POB|P0B|(H\s?C(\s?R|\s?1)?(\s?BOX)?))(\s(?<number>\w+))?";

		#region Private Members & Constructors

		/// <summary>
		/// regular expression for street type codes
		/// </summary>
		private string rgxType;
		/// <summary>
		/// Regex to recognize and extract the number part of an address
		/// including the optional fraction
		/// </summary>
		private Regex rgxNumber;
		/// <summary>
		/// Regex for the recognition of the Unit part
		/// of an address
		/// </summary>
		private Regex rgxUnit;
		private string rgxDirect;
		private string rgxDirCode;
		private string rgxStreet;
		// added for PO/RR/HC box matching
		private Regex rgxPOBox;

		private bool mDebugBadAddresses = false;

		/// <summary>
		/// Place to collect the parsed result
		/// </summary>
		private ParsedAddress locRes;
		/// <summary>
		/// Constructor, initialize internal data structures
		/// </summary>

		public ParseAddress()
		{
			this.Directional = this._initDirectional();
			this.DirectionCode = this._reverse(this.Directional);
			this.StreetType = this._initStreetType();
			this.StreetTypeList = this._mergeKeyVal(this.StreetType);
			this.UnitType = this._initUnitType();

			this._buildRegexes();
		}

		public ParseAddress(bool DebugBadAddresses)
			: this()
		{
			mDebugBadAddresses = DebugBadAddresses;
		}

		#endregion

		#region Private Init Methods

		/// <summary>
		/// Utility method, reverse a hashtable with a 1-1
		/// key-value mapping:
		/// convert from key => value
		/// to value => key
		/// </summary>
		/// <param name="dir">
		/// Source hashtable
		/// </param>
		/// <returns>
		/// Inverted hashtable
		/// </returns>
		private Dictionary<string, string> _reverse(Dictionary<string, string> dir)
		{
			Dictionary<string, string> res = new Dictionary<string, string>(dir.Count, StringComparer.InvariantCultureIgnoreCase);

			foreach ( string key in dir.Keys )
			{
				res.Add(dir[key], key);
			}
			return res;
		}

		/// <summary>
		/// Get all keys and values from a Hashtable and make them
		/// to keys in a new Hashtables, using 1 as a value
		/// </summary>
		/// <param name="src"></param>
		/// <returns></returns>
		private Dictionary<string, int> _mergeKeyVal(Dictionary<string, string> src)
		{
			Dictionary<string, int> res = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);

			foreach ( string key in src.Keys )
			{
				if ( !res.ContainsKey(key) )
				{
					res.Add(key, 1);
				}

				if ( !res.ContainsKey(src[key]) )
				{
					res.Add(src[key], 1);
				}

			}
			return res;
		}

		/// <summary>
		/// _initDirectional, fills the Directional structure. The perl hashes are
		/// represented by the Hashtable collection.
		/// </summary>
		/// <returns>
		/// Return value: hashtable to convert long direction names into short
		/// </returns>
		private Dictionary<string, string> _initDirectional()
		{
			Dictionary<string, string> dir = new Dictionary<string, string>(8, StringComparer.InvariantCultureIgnoreCase);

			dir.Add("north", "N");
			dir.Add("northeast", "NE");
			dir.Add("east", "E");
			dir.Add("southeast", "SE");
			dir.Add("south", "S");
			dir.Add("southwest", "SW");
			dir.Add("west", "W");
			dir.Add("northwest", "NW");

			return dir;
		}

		/// <summary>
		/// Map allowed street type codes to their canonical representation
		/// </summary>
		/// <returns>
		/// Hashtable: street-type => canonical short name
		/// </returns>
		private Dictionary<string, string> _initStreetType()
		{
			Dictionary<string, string> res = new Dictionary<string, string>(362, StringComparer.InvariantCultureIgnoreCase);

			res.Add("allee", "aly");
			res.Add("alley", "aly");
			res.Add("ally", "aly");
			res.Add("anex", "anx");
			res.Add("annex", "anx");
			res.Add("annx", "anx");
			res.Add("arcade", "arc");
			res.Add("av", "ave");
			res.Add("aven", "ave");
			res.Add("avenu", "ave");
			res.Add("avenue", "ave");
			res.Add("avn", "ave");
			res.Add("avnue", "ave");
			res.Add("bayoo", "byu");
			res.Add("bayou", "byu");
			res.Add("beach", "bch");
			res.Add("bend", "bnd");
			res.Add("bluf", "blf");
			res.Add("bluff", "blf");
			res.Add("bluffs", "blfs");
			res.Add("bot", "btm");
			res.Add("bottm", "btm");
			res.Add("bottom", "btm");
			res.Add("boul", "blvd");
			res.Add("boulevard", "blvd");
			res.Add("boulv", "blvd");
			res.Add("branch", "br");
			res.Add("brdge", "brg");
			res.Add("bridge", "brg");
			res.Add("brnch", "br");
			res.Add("brook", "brk");
			res.Add("brooks", "brks");
			res.Add("burg", "bg");
			res.Add("burgs", "bgs");
			res.Add("bypa", "byp");
			res.Add("bypas", "byp");
			res.Add("bypass", "byp");
			res.Add("byps", "byp");
			res.Add("camp", "cp");
			res.Add("canyn", "cyn");
			res.Add("canyon", "cyn");
			res.Add("cape", "cpe");
			res.Add("causeway", "cswy");
			res.Add("causway", "cswy");
			res.Add("cen", "ctr");
			res.Add("cent", "ctr");
			res.Add("center", "ctr");
			res.Add("centers", "ctrs");
			res.Add("centr", "ctr");
			res.Add("centre", "ctr");
			res.Add("circ", "cir");
			res.Add("circl", "cir");
			res.Add("circle", "cir");
			res.Add("circles", "cirs");
			res.Add("ck", "crk");
			res.Add("cliff", "clf");
			res.Add("cliffs", "clfs");
			res.Add("club", "clb");
			res.Add("cmp", "cp");
			res.Add("cnter", "ctr");
			res.Add("cntr", "ctr");
			res.Add("cnyn", "cyn");
			res.Add("common", "cmn");
			res.Add("corner", "cor");
			res.Add("corners", "cors");
			res.Add("course", "crse");
			res.Add("court", "ct");
			res.Add("courts", "cts");
			res.Add("cove", "cv");
			res.Add("coves", "cvs");
			res.Add("cr", "crk");
			res.Add("crcl", "cir");
			res.Add("crcle", "cir");
			res.Add("crecent", "cres");
			res.Add("creek", "crk");
			res.Add("crescent", "cres");
			res.Add("cresent", "cres");
			res.Add("crest", "crst");
			res.Add("crossing", "xing");
			res.Add("crossroad", "xrd");
			res.Add("crscnt", "cres");
			res.Add("crsent", "cres");
			res.Add("crsnt", "cres");
			res.Add("crssing", "xing");
			res.Add("crssng", "xing");
			res.Add("crt", "ct");
			res.Add("curve", "curv");
			res.Add("dale", "dl");
			res.Add("dam", "dm");
			res.Add("div", "dv");
			res.Add("divide", "dv");
			res.Add("driv", "dr");
			res.Add("drive", "dr");
			res.Add("drives", "drs");
			res.Add("drv", "dr");
			res.Add("dvd", "dv");
			res.Add("estate", "est");
			res.Add("estates", "ests");
			res.Add("exp", "expy");
			res.Add("expr", "expy");
			res.Add("express", "expy");
			res.Add("expressway", "expy");
			res.Add("expw", "expy");
			res.Add("extension", "ext");
			res.Add("extensions", "exts");
			res.Add("extn", "ext");
			res.Add("extnsn", "ext");
			res.Add("falls", "fls");
			res.Add("ferry", "fry");
			res.Add("field", "fld");
			res.Add("fields", "flds");
			res.Add("flat", "flt");
			res.Add("flats", "flts");
			res.Add("ford", "frd");
			res.Add("fords", "frds");
			res.Add("forest", "frst");
			res.Add("forests", "frst");
			res.Add("forg", "frg");
			res.Add("forge", "frg");
			res.Add("forges", "frgs");
			res.Add("fork", "frk");
			res.Add("forks", "frks");
			res.Add("fort", "ft");
			res.Add("freeway", "fwy");
			res.Add("freewy", "fwy");
			res.Add("frry", "fry");
			res.Add("frt", "ft");
			res.Add("frway", "fwy");
			res.Add("frwy", "fwy");
			res.Add("garden", "gdn");
			res.Add("gardens", "gdns");
			res.Add("gardn", "gdn");
			res.Add("gateway", "gtwy");
			res.Add("gatewy", "gtwy");
			res.Add("gatway", "gtwy");
			res.Add("glen", "gln");
			res.Add("glens", "glns");
			res.Add("grden", "gdn");
			res.Add("grdn", "gdn");
			res.Add("grdns", "gdns");
			res.Add("green", "grn");
			res.Add("greens", "grns");
			res.Add("grov", "grv");
			res.Add("grove", "grv");
			res.Add("groves", "grvs");
			res.Add("gtway", "gtwy");
			res.Add("harb", "hbr");
			res.Add("harbor", "hbr");
			res.Add("harbors", "hbrs");
			res.Add("harbr", "hbr");
			res.Add("haven", "hvn");
			res.Add("havn", "hvn");
			res.Add("height", "hts");
			res.Add("heights", "hts");
			res.Add("hgts", "hts");
			res.Add("highway", "hwy");
			res.Add("highwy", "hwy");
			res.Add("hill", "hl");
			res.Add("hills", "hls");
			res.Add("hiway", "hwy");
			res.Add("hiwy", "hwy");
			res.Add("hllw", "holw");
			res.Add("hollow", "holw");
			res.Add("hollows", "holw");
			res.Add("holws", "holw");
			res.Add("hrbor", "hbr");
			res.Add("ht", "hts");
			res.Add("hway", "hwy");
			res.Add("inlet", "inlt");
			res.Add("island", "is");
			res.Add("islands", "iss");
			res.Add("isles", "isle");
			res.Add("islnd", "is");
			res.Add("islnds", "iss");
			res.Add("jction", "jct");
			res.Add("jctn", "jct");
			res.Add("jctns", "jcts");
			res.Add("junction", "jct");
			res.Add("junctions", "jcts");
			res.Add("junctn", "jct");
			res.Add("juncton", "jct");
			res.Add("key", "ky");
			res.Add("keys", "kys");
			res.Add("knol", "knl");
			res.Add("knoll", "knl");
			res.Add("knolls", "knls");
			res.Add("la", "ln");
			res.Add("lake", "lk");
			res.Add("lakes", "lks");
			res.Add("landing", "lndg");
			res.Add("lane", "ln");
			res.Add("lanes", "ln");
			res.Add("ldge", "ldg");
			res.Add("light", "lgt");
			res.Add("lights", "lgts");
			res.Add("lndng", "lndg");
			res.Add("loaf", "lf");
			res.Add("lock", "lck");
			res.Add("locks", "lcks");
			res.Add("lodg", "ldg");
			res.Add("lodge", "ldg");
			res.Add("loops", "loop");
			res.Add("manor", "mnr");
			res.Add("manors", "mnrs");
			res.Add("meadow", "mdw");
			res.Add("meadows", "mdws");
			res.Add("medows", "mdws");
			res.Add("mill", "ml");
			res.Add("mills", "mls");
			res.Add("mission", "msn");
			res.Add("missn", "msn");
			res.Add("mnt", "mt");
			res.Add("mntain", "mtn");
			res.Add("mntn", "mtn");
			res.Add("mntns", "mtns");
			res.Add("motorway", "mtwy");
			res.Add("mount", "mt");
			res.Add("mountain", "mtn");
			res.Add("mountains", "mtns");
			res.Add("mountin", "mtn");
			res.Add("mssn", "msn");
			res.Add("mtin", "mtn");
			res.Add("neck", "nck");
			res.Add("orchard", "orch");
			res.Add("orchrd", "orch");
			res.Add("overpass", "opas");
			res.Add("ovl", "oval");
			res.Add("parks", "park");
			res.Add("parkway", "pkwy");
			res.Add("parkways", "pkwy");
			res.Add("parkwy", "pkwy");
			res.Add("passage", "psge");
			res.Add("paths", "path");
			res.Add("pikes", "pike");
			res.Add("pine", "pne");
			res.Add("pines", "pnes");
			res.Add("pk", "park");
			res.Add("pkway", "pkwy");
			res.Add("pkwys", "pkwy");
			res.Add("pky", "pkwy");
			res.Add("place", "pl");
			res.Add("plain", "pln");
			res.Add("plaines", "plns");
			res.Add("plains", "plns");
			res.Add("plaza", "plz");
			res.Add("plza", "plz");
			res.Add("point", "pt");
			res.Add("points", "pts");
			res.Add("port", "prt");
			res.Add("ports", "prts");
			res.Add("prairie", "pr");
			res.Add("prarie", "pr");
			res.Add("prk", "park");
			res.Add("prr", "pr");
			res.Add("rad", "radl");
			res.Add("radial", "radl");
			res.Add("radiel", "radl");
			res.Add("ranch", "rnch");
			res.Add("ranches", "rnch");
			res.Add("rapid", "rpd");
			res.Add("rapids", "rpds");
			res.Add("rdge", "rdg");
			res.Add("rest", "rst");
			res.Add("ridge", "rdg");
			res.Add("ridges", "rdgs");
			res.Add("river", "riv");
			res.Add("rivr", "riv");
			res.Add("rnchs", "rnch");
			res.Add("road", "rd");
			res.Add("roads", "rds");
			res.Add("route", "rte");
			res.Add("rvr", "riv");
			res.Add("shoal", "shl");
			res.Add("shoals", "shls");
			res.Add("shoar", "shr");
			res.Add("shoars", "shrs");
			res.Add("shore", "shr");
			res.Add("shores", "shrs");
			res.Add("skyway", "skwy");
			res.Add("spng", "spg");
			res.Add("spngs", "spgs");
			res.Add("spring", "spg");
			res.Add("springs", "spgs");
			res.Add("sprng", "spg");
			res.Add("sprngs", "spgs");
			res.Add("spurs", "spur");
			res.Add("sqr", "sq");
			res.Add("sqre", "sq");
			res.Add("sqrs", "sqs");
			res.Add("squ", "sq");
			res.Add("square", "sq");
			res.Add("squares", "sqs");
			res.Add("station", "sta");
			res.Add("statn", "sta");
			res.Add("stn", "sta");
			res.Add("str", "st");
			res.Add("strav", "stra");
			res.Add("strave", "stra");
			res.Add("straven", "stra");
			res.Add("stravenue", "stra");
			res.Add("stravn", "stra");
			res.Add("stream", "strm");
			res.Add("street", "st");
			res.Add("streets", "sts");
			res.Add("streme", "strm");
			res.Add("strt", "st");
			res.Add("strvn", "stra");
			res.Add("strvnue", "stra");
			res.Add("sumit", "smt");
			res.Add("sumitt", "smt");
			res.Add("summit", "smt");
			res.Add("terr", "ter");
			res.Add("terrace", "ter");
			res.Add("throughway", "trwy");
			res.Add("tpk", "tpke");
			res.Add("tr", "trl");
			res.Add("trace", "trce");
			res.Add("traces", "trce");
			res.Add("track", "trak");
			res.Add("tracks", "trak");
			res.Add("trafficway", "trfy");
			res.Add("trail", "trl");
			res.Add("trails", "trl");
			res.Add("trk", "trak");
			res.Add("trks", "trak");
			res.Add("trls", "trl");
			res.Add("trnpk", "tpke");
			res.Add("trpk", "tpke");
			res.Add("tunel", "tunl");
			res.Add("tunls", "tunl");
			res.Add("tunnel", "tunl");
			res.Add("tunnels", "tunl");
			res.Add("tunnl", "tunl");
			res.Add("turnpike", "tpke");
			res.Add("turnpk", "tpke");
			res.Add("underpass", "upas");
			res.Add("union", "un");
			res.Add("unions", "uns");
			res.Add("valley", "vly");
			res.Add("valleys", "vlys");
			res.Add("vally", "vly");
			res.Add("vdct", "via");
			res.Add("viadct", "via");
			res.Add("viaduct", "via");
			res.Add("view", "vw");
			res.Add("views", "vws");
			res.Add("vill", "vlg");
			res.Add("villag", "vlg");
			res.Add("village", "vlg");
			res.Add("villages", "vlgs");
			res.Add("ville", "vl");
			res.Add("villg", "vlg");
			res.Add("villiage", "vlg");
			res.Add("vist", "vis");
			res.Add("vista", "vis");
			res.Add("vlly", "vly");
			res.Add("vst", "vis");
			res.Add("vsta", "vis");
			res.Add("walks", "walk");
			res.Add("well", "wl");
			res.Add("wells", "wls");
			res.Add("wy", "way");

			return res;
		}

		/// <summary>
		/// Map allowed unit type codes to their canonical representation
		/// </summary>
		/// <returns>
		/// Hashtable: unit-type => canonical short name
		/// </returns>
		private Dictionary<string, string> _initUnitType()
		{
			Dictionary<string, string> res = new Dictionary<string, string>(41, StringComparer.InvariantCultureIgnoreCase);

			res.Add("APT", "APT");
			res.Add("APT #", "APT");
			res.Add("APARTMENT", "APT");
			res.Add("BSMT", "BSMT");
			res.Add("BASEMENT", "BSMT");
			res.Add("BLDG", "BLDG");
			res.Add("BUILDING", "BLDG");
			res.Add("DEPT", "DEPT");
			res.Add("DEPARTMENT", "DEPT");
			res.Add("FL", "FL");
			res.Add("FLOOR", "FL");
			res.Add("FRNT", "FRNT");
			res.Add("FRONT", "FRNT");
			res.Add("HNGR", "HNGR");
			res.Add("HANGAR", "HNGR");
			res.Add("LBBY", "LBBY");
			res.Add("LOBBY", "LBBY");
			res.Add("LOT", "LOT");
			res.Add("LOWR", "LOWR");
			res.Add("LOWER", "LOWR");
			res.Add("OFC", "OFC");
			res.Add("OFFICE", "OFC");
			res.Add("PH", "PH");
			res.Add("PENTHOUSE", "PH");
			res.Add("PIER", "PIER");
			res.Add("REAR", "REAR");
			res.Add("RM", "RM");
			res.Add("ROOM", "RM");
			res.Add("SIDE", "SIDE");
			res.Add("SLIP", "SLIP");
			res.Add("SPC", "SPC");
			res.Add("SPACE", "SPC");
			res.Add("STOP", "STOP");
			res.Add("STE", "STE");
			res.Add("SUITE", "STE");
			res.Add("TRLR", "TRLR");
			res.Add("TRAILER", "TRLR");
			res.Add("#", "#");
			res.Add("UNIT", "UNIT");
			res.Add("UPPR", "UPPR");
			res.Add("UPPER", "UPPR");

			return res;
		}

		#endregion

		#region Private Regex Methods

		/// <summary>
		/// Build and precompile the regular expression
		/// </summary>
		private void _buildRegexes()
		{
			this.rgxType = this._typeRegex();
			// modified to allow alpha-numeric house numbers with at least one numeric digit to ensure a proper parse with "bad" house numbers
			// to keep from improperly parsing addresses with missing house numbers but numeric street names, i.e., 101ST Street,
			// exclude it from matching.
			// ORIG: this.rgxNumber = new Regex(@"^\W*(?<number>\d+-?\d*)\W*(:?\d+\/\d+\W*)?");
			this.rgxNumber = new Regex(@"(?!^\d+(ST|ND|RD|TH))(^(?<number>\w*\d+-?\w*)(:?\d+\/\d+\W*)?)", RegexOptions.IgnoreCase);
			this.rgxDirCode = this._dircodeRegex();
			this.rgxDirect = this._directRegex();
			this.rgxUnit = new Regex(REGEX_UNIT, RegexOptions.IgnoreCase);
			this.rgxStreet = this._streetRegex();
			// removed dot matching since they are pre-scrubbed out
			this.rgxPOBox = new Regex(REGEX_POBOX, RegexOptions.IgnoreCase);
		}
		/// <summary>
		/// use the StreetTypeList Hashtable to produce a list of alternatives
		/// for the street type recognizer regex
		/// </summary>
		/// <returns>string of alternatives sorted by descending length</returns>
		private string _typeRegex()
		{
			string[] fields = this._hashToStrings(this.StreetTypeList, 1);
			string types = string.Join("|", fields);
			return types;
		}
		private string _dircodeRegex()
		{
			string[] fields = this._hashToStrings(this.DirectionCode, 1);
			string types = string.Join("|", fields);
			return types;
		}
		private string _directRegex()
		{
			string res = "";
			string[] fields = this._hashToStrings(this.Directional, 3); // keys AND values
			ArrayList withDots = new ArrayList();
			foreach ( string f in fields )
			{
				withDots.Add(f);
				if ( f.Length < 3 )
				{
					string dotted = f[0] + @"\.";
					if ( f.Length == 2 )
					{
						dotted += f[1];
					}
					withDots.Add(dotted);
				}
			};
			withDots.Sort(new LengthComparer());
			string[] dot = (string[])withDots.ToArray(typeof(string));
			res = string.Join("|", dot);
			return res;
		}
		private string _streetRegex()
		{
			string res;
			string direct = this.rgxDirect;
			string type = this.rgxType;
			//
			// special case: street name = direction, eg North street
			//
			res = @"^(?:";
			res += @"(?:(?<street>" + direct + @"))\W+";
			res += @"(?<type>" + type + @")\b";
			//
			// street name with optional direction prefix or direction suffix
			//
			// res += "|";
			res += @"(?:(?<prefix>" + direct + @")\W+)?";
			res += @"(?:(?<street>[^,]+)";
			res += @"(?:[^\w,]+(?<type>" + type + @"))\b";
			res += @"(?:[^\w,]+(?<suffix>" + direct + @"))\b";
			res += @")?";
			res += ")";
			return res;
		}
		/// <summary>
		/// Take a hash with string keys and values and return an array
		/// of keys, values or both
		/// </summary>
		/// <param name="h">
		/// Source hashtable
		/// </param>
		/// <param name="mode">
		/// 1 - keys, 2 - values, 3 - both
		/// </param>
		/// <returns></returns>
		private string[] _hashToStrings(Dictionary<string, int> h, int mode)
		{
			ArrayList al = new ArrayList();
			foreach ( string k in h.Keys )
			{
				if ( ( mode & 1 ) != 0 )
				{
					if ( al.Contains(k) == false )
						al.Add(k);
				}
				if ( ( mode & 2 ) != 0 )
				{
					if ( al.Contains(h[k]) == false )
						al.Add(h[k]);
				}
			}
			al.Sort(new LengthComparer());
			return (string[])al.ToArray(typeof(string));
		}
		private string[] _hashToStrings(Dictionary<string, string> h, int mode)
		{
			ArrayList al = new ArrayList();
			foreach ( string k in h.Keys )
			{
				if ( ( mode & 1 ) != 0 )
				{
					if ( al.Contains(k) == false )
						al.Add(k);
				}
				if ( ( mode & 2 ) != 0 )
				{
					if ( al.Contains(h[k]) == false )
						al.Add(h[k]);
				}
			}
			al.Sort(new LengthComparer());
			return (string[])al.ToArray(typeof(string));
		}

		#endregion

		#region Private Parsing Methods

		/// <summary>
		/// Extract the street & number from an address string if it's a PO/RR Box 
		/// and delete the corresponding text
		/// </summary>
		/// <param name="loc"></param>
		/// <returns>true on success, false on failure</returns>
		private bool _extractPOBox(ref string loc)
		{
			Match m = this.rgxPOBox.Match(loc);
			bool retval = m.Success;

			if ( retval )
			{
				this.locRes.HouseNumber = m.Groups["number"].Value.Trim();
				this.locRes.StreetName = m.Groups["street"].Value.Trim();
				this.locRes.IsPOBox = true;

				loc = this.rgxPOBox.Replace(loc, "").Trim();
			}
			return retval;
		}

		// check to see if we have a leading apt #
		// "APT H 968 MOOTY BRIDGE RD"
		private bool _extractLeadingUnit(ref string loc)
		{
			loc += " ";
			string tmpRegex = "^" + REGEX_UNIT.Remove(8, 3) + @"\s";
			Match m = Regex.Match(loc, tmpRegex, RegexOptions.IgnoreCase);
			if ( m.Success )
			{
				this.locRes.UnitNumber = m.Groups["number"].Value.Trim();
				this.locRes.UnitType = m.Groups["type"].Value.Trim();
				loc = Regex.Replace(loc, tmpRegex, "", RegexOptions.IgnoreCase).Trim();
			}
			else
			{
				tmpRegex = @"^(?<number>[\d-]+)";
				m = Regex.Match(loc, tmpRegex, RegexOptions.IgnoreCase);
				if ( m.Success )
				{
					this.locRes.UnitNumber = m.Groups["number"].Value.Trim();
					loc = Regex.Replace(loc, tmpRegex, "", RegexOptions.IgnoreCase).Trim();
				}
			}
			return m.Success;
		}

		/// <summary>
		/// Extract the number from an address string and delete the corresponding text
		/// </summary>
		/// <param name="loc"></param>
		/// <returns>true on success, false on failure</returns>
		private bool _extractNumber(ref string loc)
		{
			Match m = this.rgxNumber.Match(loc);
			bool retval = m.Success;

			if ( retval )
			{
				this.locRes.HouseNumber = m.Groups["number"].Value.Trim();
				loc = this.rgxNumber.Replace(loc, "").Trim();
			}
			else
			{
				// check to see if we have a leading apt #
				retval = _extractLeadingUnit(ref loc);

				if ( retval )
				{
					// go after the house number again
					m = this.rgxNumber.Match(loc);
					retval = m.Success;

					if ( retval )
					{
						this.locRes.HouseNumber = m.Groups["number"].Value.Trim();
						loc = this.rgxNumber.Replace(loc, "").Trim();
					}
				}
			}

			return retval;
		}

		/// <summary>
		/// Parse street name and unit number un the input line
		/// remove recognized part from input
		/// </summary>
		/// <param name="loc">
		/// Input string
		/// </param>
		/// <returns>
		/// Success indicator
		/// </returns>
		private bool _extractStreet(ref string loc, string loc2)
		{
			//
			// extract street part for parsing:
			// from start of line to first ','
			//
			Regex rgx = new Regex(@"^([^,]+)");
			Match s = rgx.Match(loc);
			string part = s.Groups[1].Value;
			//
			// from now on we use the part string
			//
			loc = rgx.Replace(loc, "");
			//
			// the unit part (apartment etc.), save and remove
			//
			rgx = this.rgxUnit;
			s = rgx.Match(part);
			if ( s.Success )
			{
				locRes.UnitType = s.Groups["type"].Value.Trim();
				locRes.UnitNumber = s.Groups["number"].Value.Trim();
				// if there's anything out past this point drop it like a hot potato
				int extendString = s.Index + s.Length;
				if ( part.Length > extendString + 1 )
				{
					part = part.Substring(0, extendString);
				}

				// now we can remove the match
				part = rgx.Replace(part, "");

				// if we're missing the number but have something in loc2 ...
				if ( locRes.UnitNumber.Length == 0 && loc2.Length > 0 )
				{
					_extractLeadingUnit(ref loc2);
				}
			}
			else if ( loc2.Length > 0 )
			{
				// maybe its in the addy2 field; since the number parser catches this, use it
				_extractLeadingUnit(ref loc2);
			}

			//
			// street: [direction]
			//
			string rgdir = @"(?<prefix>" + this.rgxDirect + @")";
			string rgtyp = @"\W+(?<type>" + this.rgxType + @")\W*";
			rgx = new Regex(@"^" + rgdir + @"\W+", RegexOptions.IgnoreCase);
			Match m = rgx.Match(part);
			if ( m.Success )
			{
				part = rgx.Replace(part, "");     // remove match
				//
				// a direction code up front, check for exceptional case
				// street name = some valid direction.
				// Don't capture highways
				//
				if ( !Regex.IsMatch(part, REGEX_HIGHWAY, RegexOptions.IgnoreCase) )
				{
					rgx = new Regex(@"^(?<type>" + this.rgxType + @")\b", RegexOptions.IgnoreCase);
					Match t = rgx.Match(part);
					if ( t.Success )
					{
						string retest = rgx.Replace(part, "").Trim();
						// make sure we don't capture the wrong street type
						// example: "4 W Grove Ave"
						if ( !Regex.IsMatch(retest, @"\b(" + this.rgxType + @")\b", RegexOptions.IgnoreCase) )
						{
							this.locRes.StreetName = m.Groups["prefix"].Value.Trim();
							this.locRes.StreetType = t.Groups["type"].Value.Trim();
							return true;
						}
					}
				}
				this.locRes.PreDirection = m.Groups["prefix"].Value.Trim();

			}
			//
			// prefix (if present) is now removed

			//
			// now check for suffix (direction word at end-of-line)
			//
			rgx = new Regex(@"\W+" + rgdir + @"\W*$", RegexOptions.IgnoreCase);
			m = rgx.Match(part);
			if ( m.Success )
			{
				this.locRes.PostDirection = m.Groups["prefix"].Value.Trim();
				part = rgx.Replace(part, "");
			}
			///
			/// usually the street type is at the end of the street string
			/// if this is the case, remove the type designation from name
			/// 
			rgx = new Regex(rgtyp + "$", RegexOptions.IgnoreCase);
			m = rgx.Match(part);
			if ( m.Success )
			{
				this.locRes.StreetType = m.Groups["type"].Value.Trim();
				part = rgx.Replace(part, "");
				//
				// check for suffix again, since sometimes its before the type (direction word at end-of-line)
				//
				rgx = new Regex(@"\W+" + rgdir + @"\W*$", RegexOptions.IgnoreCase);
				m = rgx.Match(part);
				if ( m.Success )
				{
					this.locRes.PostDirection = m.Groups["prefix"].Value.Trim();
					part = rgx.Replace(part, "");
				}
			}
			else
			{
				///
				/// no street type at end of line, look for a street type in the middle
				/// 
				rgx = new Regex(rgtyp + @"\W+", RegexOptions.IgnoreCase);
				m = rgx.Match(part);
				if ( m.Success )
				{
					this.locRes.StreetType = m.Groups["type"].Value.Trim();
					if ( !Regex.IsMatch(part, REGEX_HIGHWAY, RegexOptions.IgnoreCase) )
					{
						// if there's anything out past this point drop it like a hot potato
						int extendString = m.Index + m.Length;
						if ( part.Length >= extendString + 1 )
						{
							part = part.Substring(0, extendString);
						}

						// now we can remove the match
						part = rgx.Replace(part, " ");

						//
						// check for suffix again, since sometimes its before the type (direction word at end-of-line)
						//
						rgx = new Regex(@"\W+" + rgdir + @"\W*$", RegexOptions.IgnoreCase);
						m = rgx.Match(part);
						if ( m.Success )
						{
							this.locRes.PostDirection = m.Groups["prefix"].Value.Trim();
							part = rgx.Replace(part, "");
						}
					}
				}

			}

			this.locRes.StreetName = part.Trim();
			this.locRes.StreetType = this.locRes.StreetType.Length == 0 ? m.Groups["type"].Value.Trim() : this.locRes.StreetType;

			// fixes examples such as "F LONDONDERRY ROAD"
			string[] tmp = this.locRes.StreetName.Split(' ');
			if ( tmp.Length > 1 && tmp[0].Length == 1 && !char.IsDigit(tmp[0], 0) )
			{
				this.locRes.StreetName = string.Join(" ", tmp, 1, tmp.Length - 1);
			}

			return ( this.locRes.StreetName.Length > 0 );
		}
		private string _normalizeDirection(string src)
		{
			Regex dots = new Regex(@"\.");
			string res = dots.Replace(src, "");
			if ( this.Directional.ContainsKey(res.ToLower()) )
			{
				res = (string)this.Directional[res.ToLower()];
			}
			return res;
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Parses a street address into an address specifier, returning an object
		/// with Valid property set to FALSE if the address cannot be parsed. 
		/// </summary>
		/// <param name="Address1">
		/// Address string
		/// Address Syntax: <number>[<fraction>]<street>[<unit>]
		/// the extracted data is stored in thr locRes ParsedAddress object
		/// </param>
		/// <returns>
		/// ParsedAddress object
		/// </returns>
		public ParsedAddress Parse(string Address1)
		{
			return this.Parse(Address1, "");
		}
		public ParsedAddress Parse(string Address1, string Address2)
		{
			bool isValidNumber = false;
			bool isValidStreet = false;

			/// the extracted data is stored in thr locRes ParsedAddress object
			this.locRes = new ParsedAddress();       // clear result space

			// preserve the original address data
			this.locRes.RawAddress1 = Address1;
			this.locRes.RawAddress2 = Address2;

			// strip out leading "MR/MR-"; this screws up separating number & street
			Address1 = Regex.Replace(Address1, @"^MR-?|\.", "", RegexOptions.IgnoreCase).Trim();

			// combine both address lines since the parser expects this
			if ( Address2.Length > 0 )
			{
				Address1 += " " + Address2;
				Address2 = "";
			}
			Address1 = Address1.Trim();

			// first attempt to identify and parse Post Office(PO)/Rural Routes(RR) Boxes
			if ( !this._extractPOBox(ref Address1) )
			{
				// get <number>
				isValidNumber = this._extractNumber(ref Address1);

				if ( isValidNumber )
				{
					// remaining part: <street>...
					isValidStreet = this._extractStreet(ref Address1, Address2);
				}
				else if ( this.locRes.RawAddress2.Length > 0 )
				{
					// swap and try again...
					Address1 = this.locRes.RawAddress2 + " " + this.locRes.RawAddress1;
					Address1 = Address1.Trim();

					if ( !this._extractPOBox(ref Address1) )
					{
						// get <number>
						isValidNumber = this._extractNumber(ref Address1);

						// remaining part: <street>...
						isValidStreet = this._extractStreet(ref Address1, Address2);
					}
				}
			}

			if ( this.locRes.IsPOBox )
			{
				// flag as correctly parsed PO Box/RR
				isValidNumber = ( this.locRes.StreetName.Length > 0 );
				isValidStreet = ( this.locRes.HouseNumber.Length > 0 );
			}

			this.locRes.Valid = ( isValidNumber && isValidStreet );

			if ( mDebugBadAddresses && !this.locRes.Valid ) writeToFile(locRes);

			return this.NormalizeAddress(locRes);
		}

		/// <summary>
		/// Normalize ParsedAddress object: 
		/// - canonical direction codes
		/// - canonical state abbreviations
		/// </summary>
		/// <param name="src">
		/// Loaction object
		/// </param>
		/// <returns>
		/// Normalized object
		/// </returns>
		public ParsedAddress NormalizeAddress(ParsedAddress src)
		{
			ParsedAddress res = src;
			///
			/// normalize street type
			/// 
			string tp = src.StreetType.ToLower();
			string val = tp;
			if ( this.StreetType.ContainsKey(tp) )
			{
				val = (string)this.StreetType[tp];
			}
			///
			/// normalize directions (preifx, suffix)
			/// 
			res.PreDirection = this._normalizeDirection(src.PreDirection);
			res.PostDirection = this._normalizeDirection(src.PostDirection);

			//
			// normalize unit type
			//
			if ( this.UnitType.ContainsKey(res.UnitType) )
			{
				res.UnitType = this.UnitType[res.UnitType];
			}

			//
			// normalize PO/RR/HC Box
			//
			const string RR_CHECK_PATTERN = @"^R\s?(R|T)\s\d+\s(BOX|B)";
			const string RR_REPLACE_PATTERN = @"^R\s?(R|T)";
			const string RR_REPLACE_VALUE = "RR";
			const string HC_CHECK_PATTERN = @"^H\s?C(\s?R|\s?1)?(\s?BOX)?";
			const string HC_REPLACE_PATTERN = @"^H\s?C(\s?R|\s?1)?(\s?BOX)?";
			const string HC_REPLACE_VALUE = "HC";
			const string PO_PATTERN = @"^((P\s?(O|0)\s?)?(BOX))|POB|P0B";
			const string PO_REPLACE = "PO BOX";
			// check order is important
			if ( Regex.IsMatch(res.StreetName, RR_CHECK_PATTERN, RegexOptions.IgnoreCase) )
			{
				res.StreetName = Regex.Replace(res.StreetName, RR_REPLACE_PATTERN, RR_REPLACE_VALUE, RegexOptions.IgnoreCase);
				// also remove box
				//res.StreetName = Regex.Replace(res.StreetName, @"\sBOX", "", RegexOptions.IgnoreCase);
			}
			else if ( Regex.IsMatch(res.StreetName, HC_CHECK_PATTERN, RegexOptions.IgnoreCase) )
			{
				res.StreetName = Regex.Replace(res.StreetName, HC_REPLACE_PATTERN, HC_REPLACE_VALUE, RegexOptions.IgnoreCase);
				// also remove box
				res.StreetName = Regex.Replace(res.StreetName, @"\sBOX", "", RegexOptions.IgnoreCase);
			}
			else if ( Regex.IsMatch(res.StreetName, PO_PATTERN, RegexOptions.IgnoreCase) )
			{
				res.StreetName = Regex.Replace(res.StreetName, PO_PATTERN, PO_REPLACE, RegexOptions.IgnoreCase);
			}

			Type type = res.GetType();
			FieldInfo[] props = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
			foreach ( FieldInfo prop in props )
			{
				if ( prop.FieldType == typeof(string) )
				{
					string uVal = prop.GetValue(res).ToString().Trim().ToUpper();
					prop.SetValue(res, uVal);
				}
			}

			res.NormalizedAddress = getNormalizedAddress();

			return res;
		}

		#endregion

		#region Private Support Routines

		private string getNormalizedAddress()
		{
			string retval = this.locRes.RawAddress1;

			if ( this.locRes.Valid )
			{
				if ( !this.locRes.IsPOBox )
				{
					if ( !Regex.IsMatch(this.locRes.StreetName, REGEX_HIGHWAY, RegexOptions.IgnoreCase) )
					{
						// normal formatting
						retval = string.Format("{0} {1} {2}",
							this.locRes.PreDirection,
							this.locRes.StreetName,
							this.locRes.StreetType).Trim();
						retval = string.Format("{0} {1}", retval, this.locRes.PostDirection).Trim();
					}
					else
					{
						// highway formatting
						retval = string.Format("{0} {1} {2}",
							this.locRes.PreDirection,
							this.locRes.StreetName,
							this.locRes.PostDirection).Trim();
					}

					// apt formatting
					string apt = string.Format("{0} {1}", this.locRes.UnitType, this.locRes.UnitNumber).Trim();

					// finish...
					retval = string.Format("{0} {1} {2}", this.locRes.HouseNumber, retval, apt);
				}
				else
				{
					//PO Box formatting
					retval = string.Format("{0} {1}", this.locRes.StreetName, this.locRes.HouseNumber).Trim();
				}
			}

			return retval;
		}

		private bool writeToFile(ParsedAddress loc)
		{
			bool retval = false;

			try
			{
				string filename = Assembly.GetExecutingAssembly().Location;
				FileInfo fi = new FileInfo(filename);
				filename = fi.Directory + @"\AddressParser.ParsedAddress.bad.xml";
				fi = new FileInfo(filename);
				if ( !fi.Exists )
				{
					using ( TextWriter tx = fi.CreateText() )
					{
						tx.Write("<xml></xml>");
						tx.Flush();
						tx.Close();
					}
				}

				XmlDocument xml = new XmlDocument();
				xml.Load(fi.FullName);

				XmlDocumentFragment frag = xml.CreateDocumentFragment();
				frag.InnerXml = loc.ToString();

				xml.DocumentElement.AppendChild(frag);

				xml.Save(fi.FullName);

				retval = true;
			}
			catch
			{
				retval = false;
			}

			return retval;
		}

		#endregion

		#region Static Address Parsing Routines
		// support methods for supporting legacy vendor specific formatting

		public static bool IsNumericStreetName(string StreetName)
		{
			// get name or leading number (assumed street name is some variation of "4th")
			return Regex.IsMatch(StreetName, @"^\d+(/\d*)?(ST|ND|RD|TH)?", RegexOptions.IgnoreCase);
		}

		public static string ExtractNumericStreetName(string StreetName)
		{
			// assumed that directionals have been removed in street name,
			// i.e., a properly parsed out street name
			string retval = StreetName;

			if ( ParseAddress.IsNumericStreetName(retval) )
			{
				// strip off any attached suffix
				retval = Regex.Replace(retval, @"ST|ND|RD|TH", "", RegexOptions.IgnoreCase).Trim();
			}

			return retval;
		}

		public static bool IsNewYorkCity(string City)
		{
			string checkCity = Regex.Replace(City, "[^A-Za-z]", "");

			return Regex.IsMatch(checkCity, "NYC|NEWYORK(CITY)?", RegexOptions.IgnoreCase);
		}

		public static void ParseZipCode(string inZipCode, ref string outZipCode, ref string outZipCodePlus4)
		{
			outZipCode = "";
			outZipCodePlus4 = "";

			// strip out any non-numeric character
			inZipCode = Regex.Replace(inZipCode, @"\D", "");

			if ( inZipCode.Length <= 5 )
			{
				outZipCode = inZipCode;
			}
			else
			{
				outZipCode = inZipCode.Substring(0, 5);
				outZipCodePlus4 = inZipCode.Substring(5);
			}
		}

		#endregion

		/// <summary>
		/// ParsedAddress class, actually a structure
		/// </summary>
		public sealed class ParsedAddress
		{
			public string RawAddress1 = "";
			public string RawAddress2 = "";
			public string NormalizedAddress = "";
			public string HouseNumber = "";
			public string PreDirection = "";
			public string StreetName = "";
			// used to store just the numeric portion of the streetname, i.e., 8 instead of 8TH
			public string NumericStreetName = ""; 
			public string PostDirection = "";
			public string StreetType = "";
			public string UnitType = "";
			public string UnitNumber = "";
			public bool IsPOBox = false;
			public bool Valid = false;

			public override string ToString()
			{
				Type type = this.GetType();

				StringBuilder retval = new StringBuilder();
				retval.AppendFormat("<{0}>", type.Name);

				FieldInfo[] props = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);

				foreach ( FieldInfo prop in props )
				{
					string val = prop.GetValue(this).ToString();

					if ( val.Length > 0 )
					{
						retval.AppendFormat("<{0}>{1}</{2}>",
							prop.Name,
							prop.GetValue(this),
							prop.Name);
					}
					else
					{
						retval.AppendFormat("<{0} />", prop.Name);
					}
				}

				retval.AppendFormat("</{0}>", type.Name);
				return retval.ToString();
			}
		}

		public sealed class LengthComparer : IComparer
		{
			public int Compare(object a, object b)
			{
				string strA = a.ToString().ToLower();
				string strB = b.ToString().ToLower();
				int lena = strA.Length;
				int lenb = strB.Length;
				if ( lenb != lena ) return lenb - lena;
				return strA.CompareTo(strB);
			}

		}
	}
}
