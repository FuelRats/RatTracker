using System;
using System.IO;
using System.Threading.Tasks;
using FirebirdSql.Data.FirebirdClient;
using log4net;

namespace RatTracker.Firebird
{
  public class StarSystemDatabase
  {
    private const string DatabaseFileName = "StarSystemDatabase.FDB";
    private const int DataBaseVersion = 1;

    private static readonly string databaseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "RatTracker");
    private static readonly string databaseFullPath = Path.Combine(databaseDirectory, DatabaseFileName);

    private static readonly string connectionstring =
      $"User=SYSDBA;Password=masterkey;Database={databaseFullPath};Dialect=3;Charset=UTF8;ServerType=1;Pooling=false";

    private readonly ILog logger;
    private FbConnection systemDatabaseConnection;

    public StarSystemDatabase(ILog log)
    {
      logger = log;
    }

    public void Initialize()
    {
      logger.Debug("Starting FbConnection");
      if (!File.Exists(databaseFullPath))
      {
        CreateDatabase();
      }

      try
      {
        var version = GetVersionFromDatabase();
        if (DataBaseVersion != version)
        {
          File.Delete(databaseFullPath);
          Initialize();
          return;
        }

        systemDatabaseConnection = new FbConnection(connectionstring);
        systemDatabaseConnection.Open();
        logger.Info("FBWorker has connected to the database.");
      }
      catch (Exception ex)
      {
        logger.Fatal(ex.ToString());
        throw;
      }
    }

    public async Task FindNearestStationAsync()
    {
      // not implemented yet
      await Task.CompletedTask;
    }

    private static int GetVersionFromDatabase()
    {
      using (var connection = new FbConnection(connectionstring))
      {
        connection.Open();
        using (var command = connection.CreateCommand())
        {
          command.CommandText = "SELECT version FROM dbinfo";
          var versionObject = command.ExecuteScalar();
          var version = versionObject is int i ? i : -1;
          return version;
        }
      }
    }

    private void CreateDatabase()
    {
      logger.Debug("Creating Database!");
      try
      {
        if (!Directory.Exists(databaseDirectory))
        {
          Directory.CreateDirectory(databaseDirectory);
        }

        var builder = new FbConnectionStringBuilder
        {
          UserID = "SYSDBA",
          Database = databaseFullPath,
          ServerType = FbServerType.Embedded,
          Pooling = false
        };

        logger.Info("Creating database " + builder.ConnectionString + ".");
        FbConnection.CreateDatabase(builder.ConnectionString);
        CreateTables();
      }
      catch (Exception ex)
      {
        logger.Fatal("Oops. Couldn't create database, probably because I got launched for Oauth. Doing nothing. ", ex);
        throw;
      }
    }

    private void CreateTables()
    {
      using (var connection = new FbConnection(connectionstring))
      {
        try
        {
          connection.Open();
          logger.Debug("Starting database table creation.");
          using (var createTable = connection.CreateCommand())
          {
            createTable.CommandText =
              "CREATE TABLE dbinfo (version int)";
            createTable.ExecuteNonQuery();
          }
          using (var insertToDbInfo = connection.CreateCommand())
          {
            insertToDbInfo.CommandText = $"INSERT INTO dbinfo VALUES ({DataBaseVersion})";
            insertToDbInfo.ExecuteNonQuery();
          }
          using (var createTable = connection.CreateCommand())
          {
            createTable.CommandText =
              "CREATE TABLE eddb_systems (id int, name varchar(150), x float, y float, z float, faction varchar(150), population bigint, goverment varchar(130), allegiance varchar(130), state varchar(130), " +
              "security varchar(150), primary_economy varchar(130), power varchar(130), power_state varchar(130), needs_permit boolean, updated_at bigint, simbad_ref varchar(150), lowercase_name varchar(150))";
            createTable.ExecuteNonQuery();
          }
          using (var createTable = connection.CreateCommand())
          {
            createTable.CommandText =
              "CREATE TABLE eddb_stations (id bigint, name varchar(150), system_id bigint, max_landing_pad_size varchar(5), distance_to_star bigint, faction varchar(150), government varchar(120), allegiance varchar(130), " +
              "state varchar(120), type_id int, type varchar(130), has_blackmarket boolean, has_market boolean, has_refuel boolean, has_repair boolean, has_rearm boolean, has_outfitting boolean, has_shipyard boolean, has_docking boolean, " +
              "has_commodities boolean, prohibited_commodities varchar(10000), economies varchar(10000), updated_at bigint, shipyard_updated_at bigint, outfitting_updated_at bigint, market_updated_at bigint, is_planetary boolean, " +
              "selling_ships varchar(20000), selling_modules varchar(20000), lowercase_name varchar(150))";
            createTable.ExecuteNonQuery();
          }
          using (var createTable = connection.CreateCommand())
          {
            createTable.CommandText =
              "CREATE TABLE eddb_info (sectorname varchar(150), sectorsize bigint, injectedat bigint)";
            createTable.ExecuteNonQuery();
          }

          logger.Debug("Completed database table creation.");
        }
        catch (Exception ex)
        {
          logger.Debug("Exception in CreateTables! " + ex.Message + "@" + ex.Source);
        }
      }
    }
  }
}