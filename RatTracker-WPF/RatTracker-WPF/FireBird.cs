using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirebirdSql.Data.FirebirdClient;
using log4net;
using RatTracker_WPF.Models.Edsm;

using static System.Windows.MessageBox;


namespace RatTracker_WPF
{
	public class FireBird
	{
		private static readonly ILog Logger =
			LogManager.GetLogger(System.Reflection.Assembly.GetCallingAssembly().GetName().Name);

		private FbConnection con;
		public void InitDB()
		{
			Logger.Debug("Starting FbConnection");
			FbConnectionStringBuilder builder = new FbConnectionStringBuilder();
			builder.UserID = "SYSDBA";
			builder.Database = "EDDB.FDB";
			builder.ServerType = FbServerType.Embedded;
			//string rtPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			string rtPath=@"G:\";
			if (File.Exists(rtPath+@"EDDB.FDB"))
			{
				Logger.Debug("EDDB database file found.");
			}
			else
			{
				Logger.Debug("Creating Database!");
				FbConnection.CreateDatabase(builder.ConnectionString);
			}

			con = new FbConnection(
						"User=SYSDBA;Password=masterkey;Database="+rtPath+@"EDDB.FDB;Dialect=3;Charset=UTF8;ServerType=1;");
				try
				{
					con.Open();
					Show("I have connected to the DB!");
				}
				catch (Exception ex)
				{
					Show(ex.ToString());
				}
			}

		public void CreateTables()
		{
			Logger.Debug("Starting database table creation.");
			using (FbCommand createDomain = con.CreateCommand())
			{
				createDomain.CommandText = "CREATE DOMAIN BOOLEAN AS SMALLINT CHECK (value is null or value in (0, 1))";
				createDomain.ExecuteNonQuery();
			}
			using (FbCommand createTable = con.CreateCommand())
			{
				//{"id":17,"name":"10 Ursae Majoris","x":0.03125,"y":34.90625,"z":-39.09375,"faction":"NoneNone","population":0,"government":"None",
				//"allegiance":"None","state":"None","security":"Low","primary_economy":"None","power":"None","power_state":null,"needs_permit":0,"updated_at":1455744292,"simbad_ref":"10 Ursae Majoris"}
				createTable.CommandText =
						"CREATE TABLE eddb_systems (id int, name varchar(150), x float, y float, z float, faction varchar(150), population bigint, goverment varchar(130), allegiance varchar(130), state varchar(130), "+
						"security varchar(150), primary_economy varchar(130), power varchar(130), power_state varchar(130), needs_permit boolean, updated_at bigint, simbad_ref varchar(150), lowercase_name varchar(150))";
				createTable.ExecuteNonQuery();
			}
			using (FbCommand createTable = con.CreateCommand())
			{
				//{"id":39,"name":"Porta","system_id":189,"max_landing_pad_size":"L","distance_to_star":995,"faction":"39 Tauri Interstellar","government":"Corporate","allegiance":"Federation","state":"Retreat",
				//"type_id":8,"type":"Orbis Starport","has_blackmarket":0,"has_market":1,"has_refuel":1,"has_repair":1,"has_rearm":1,"has_outfitting":1,"has_shipyard":1,"has_docking":1,"has_commodities":1,
				//"import_commodities":["Beer","Grain","Silver"],"export_commodities":["Hydrogen Fuel","Biowaste","Limpet"],
				//"prohibited_commodities":["Narcotics","Tobacco","Combat Stabilisers","Imperial Slaves","Slaves","Personal Weapons","Battle Weapons","Toxic Waste","Wuthielo Ku Froth","Bootleg Liquor","Landmines","Unknown Probe"],
				//"economies":["Service"],"updated_at":1467734848,"shipyard_updated_at":1473035640,"outfitting_updated_at":1473035635,"market_updated_at":1473035634,"is_planetary":0,
				//"selling_ships":["Adder","Asp Explorer","Cobra Mk. III","Diamondback Scout","Hauler","Python","Sidewinder Mk. I","Federal Corvette","Cobra MK IV"],
				//"selling_modules":[738,739,740,741,742,748,749,750,751,752,753,754,755,756,757,763,764,765,766,767,778,779,780,781,782,808,809,810,811,812,828,835,853,854,857,858,859,862,863,864,865,866,868,869,871,876,878,879,
				//880,882,883,884,885,886,887,889,890,895,900,919,920,925,930,933,935,999,1000,1005,1009,1010,1029,1033,1034,1035,1066,1067,1068,1069,1070,1071,1073,1074,1075,1106,1107,1109,1110,1111,1112,1113,1116,1117,1118,1119,
				//1120,1121,1122,1123,1124,1125,1141,1142,1143,1144,1146,1147,1148,1149,1150,1152,1153,1155,1158,1160,1162,1163,1164,1165,1181,1182,1183,1184,1186,1188,1189,1191,1199,1200,1201,1202,1203,1204,1205,1206,1207,1208,1209,
				//1210,1211,1213,1214,1215,1216,1220,1221,1223,1224,1225,1226,1228,1231,1232,1233,1234,1236,1237,1242,1243,1244,1245,1246,1248,1255,1256,1260,1261,1262,1263,1264,1268,1269,1270,1271,1272,1276,1277,1278,1279,1284,1285,
				//1286,1288,1289,1292,1293,1296,1297,1300,1301,1304,1305,1306,1308,1309,1312,1316,1317,1321,1327,1334,1335,1340,1341,1343,1345,1346,1349,1351,1356,1357,1358,1359,1363,1364,1367,1383,1384,1385,1386,1387,1388,1396,1397,
				//1401,1405,1409,1453,1498,1499,1500,1501,1502,1518,1519,1520,1521,1522,1523,1524,1525,1526,1527,1528,1529,1530,1531,1532,1536,1537,1543,1545,1546,1549]}

				createTable.CommandText =
					"CREATE TABLE eddb_stations (id bigint, name varchar(150), system_id bigint, max_landing_pad_size varchar(5), distance_to_star bigint, faction varchar(150), goverment varchar(120), allegiance varchar(130), " +
					"state varchar(120), type_id int, type varchar(130), has_blackmarket boolean, has_market boolean, has_refuel boolean, has_repair boolean, has_rearm boolean, has_outfitting boolean, has_shipyard boolean, has_docking boolean, " +
					"has_commodities boolean, prohibited_commodities varchar(10000), economies varchar(10000), updated_at bigint, shipyard_updated_at bigint, outfitting_updated_at bigint, market_updated_at bigint, is_planetary boolean, " +
					"selling_ships varchar(20000), selling_modules varchar(20000), lowercase_name varchar(150))";
				createTable.ExecuteNonQuery();
			}
			Logger.Debug("Completed database table creation.");

		}

		public async Task AddStation(int id, string name, int system_id, string max_landing_pad_size, int? distance_to_star,
			string faction, string government, string allegiance, string state, int? type_id, string type, bool? has_blackmarket,
			bool? has_market, bool? has_refuel, bool? has_repair, bool? has_rearm, bool? has_outfitting, bool? has_shipyard, bool? has_docking,
			bool? has_commodities, List<string> import_commodities, List<string> export_commodities, List<string> prohibited_commodities,
			List<string> economies, int? updated_at, int? shipyard_updated_at, int? outfitting_updated_at, int? market_updated_at,
			bool is_planetary, List<string> selling_ships, List<int> selling_modules)
		{
			using (FbCommand insertStation = con.CreateCommand())
			{
				insertStation.CommandText =
					"INSERT INTO eddb_stations values (@id, @name, @system_id, @max_landing_pad_size, @distance_to_star, @faction, @government, @allegiance, @state, @type_id, @type, @has_blackmarket, @has_market, " +
					"@has_refuel, @has_repair, @has_rearm, @has_outfitting, @has_shipyard, @has_docking, @has_commodities, @prohibited_commodities, @economies, @updated_at, @shipyard_updated_at, @outfitting_updated_at, @market_updated_at, " +
					"@is_planetary, @selling_ships, @selling_modules, @lowercase_name)";
				insertStation.Parameters.Clear();
				insertStation.Parameters.AddWithValue("@id", id);
				insertStation.Parameters.AddWithValue("@name", name);
				insertStation.Parameters.AddWithValue("@system_id", system_id);
				insertStation.Parameters.AddWithValue("@max_landing_pad_size", max_landing_pad_size);
				insertStation.Parameters.AddWithValue("@distance_to_star", distance_to_star);
				insertStation.Parameters.AddWithValue("@faction", faction);
				insertStation.Parameters.AddWithValue("@government", government);
				insertStation.Parameters.AddWithValue("@allegiance", allegiance);
				insertStation.Parameters.AddWithValue("@state", state);
				insertStation.Parameters.AddWithValue("@type_id", type_id);
				insertStation.Parameters.AddWithValue("@type", type);
				insertStation.Parameters.AddWithValue("@has_blackmarket", Convert.ToInt16(has_blackmarket));
				insertStation.Parameters.AddWithValue("@has_market", Convert.ToInt16(has_market));
				insertStation.Parameters.AddWithValue("@has_refuel", Convert.ToInt16(has_refuel));
				insertStation.Parameters.AddWithValue("@has_rearm", Convert.ToInt16(has_rearm));
				insertStation.Parameters.AddWithValue("@has_repair", Convert.ToInt16(has_repair));
				insertStation.Parameters.AddWithValue("@has_outfitting", Convert.ToInt16(has_outfitting));
				insertStation.Parameters.AddWithValue("@has_shipyard", Convert.ToInt16(has_shipyard));
				insertStation.Parameters.AddWithValue("@has_docking", Convert.ToInt16(has_docking));
				insertStation.Parameters.AddWithValue("@has_commodities", Convert.ToInt16(has_commodities));
				insertStation.Parameters.AddWithValue("@prohibited_commodities", string.Join(", ", prohibited_commodities.ToArray()));
				insertStation.Parameters.AddWithValue("@economies", string.Join(", ", economies.ToArray()));
				insertStation.Parameters.AddWithValue("@updated_at", updated_at);
				insertStation.Parameters.AddWithValue("@shipyard_updated_at", shipyard_updated_at);
				insertStation.Parameters.AddWithValue("@outfitting_updated_at", outfitting_updated_at);
				insertStation.Parameters.AddWithValue("@market_updated_at", market_updated_at);
				insertStation.Parameters.AddWithValue("@is_planetary", Convert.ToInt16(is_planetary));
				insertStation.Parameters.AddWithValue("@selling_ships", string.Join(", ",selling_ships.ToArray()));
				insertStation.Parameters.AddWithValue("@selling_modules", string.Join(", ",selling_modules.ToArray()));
				insertStation.Parameters.AddWithValue("@lowercase_name", name.ToLower());
				insertStation.ExecuteNonQuery();
			}
		}
		public async Task AddSystem(int id, string name, double x, double y, double z,string faction, long? population, string government, string allegiance, string state, string security, 
			string primary_economy, string power, string power_state, int needs_permit, int updated_at, string simbad_ref )
		{
			//Logger.Debug("Inserting system id " + id + ", " + name);
			using (FbCommand insertSystem = con.CreateCommand())
			{
				insertSystem.CommandText =
					"INSERT INTO eddb_systems values (@id, @name, @x, @y, @z, @faction, @population, @government, @allegiance, @state, @security, @primary_economy, @power, @power_state, @needs_permit, @updated_at, @simbad_ref, @lowercase_name)";
				insertSystem.Parameters.Clear();
				insertSystem.Parameters.Add("@id", FbDbType.Integer).Value = id;
				insertSystem.Parameters.Add("@name", FbDbType.VarChar, 150).Value = name;
				insertSystem.Parameters.Add("@x", FbDbType.Double).Value = x;
				insertSystem.Parameters.Add("@y", FbDbType.Double).Value = y;
				insertSystem.Parameters.Add("@z", FbDbType.Double).Value = z;
				insertSystem.Parameters.Add("@faction", FbDbType.VarChar, 150).Value = faction;
				insertSystem.Parameters.Add("@population", FbDbType.BigInt).Value = population;
				insertSystem.Parameters.Add("@government", FbDbType.VarChar, 130).Value = government;
				insertSystem.Parameters.Add("@allegiance", FbDbType.VarChar, 130).Value = allegiance;
				insertSystem.Parameters.Add("@state", FbDbType.VarChar, 130).Value = state;
				insertSystem.Parameters.Add("@security", FbDbType.VarChar, 150).Value = security;
				insertSystem.Parameters.Add("@primary_economy", FbDbType.VarChar, 130).Value = primary_economy;
				insertSystem.Parameters.Add("@power", FbDbType.VarChar, 130).Value = power;
				insertSystem.Parameters.Add("@power_state", FbDbType.VarChar, 130).Value = power_state;
				insertSystem.Parameters.Add("@needs_permit", FbDbType.Integer).Value = needs_permit;
				insertSystem.Parameters.Add("@updated_at", FbDbType.BigInt).Value = updated_at;
				insertSystem.Parameters.Add("@simbad_ref", FbDbType.VarChar, 150).Value = simbad_ref;
				insertSystem.Parameters.Add("@lowercase_name", FbDbType.VarChar, 150).Value = name.ToLower();
				insertSystem.ExecuteNonQuery();
			}
		}

		
		public void TestInserts()
		{
			Logger.Debug("Testing a query for a system.");
			using (FbCommand querySystem = con.CreateCommand())
			{
				querySystem.CommandText = "SELECT * from eddb_systems LIMIT 5";
				using (FbDataReader r = querySystem.ExecuteReader())
				{
					while (r.Read())
					{
						Logger.Debug("Got a record: " + r.GetString(0) + ": " + r.GetString(1));
					}
				}
			}
		}

		public List<EdsmSystem> GetSystemAsEdsm(string systemname)
		{
			List<EdsmSystem> systemResult = new List<EdsmSystem>();

			using (FbCommand getSystem = con.CreateCommand())
			{
				getSystem.CommandText = "SELECT name,id,x,y,z FROM eddb_systems WHERE lowercase_name LIKE '%" + systemname.ToLower() + "%'";
				using (FbDataReader r = getSystem.ExecuteReader())
				{
					while (r.Read())
					{
						EdsmSystem tmpsys = new EdsmSystem();
						tmpsys.Coords = new EdsmCoords();
						tmpsys.Name = r.GetString(0);
						tmpsys.Coords.X = r.GetDouble(r.GetOrdinal("X"));
						tmpsys.Coords.Y = r.GetDouble(r.GetOrdinal("Y"));
						tmpsys.Coords.Z = r.GetDouble(r.GetOrdinal("Z"));
						systemResult.Add(tmpsys);
						Logger.Debug("GetSystemEDSM added: " + r.GetString(0) + ": " + r.GetString(1) +" X: "+r.GetString(2)+" Y: "+ r.GetString(3)+" Z: "+r.GetString(4));
					}
				}

			}
			return systemResult;
		}
		public int GetSystemCount()
		{
			Logger.Debug("Counting systems...");
			try
			{
				using (FbCommand querySystemCount = con.CreateCommand())
				{
					querySystemCount.CommandText = "SELECT COUNT(id) from eddb_systems";
					using (FbDataReader r = querySystemCount.ExecuteReader())
					{
						while (r.Read())
						{
							return r.GetInt32(0);
						}
					}
				}
			}
			catch (Exception e)
			{
				Logger.Debug("Failed to query system count in SQL, probably no database. Creating.");
				CreateTables();
				return 0;
			}
			return 0;
		}
	}
}

