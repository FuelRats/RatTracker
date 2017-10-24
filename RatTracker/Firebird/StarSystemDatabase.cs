using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using FirebirdSql.Data.FirebirdClient;
using log4net;
using RatTracker.Models.App.StarSystems;

namespace RatTracker.Firebird
{
  public class StarSystemDatabase
  {
    private const string DatabaseFileName = "StarSystemDatabase.FDB";
    private const int DataBaseVersion = 2;

    private static readonly string insertSystemCommandText =
      $@"INSERT INTO systems values (
            @{nameof(EddbSystem.Id)}, 
            @{nameof(EddbSystem.Name)}, 
            @{nameof(EddbSystem.X)}, 
            @{nameof(EddbSystem.Y)}, 
            @{nameof(EddbSystem.Z)}, 
            @{nameof(EddbSystem.Population)}, 
            @{nameof(EddbSystem.NeedsPermit)}, 
            @{nameof(EddbSystem.UpdatedAt)}, 
            @{nameof(EddbSystem.UpperCaseName)}
          )";

    private static readonly string insertStationCommandText =
      $@"INSERT INTO stations values (
            @{nameof(EddbStation.Id)}, 
            @{nameof(EddbStation.Name)}, 
            @{nameof(EddbStation.SystemId)}, 
            @{nameof(EddbStation.MaxLandingPadSize)}, 
            @{nameof(EddbStation.DistanceToStar)}, 
            @{nameof(EddbStation.HasRefuel)}, 
            @{nameof(EddbStation.HasRepair)}, 
            @{nameof(EddbStation.HasRearm)}, 
            @{nameof(EddbStation.HasOutfitting)}, 
            @{nameof(EddbStation.HasShipyard)}, 
            @{nameof(EddbStation.IsPlanetary)}, 
            @{nameof(EddbStation.UpdatedAt)}, 
            @{nameof(EddbStation.UpperCaseName)}
          )";

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

    public void Insert(BlockingCollection<EddbSystem> systems)
    {
      if (systems == null) { throw new ArgumentNullException(nameof(systems)); }

      using (var connection = new FbConnection(connectionstring))
      {
        try
        {
          connection.Open();
          using (var insertSystem = connection.CreateCommand())
          {
            insertSystem.CommandText = insertSystemCommandText;
            insertSystem.Parameters.Clear();
            insertSystem.Parameters.Add($"@{nameof(EddbSystem.Id)}", FbDbType.BigInt);
            insertSystem.Parameters.Add($"@{nameof(EddbSystem.Name)}", FbDbType.VarChar, 150);
            insertSystem.Parameters.Add($"@{nameof(EddbSystem.X)}", FbDbType.Double);
            insertSystem.Parameters.Add($"@{nameof(EddbSystem.Y)}", FbDbType.Double);
            insertSystem.Parameters.Add($"@{nameof(EddbSystem.Z)}", FbDbType.Double);
            insertSystem.Parameters.Add($"@{nameof(EddbSystem.Population)}", FbDbType.BigInt);
            insertSystem.Parameters.Add($"@{nameof(EddbSystem.NeedsPermit)}", FbDbType.Boolean);
            insertSystem.Parameters.Add($"@{nameof(EddbSystem.UpdatedAt)}", FbDbType.BigInt);
            insertSystem.Parameters.Add($"@{nameof(EddbSystem.UpperCaseName)}", FbDbType.VarChar, 150);
            var tx = insertSystem.Connection.BeginTransaction();
            insertSystem.Transaction = tx;
            insertSystem.Prepare();
            var i = 0;

            while (!systems.IsCompleted)
            {
              if (i % 10000 == 0)
              {
                tx.Commit();
                tx = insertSystem.Connection.BeginTransaction();
                insertSystem.Transaction = tx;
              }

              if (systems.TryTake(out var system))
              {
                insertSystem.Parameters[$"@{nameof(EddbSystem.Id)}"].Value = system.Id;
                insertSystem.Parameters[$"@{nameof(EddbSystem.Name)}"].Value = system.Name;
                insertSystem.Parameters[$"@{nameof(EddbSystem.X)}"].Value = system.X;
                insertSystem.Parameters[$"@{nameof(EddbSystem.Y)}"].Value = system.Y;
                insertSystem.Parameters[$"@{nameof(EddbSystem.Z)}"].Value = system.Z;
                insertSystem.Parameters[$"@{nameof(EddbSystem.Population)}"].Value = system.Population;
                insertSystem.Parameters[$"@{nameof(EddbSystem.NeedsPermit)}"].Value = system.NeedsPermit;
                insertSystem.Parameters[$"@{nameof(EddbSystem.UpdatedAt)}"].Value = system.UpdatedAt;
                insertSystem.Parameters[$"@{nameof(EddbSystem.UpperCaseName)}"].Value = system.UpperCaseName;
                insertSystem.ExecuteNonQuery();
                i++;
              }
            }

            tx.Commit();
            connection.Close();
          }

          logger.Info("Completed injection.");
        }
        catch (Exception ex)
        {
          logger.Debug("Exception in InjectSystemsToSql: " + ex.Message + "@" + ex.Source);
        }
      }
    }

    public void CloseConnection()
    {
      systemDatabaseConnection?.Close();
    }

    public void Insert(BlockingCollection<EddbStation> stations)
    {
      if (stations == null) { throw new ArgumentNullException(nameof(stations)); }

      using (var connection = new FbConnection(connectionstring))
      {
        try
        {
          connection.Open();
          using (var insertSystem = connection.CreateCommand())
          {
            insertSystem.CommandText = insertStationCommandText;
            insertSystem.Parameters.Clear();
            insertSystem.Parameters.Add($"@{nameof(EddbStation.Id)}", FbDbType.BigInt);
            insertSystem.Parameters.Add($"@{nameof(EddbStation.Name)}", FbDbType.VarChar, 150);
            insertSystem.Parameters.Add($"@{nameof(EddbStation.SystemId)}", FbDbType.BigInt);
            insertSystem.Parameters.Add($"@{nameof(EddbStation.MaxLandingPadSize)}", FbDbType.Char, 1);
            insertSystem.Parameters.Add($"@{nameof(EddbStation.DistanceToStar)}", FbDbType.Integer);
            insertSystem.Parameters.Add($"@{nameof(EddbStation.HasRefuel)}", FbDbType.Boolean);
            insertSystem.Parameters.Add($"@{nameof(EddbStation.HasRepair)}", FbDbType.Boolean);
            insertSystem.Parameters.Add($"@{nameof(EddbStation.HasRearm)}", FbDbType.Boolean);
            insertSystem.Parameters.Add($"@{nameof(EddbStation.HasOutfitting)}", FbDbType.Boolean);
            insertSystem.Parameters.Add($"@{nameof(EddbStation.HasShipyard)}", FbDbType.Boolean);
            insertSystem.Parameters.Add($"@{nameof(EddbStation.IsPlanetary)}", FbDbType.Boolean);
            insertSystem.Parameters.Add($"@{nameof(EddbStation.UpdatedAt)}", FbDbType.BigInt);
            insertSystem.Parameters.Add($"@{nameof(EddbStation.UpperCaseName)}", FbDbType.VarChar, 150);
            var tx = insertSystem.Connection.BeginTransaction();
            insertSystem.Transaction = tx;
            insertSystem.Prepare();
            var i = 0;

            while (!stations.IsCompleted)
            {
              if (i % 10000 == 0)
              {
                tx.Commit();
                tx = insertSystem.Connection.BeginTransaction();
                insertSystem.Transaction = tx;
              }

              if (stations.TryTake(out var station))
              {
                insertSystem.Parameters[$"@{nameof(EddbStation.Id)}"].Value = station.Id;
                insertSystem.Parameters[$"@{nameof(EddbStation.Name)}"].Value = station.Name;
                insertSystem.Parameters[$"@{nameof(EddbStation.SystemId)}"].Value = station.SystemId;
                insertSystem.Parameters[$"@{nameof(EddbStation.MaxLandingPadSize)}"].Value = station.MaxLandingPadSize;
                insertSystem.Parameters[$"@{nameof(EddbStation.DistanceToStar)}"].Value = station.DistanceToStar;
                insertSystem.Parameters[$"@{nameof(EddbStation.HasRefuel)}"].Value = station.HasRefuel;
                insertSystem.Parameters[$"@{nameof(EddbStation.HasRepair)}"].Value = station.HasRepair;
                insertSystem.Parameters[$"@{nameof(EddbStation.HasRearm)}"].Value = station.HasRearm;
                insertSystem.Parameters[$"@{nameof(EddbStation.HasOutfitting)}"].Value = station.HasOutfitting;
                insertSystem.Parameters[$"@{nameof(EddbStation.HasShipyard)}"].Value = station.HasShipyard;
                insertSystem.Parameters[$"@{nameof(EddbStation.IsPlanetary)}"].Value = station.IsPlanetary;
                insertSystem.Parameters[$"@{nameof(EddbStation.UpdatedAt)}"].Value = station.UpdatedAt;
                insertSystem.Parameters[$"@{nameof(EddbStation.UpperCaseName)}"].Value = station.UpperCaseName;
                insertSystem.ExecuteNonQuery();
                i++;
              }
            }

            using (var command = connection.CreateCommand())
            {
              command.CommandText = $"UPDATE dbinfo SET lastFullImport = '{DateTime.UtcNow:yyyy-MM-dd}', lastRecentSystemsImport = '{DateTime.UtcNow:yyyy-MM-dd}'";
              command.Transaction = tx;
              command.ExecuteNonQuery();
            }

            tx.Commit();
            connection.Close();
          }

          logger.Info("Completed injection.");
        }
        catch (Exception ex)
        {
          logger.Debug("Exception in InjectSystemsToSql: " + ex.Message + "@" + ex.Source);
        }
      }
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
              "CREATE TABLE dbinfo (version int, lastFullImport DATE, lastRecentSystemsImport DATE)";
            createTable.ExecuteNonQuery();
          }
          using (var insertToDbInfo = connection.CreateCommand())
          {
            insertToDbInfo.CommandText = $"INSERT INTO dbinfo VALUES ({DataBaseVersion}, '1-1-1', '1-1-1')";
            insertToDbInfo.ExecuteNonQuery();
          }
          using (var createTable = connection.CreateCommand())
          {
            createTable.CommandText =
              @"CREATE TABLE systems (
                  id int, 
                  name varchar(150), 
                  x float, 
                  y float, 
                  z float, 
                  population bigint, 
                  needspermit boolean, 
                  updatedat bigint, 
                  uppercasename varchar(150))";
            createTable.ExecuteNonQuery();
          }
          using (var createTable = connection.CreateCommand())
          {
            createTable.CommandText =
              @"CREATE TABLE stations (
                  id bigint, 
                  name varchar(150), 
                  systemid bigint, 
                  maxlandingpadsize char(1), 
                  distancetostar bigint,  
                  hasrefuel boolean, 
                  hasrepair boolean, 
                  hasrearm boolean, 
                  hasoutfitting boolean, 
                  hasshipyard boolean, 
                  isplanetary boolean,
                  updatedat bigint, 
                  uppercasename varchar(150))";
            createTable.ExecuteNonQuery();
          }
          //using (var createTable = connection.CreateCommand())
          //{
          //  createTable.CommandText =
          //    "CREATE TABLE eddb_info (sectorname varchar(150), sectorsize bigint, injectedat bigint)";
          //  createTable.ExecuteNonQuery();
          //}

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