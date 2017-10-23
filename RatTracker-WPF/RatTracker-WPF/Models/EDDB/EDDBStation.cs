using System.Collections.Generic;

namespace RatTracker_WPF.Models.Eddb
{
  public class EddbStation
  {
    //[{"id":5,"name":"Reilly Hub","system_id":396,"max_landing_pad_size":"L","distance_to_star":171,"faction":"Abukunin Silver Fortune Ind","government":"Corporate","allegiance":"Federation","state":"None","type_id":8,"type":"Orbis Starport",
    //"has_blackmarket":false,"has_market":true,"has_refuel":true,"has_repair":true,"has_rearm":true,"has_outfitting":true,"has_shipyard":true,"has_docking":true,"has_commodities":true,
    //"import_commodities":["Pesticides","Aquaponic Systems","Biowaste"],"export_commodities":["Mineral Oil","Fruit and Vegetables","Grain"],
    //"prohibited_commodities":["Narcotics","Tobacco","Combat Stabilisers","Imperial Slaves","Slaves","Personal Weapons","Battle Weapons","Toxic Waste","Wuthielo Ku Froth","Bootleg Liquor","Landmines"],
    //"economies":["Agriculture"],"updated_at":1460286969,"shipyard_updated_at":1473630036,"outfitting_updated_at":1475052510,"market_updated_at":1475052510,"is_planetary":false,
    //"selling_ships":["Adder","Eagle Mk. II","Hauler","Sidewinder Mk. I","Viper Mk III"],
    //"selling_modules":[739,740,744,745,749,750,754,755,756,757,759,760,761,762,828,830,831,839,840,850,851,876,878,879,880,881,882,883,885,886,887,889,890,891,892,893,894,896,897,898,899,900,929,930,933,934,936,937,938,941,942,946,947,948,961,962,963,964,965,966,967,968,969,970,999,1004,1005,1008,1009,1011,1012,1013,1016,1017,1018,1021,1022,1023,1027,1032,1036,1037,1038,1041,1042,1043,1046,1047,1048,1066,1067,1071,1072,1116,1119,1120,1123,1124,1125,1128,1133,1137,1138,1182,1186,1187,1191,1192,1193,1194,1195,1196,1199,1200,1201,1202,1203,1204,1207,1208,1209,1212,1213,1214,1229,1231,1232,1233,1240,1241,1242,1243,1245,1246,1286,1306,1307,1310,1311,1316,1317,1320,1321,1324,1326,1327,1373,1375,1377,1379,1381,1421,1425,1429,1523,1524,1525,1526,1527,1528,1529,1530,1531,1532,1533,1534,1535,1538,1540,1544,1545,1550]}
    public int id { get; set; }

    public string name { get; set; }
    public int system_id { get; set; }
    public string max_landing_pad_size { get; set; }
    public int? distance_to_star { get; set; }
    public int? type_id { get; set; }
    public string type { get; set; }
    public bool? has_blackmarket { get; set; }
    public bool? has_market { get; set; }
    public bool? has_refuel { get; set; }
    public bool? has_repair { get; set; }
    public bool? has_rearm { get; set; }
    public bool? has_docking { get; set; }
    public bool? has_outfitting { get; set; }
    public bool? has_shipyard { get; set; }
    public bool? has_commodities { get; set; }
    public bool is_planetary { get; set; }
    public int? updated_at { get; set; }
  }
}