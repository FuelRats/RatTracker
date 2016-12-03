using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using FirebirdSql.Data.FirebirdClient;
using log4net;
using RatTracker_WPF.Models.App;
using RatTracker_WPF.Models.Edsm;
using RatTracker_WPF.Models.Eddb;
using RatTracker_WPF.EventHandlers;
using static System.Windows.MessageBox;
using System.Threading;

namespace RatTracker_WPF
{
    public delegate void FireBirdLoadedEvent(object sender, FireBirdLoadedArgs args);
    public class FireBird : PropertyChangedBase
	{


	    public event FireBirdLoadedEvent FireBirdLoadedEvent;

		private static readonly ILog Logger =
			LogManager.GetLogger(System.Reflection.Assembly.GetCallingAssembly().GetName().Name);
        
		private string _status="Initializing";
        private bool _dbReady;
        string _rtPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        string _fbConStr = "User=SYSDBA;Password=masterkey;Database=" + Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\RatTracker\EDDB.FDB;Dialect=3;Charset=UTF8;ServerType=1;";
        EddbData _eddbworker;

		public string Status
		{
			get
			{
				return _status;
			}
			set
			{
				_status = value;
				NotifyPropertyChanged();
			}
		}
		private FbConnection _con;
        public void SetEDDB(ref EddbData eddbworker)
        {
            _eddbworker = eddbworker;
        }
		public void InitDB()
		{
			bool newdb = false;
			Logger.Debug("Starting FbConnection");
            Status = "Connecting";
		    FbConnectionStringBuilder builder = new FbConnectionStringBuilder
		    {
		        UserID = "SYSDBA",
		        Database = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\RatTracker\EDDB.FDB",
		        ServerType = FbServerType.Embedded
		    };
		    if (Thread.CurrentThread.Name == null)
            {
                Thread.CurrentThread.Name = "FireBirdWorker";
            }
            if (File.Exists(_rtPath + @"\RatTracker\EDDB.FDB"))
			{
				Logger.Info("FBworker: EDDB database file found.");
                _dbReady = true;
            }
			else
			{
				Logger.Debug("Creating Database!");
                Status = "Creating Database";
                try
                {
                    if (Directory.Exists(_rtPath + @"\RatTracker"))
                    {
                        Logger.Info("Creating database "+builder.ConnectionString+".");
                        FbConnection.CreateDatabase(builder.ConnectionString);
                        _dbReady = true;
                    }
                    else
                    {
                        Directory.CreateDirectory(_rtPath+@"\RatTracker");
                        Logger.Info("Creating directory and database " + builder.ConnectionString);
                        FbConnection.CreateDatabase(builder.ConnectionString);
                        _dbReady = true;
                    }
                    newdb = true;
                }
                catch (Exception ex)
                {
                    Logger.Debug("Oops. Couldn't create database, probably because I got launched for Oauth. Doing nothing. "+ex.Message);
                    return;
                }
			}
			_con = new FbConnection(
                        _fbConStr);
				try
				{
					_con.Open();
					Logger.Info("FBWorker has connected to the database.");
					Status = "Connected";
					if (newdb)
						CreateTables();
				    FireBirdLoadedEvent?.Invoke(this, new FireBirdLoadedArgs());
				}
				catch (Exception ex)
				{
					Show(ex.ToString());
				}
			}
        public void DropDB()
        {
            if (_dbReady == false)
            {
                Logger.Debug("Attempting to drop DB without a ready Database. Bailing.");
                return;
            }
            Logger.Debug("FBWorker is dropping the database due to a refresh command.");
            using (FbCommand dropTables = _con.CreateCommand())
            {
                dropTables.CommandText = "DROP INDEX ix_lcname";
                dropTables.ExecuteNonQuery();
                dropTables.CommandText = "DROP TABLE eddb_systems";
                dropTables.ExecuteNonQuery();
                dropTables.CommandText = "DROP TABLE eddb_stations";
                dropTables.ExecuteNonQuery();
                dropTables.CommandText = "DROP DOMAIN BOOLEAN";
                dropTables.ExecuteNonQuery();
            }
            Logger.Debug("Recreating tables.");
            CreateTables();
        }
		public void CreateTables()
		{
            if (_dbReady == false)
            {
                Logger.Debug("Attempting to create tables without a ready Database. Bailing.");
                return;
            }
            try
            {
                Logger.Debug("Starting database table creation.");
                Status = "Creating Tables";
                using (FbCommand createDomain = _con.CreateCommand())
                {
                    createDomain.CommandText = "CREATE DOMAIN BOOLEAN AS SMALLINT CHECK (value is null or value in (0, 1))";
                    createDomain.ExecuteNonQuery();
                }
                using (FbCommand createTable = _con.CreateCommand())
                {
                    createTable.CommandText =
                            "CREATE TABLE eddb_systems (id int, name varchar(150), x float, y float, z float, faction varchar(150), population bigint, goverment varchar(130), allegiance varchar(130), state varchar(130), " +
                            "security varchar(150), primary_economy varchar(130), power varchar(130), power_state varchar(130), needs_permit boolean, updated_at bigint, simbad_ref varchar(150), lowercase_name varchar(150))";
                    createTable.ExecuteNonQuery();
                }
                using (FbCommand createTable = _con.CreateCommand())
                {
                    createTable.CommandText =
                        "CREATE TABLE eddb_stations (id bigint, name varchar(150), system_id bigint, max_landing_pad_size varchar(5), distance_to_star bigint, faction varchar(150), government varchar(120), allegiance varchar(130), " +
                        "state varchar(120), type_id int, type varchar(130), has_blackmarket boolean, has_market boolean, has_refuel boolean, has_repair boolean, has_rearm boolean, has_outfitting boolean, has_shipyard boolean, has_docking boolean, " +
                        "has_commodities boolean, prohibited_commodities varchar(10000), economies varchar(10000), updated_at bigint, shipyard_updated_at bigint, outfitting_updated_at bigint, market_updated_at bigint, is_planetary boolean, " +
                        "selling_ships varchar(20000), selling_modules varchar(20000), lowercase_name varchar(150))";
                    createTable.ExecuteNonQuery();

                }
                using (FbCommand createTable = _con.CreateCommand())
                {
                    createTable.CommandText = "CREATE TABLE eddb_info (sectorname varchar(150), sectorsize bigint, injectedat bigint)";
                    createTable.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Logger.Debug("Exception in CreateTables! " + ex.Message + "@" + ex.Source);
            }
            Logger.Debug("Completed database table creation.");
			Status = "Initialized";
		}

        public void CreateIndexes()
        {
            if (_dbReady == false)
            {
                Logger.Debug("Attempting to create tables without a ready Database. Bailing.");
                return;
            }
            try
            {
                Status = "Creating Indexes";
                Logger.Info("Creating indexes...");
                using (FbCommand createIndex = _con.CreateCommand())
                {
                    createIndex.CommandText = "CREATE INDEX ix_lcname on eddb_systems (lowercase_name)";
                    createIndex.ExecuteNonQuery();
                }
                Logger.Info("Indexes created.");
                Status = "Ready";
            }
            catch (Exception ex)
            {
                Logger.Debug("Exception in CreateIndexes: " + ex.Message + "@" + ex.Source);
            }
        }
		public async Task AddStation(int id, string name, int system_id, string max_landing_pad_size, int? distance_to_star,
			string faction, string government, string allegiance, string state, int? type_id, string type, bool? has_blackmarket,
			bool? has_market, bool? has_refuel, bool? has_repair, bool? has_rearm, bool? has_outfitting, bool? has_shipyard, bool? has_docking,
			bool? has_commodities, List<string> import_commodities, List<string> export_commodities, List<string> prohibited_commodities,
			List<string> economies, int? updated_at, int? shipyard_updated_at, int? outfitting_updated_at, int? market_updated_at,
			bool is_planetary, List<string> selling_ships, List<int> selling_modules)
		{
            if (_dbReady == false)
            {
                Logger.Debug("Attempting to insert without a ready Database. Bailing.");
                return;
            }
            try
            {
                Status = "Inserting";
                using (FbCommand insertStation = _con.CreateCommand())
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
                    insertStation.Parameters.AddWithValue("@selling_ships", string.Join(", ", selling_ships.ToArray()));
                    insertStation.Parameters.AddWithValue("@selling_modules", string.Join(", ", selling_modules.ToArray()));
                    insertStation.Parameters.AddWithValue("@lowercase_name", name.ToLower());
                    await insertStation.ExecuteNonQueryAsync();
                }
                Status = "Ready!";
            }
            catch (Exception ex)
            {
                Logger.Debug("Exception in InsertStation:" + ex.Message + "@" + ex.Source);
            }
		}
/*
        public async Task BulkAddStation(List<EddbStation> stations)
        {
            if (stations == null)
            {
                Logger.Debug("Null system list passed to InjectSystemsToSql!");
                return;
            }
            Logger.Info("Injecting " + systems.Count + " systems to SQL.");
            FbConnection con2 = new FbConnection(
            "User=SYSDBA;Password=masterkey;Database=EDDB.FDB;Dialect=3;Charset=UTF8;ServerType=1;");
            try
            {
                con2.Open();
            }
            catch (Exception ex)
            {
                Show(ex.ToString());
            }
            try
            {

                using (FbCommand insertStation = con2.CreateCommand())
                {
                    insertStation.CommandText =
    "INSERT INTO eddb_systems values (@id, @name, @x, @y, @z, @faction, @population, @government, @allegiance, @state, @security, @primary_economy, @power, @power_state, @needs_permit, @updated_at, @simbad_ref, @lowercase_name)";
                    insertStation.Parameters.Clear();
                    insertStation.CommandText =
                        "INSERT INTO eddb_stations values (@id, @name, @system_id, @max_landing_pad_size, @distance_to_star, @faction, @government, @allegiance, @state, @type_id, @type, @has_blackmarket, @has_market, " +
                        "@has_refuel, @has_repair, @has_rearm, @has_outfitting, @has_shipyard, @has_docking, @has_commodities, @prohibited_commodities, @economies, @updated_at, @shipyard_updated_at, @outfitting_updated_at, @market_updated_at, " +
                        "@is_planetary, @selling_ships, @selling_modules, @lowercase_name)";
                    insertStation.Parameters.Clear();
                    //id bigint, name varchar(150), system_id bigint, max_landing_pad_size varchar(5), distance_to_star bigint, faction varchar(150), goverment varchar(120), allegiance varchar(130), " +
                    //"state varchar(120), type_id int, type varchar(130), has_blackmarket boolean, has_market boolean, has_refuel boolean, has_repair boolean, has_rearm boolean, has_outfitting boolean, has_shipyard boolean, has_docking boolean, " +
                    //"has_commodities boolean, prohibited_commodities varchar(10000), economies varchar(10000), updated_at bigint, shipyard_updated_at bigint, outfitting_updated_at bigint, market_updated_at bigint, is_planetary boolean, " +
                    //"selling_ships varchar(20000), selling_modules varchar(20000), lowercase_name varchar(150)
                    insertStation.Parameters.Add("@id", FbDbType.BigInt);
                    insertStation.Parameters.Add("@name", FbDbType.VarChar,150);
                    insertStation.Parameters.Add("@system_id", FbDbType.BigInt);
                    insertStation.Parameters.Add("@max_landing_pad_size", FbDbType.VarChar, 5);
                    insertStation.Parameters.Add("@distance_to_star", FbDbType.BigInt);
                    insertStation.Parameters.Add("@faction", FbDbType.VarChar, 150);
                    insertStation.Parameters.Add("@government", FbDbType.VarChar, 120);
                    insertStation.Parameters.Add("@allegiance", FbDbType.VarChar, 130);
                    insertStation.Parameters.Add("@state", FbDbType.VarChar, 120);
                    insertStation.Parameters.Add("@type_id", FbDbType.Integer);
                    insertStation.Parameters.Add("@type", FbDbType.VarChar, 130);
                    insertStation.Parameters.Add("@has_blackmarket", FbDbType.Boolean);
                    insertStation.Parameters.Add("@has_market", FbDbType.Boolean);
                    insertStation.Parameters.Add("@has_refuel", FbDbType.Boolean);
                    insertStation.Parameters.Add("@has_repair", FbDbType.Boolean);
                    insertStation.Parameters.Add("@has_rearm", FbDbType.Boolean);
                    insertStation.Parameters.Add("@has_repair",)
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
                    insertStation.Parameters.AddWithValue("@selling_ships", string.Join(", ", selling_ships.ToArray()));
                    insertStation.Parameters.AddWithValue("@selling_modules", string.Join(", ", selling_modules.ToArray()));
                    insertStation.Parameters.AddWithValue("@lowercase_name", name.ToLower());

                    insertSystem.Parameters.Add("@id", FbDbType.Integer);
                    insertSystem.Parameters.Add("@name", FbDbType.VarChar, 150);
                    insertSystem.Parameters.Add("@x", FbDbType.Double);
                    insertSystem.Parameters.Add("@y", FbDbType.Double);
                    insertSystem.Parameters.Add("@z", FbDbType.Double);
                    insertSystem.Parameters.Add("@faction", FbDbType.VarChar, 150);
                    insertSystem.Parameters.Add("@population", FbDbType.BigInt);
                    insertSystem.Parameters.Add("@government", FbDbType.VarChar, 130);
                    insertSystem.Parameters.Add("@allegiance", FbDbType.VarChar, 130);
                    insertSystem.Parameters.Add("@state", FbDbType.VarChar, 130);
                    insertSystem.Parameters.Add("@security", FbDbType.VarChar, 150);
                    insertSystem.Parameters.Add("@primary_economy", FbDbType.VarChar, 130);
                    insertSystem.Parameters.Add("@power", FbDbType.VarChar, 130);
                    insertSystem.Parameters.Add("@power_state", FbDbType.VarChar, 130);
                    insertSystem.Parameters.Add("@needs_permit", FbDbType.Integer);
                    insertSystem.Parameters.Add("@updated_at", FbDbType.BigInt);
                    insertSystem.Parameters.Add("@simbad_ref", FbDbType.VarChar, 150);
                    insertSystem.Parameters.Add("@lowercase_name", FbDbType.VarChar, 150);
                    FbTransaction tx = insertSystem.Connection.BeginTransaction();
                    insertSystem.Transaction = tx;
                    insertSystem.Prepare();
                    int i = 0;
                    foreach (EddbSystem system in systems)
                    {
                        if (_eddbworker != null)
                            _eddbworker.SystemCounter++;
                        insertSystem.Parameters["@id"].Value = system.id;
                        insertSystem.Parameters["@name"].Value = system.name;
                        insertSystem.Parameters["@x"].Value = system.x;
                        insertSystem.Parameters["@y"].Value = system.y;
                        insertSystem.Parameters["@z"].Value = system.z;
                        insertSystem.Parameters["@faction"].Value = system.faction;
                        insertSystem.Parameters["@population"].Value = system.population;
                        insertSystem.Parameters["@government"].Value = system.government;
                        insertSystem.Parameters["@allegiance"].Value = system.allegiance;
                        insertSystem.Parameters["@state"].Value = system.state;
                        insertSystem.Parameters["@security"].Value = system.security;
                        insertSystem.Parameters["@primary_economy"].Value = system.primary_economy;
                        insertSystem.Parameters["@power"].Value = system.power;
                        insertSystem.Parameters["@power_state"].Value = system.power_state;
                        insertSystem.Parameters["@needs_permit"].Value = system.needs_permit;
                        insertSystem.Parameters["@updated_at"].Value = system.updated_at;
                        insertSystem.Parameters["@simbad_ref"].Value = system.simbad_ref;
                        insertSystem.Parameters["@lowercase_name"].Value = system.name.ToLower();
                        await insertSystem.ExecuteNonQueryAsync();
                        i++;
                        if (i % 10000 == 0)
                        {
                            tx.Commit();
                            tx = insertSystem.Connection.BeginTransaction();
                            insertSystem.Transaction = tx;
                        }
                    }
                    tx.Commit();
                    con2.Close();
                }
                Logger.Info("Completed injection.");
            }
            catch (Exception ex)
            {
                Logger.Debug("Exception in InjectSystemsToSql: " + ex.Message + "@" + ex.Source);
            }
        }
        */
        public async Task AddSystem(int id, string name, double x, double y, double z,string faction, long? population, string government, string allegiance, string state, string security, 
			string primary_economy, string power, string power_state, int needs_permit, int updated_at, string simbad_ref )
		{
            if (_dbReady == false)
            {
                Logger.Debug("Attempting to create tables without a ready Database. Bailing.");
                return;
            }
            try
            {
                //Logger.Debug("Inserting system id " + id + ", " + name);
                Status = "Inserting";
                using (FbCommand insertSystem = _con.CreateCommand())
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
                    await insertSystem.ExecuteNonQueryAsync();
                }
                Status = "Ready!";
            }
            catch(Exception ex)
            {
                Logger.Debug("Exception in AddSystem: " + ex.Message + "@" + ex.Source);
            }
		}

		public async Task BulkAddSystem(List<EddbSystem> systems)
        {
            if (systems == null)
            {
                Logger.Debug("Null system list passed to InjectSystemsToSql!");
                return;
            }
            Logger.Info("Injecting " + systems.Count + " systems to SQL.");
            Status = "Bulk inserting...";
            FbConnection con2 = new FbConnection(
            _fbConStr);
            try
            {
                con2.Open();
            }
            catch (Exception ex)
            {
                Show(ex.ToString());
            }
            try
            {

                using (FbCommand insertSystem = con2.CreateCommand())
                {
                    insertSystem.CommandText =
    "INSERT INTO eddb_systems values (@id, @name, @x, @y, @z, @faction, @population, @government, @allegiance, @state, @security, @primary_economy, @power, @power_state, @needs_permit, @updated_at, @simbad_ref, @lowercase_name)";
                    insertSystem.Parameters.Clear();
                    insertSystem.Parameters.Add("@id", FbDbType.Integer);
                    insertSystem.Parameters.Add("@name", FbDbType.VarChar, 150);
                    insertSystem.Parameters.Add("@x", FbDbType.Double);
                    insertSystem.Parameters.Add("@y", FbDbType.Double);
                    insertSystem.Parameters.Add("@z", FbDbType.Double);
                    insertSystem.Parameters.Add("@faction", FbDbType.VarChar, 150);
                    insertSystem.Parameters.Add("@population", FbDbType.BigInt);
                    insertSystem.Parameters.Add("@government", FbDbType.VarChar, 130);
                    insertSystem.Parameters.Add("@allegiance", FbDbType.VarChar, 130);
                    insertSystem.Parameters.Add("@state", FbDbType.VarChar, 130);
                    insertSystem.Parameters.Add("@security", FbDbType.VarChar, 150);
                    insertSystem.Parameters.Add("@primary_economy", FbDbType.VarChar, 130);
                    insertSystem.Parameters.Add("@power", FbDbType.VarChar, 130);
                    insertSystem.Parameters.Add("@power_state", FbDbType.VarChar, 130);
                    insertSystem.Parameters.Add("@needs_permit", FbDbType.Integer);
                    insertSystem.Parameters.Add("@updated_at", FbDbType.BigInt);
                    insertSystem.Parameters.Add("@simbad_ref", FbDbType.VarChar, 150);
                    insertSystem.Parameters.Add("@lowercase_name", FbDbType.VarChar, 150);
                    FbTransaction tx = insertSystem.Connection.BeginTransaction();
                    insertSystem.Transaction = tx;
                    insertSystem.Prepare();
                    int i = 0;
                    foreach (EddbSystem system in systems)
                    {
                        if (_eddbworker != null)
                            _eddbworker.SystemCounter++;
                        insertSystem.Parameters["@id"].Value = system.id;
                        insertSystem.Parameters["@name"].Value = system.name;
                        insertSystem.Parameters["@x"].Value = system.x;
                        insertSystem.Parameters["@y"].Value = system.y;
                        insertSystem.Parameters["@z"].Value = system.z;
                        insertSystem.Parameters["@faction"].Value = system.faction;
                        insertSystem.Parameters["@population"].Value = system.population;
                        insertSystem.Parameters["@government"].Value = system.government;
                        insertSystem.Parameters["@allegiance"].Value = system.allegiance;
                        insertSystem.Parameters["@state"].Value = system.state;
                        insertSystem.Parameters["@security"].Value = system.security;
                        insertSystem.Parameters["@primary_economy"].Value = system.primary_economy;
                        insertSystem.Parameters["@power"].Value = system.power;
                        insertSystem.Parameters["@power_state"].Value = system.power_state;
                        insertSystem.Parameters["@needs_permit"].Value = system.needs_permit;
                        insertSystem.Parameters["@updated_at"].Value = system.updated_at;
                        insertSystem.Parameters["@simbad_ref"].Value = system.simbad_ref;
                        insertSystem.Parameters["@lowercase_name"].Value = system.name.ToLower();
                        await insertSystem.ExecuteNonQueryAsync();
                        i++;
                        if (i % 10000 == 0)
                        {
                            tx.Commit();
                            tx = insertSystem.Connection.BeginTransaction();
                            insertSystem.Transaction = tx;
                        }
                    }
                    Status = "Committing transaction...";
                    tx.Commit();
                    con2.Close();
                }
                Logger.Info("Completed injection.");
                Status = "Ready!";
            }
            catch (Exception ex)
            {
                Logger.Debug("Exception in InjectSystemsToSql: " + ex.Message + "@" + ex.Source);
            }
        }
		public void TestInserts()
		{
			Logger.Debug("Testing a query for a system.");
			using (FbCommand querySystem = _con.CreateCommand())
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

		public async Task<List<EdsmSystem>> GetSystemAsEdsm(string systemname)
		{
			List<EdsmSystem> systemResult = new List<EdsmSystem>();
            Status = "Working...";
			using (FbCommand getSystem = _con.CreateCommand())
			{
				getSystem.CommandText = "SELECT FIRST 50 name,id,x,y,z FROM eddb_systems WHERE lowercase_name LIKE '%" + systemname.ToLower() + "%'";
                using (DbDataReader r = await getSystem.ExecuteReaderAsync())
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
                        Logger.Debug("GetSystemEDSM added: " + r.GetString(0) + ": " + r.GetString(1) + " X: " + r.GetString(2) + " Y: " + r.GetString(3) + " Z: " + r.GetString(4));
                    }
                }

			}
            Status = "Ready!";
			return systemResult;
		}
		public int GetSystemCount()
		{
			Logger.Debug("Counting systems...");
            Status = "Working...";
			try
			{
				using (FbCommand querySystemCount = _con.CreateCommand())
				{
					querySystemCount.CommandText = "SELECT COUNT(id) from eddb_systems";
					using (FbDataReader r = querySystemCount.ExecuteReader())
					{
						while (r.Read())
						{
                            Status = "Ready!";
							return r.GetInt32(0);
						}
					}
				}
			}
			catch (Exception e)
			{
				Logger.Debug("Failed to query system count in SQL, probably no database. Creating. Error was "+e.Message);
				CreateTables();
				return 0;
			}
			return 0;
		}
	}
}

