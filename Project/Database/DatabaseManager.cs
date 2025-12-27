namespace GardenPlanner.Database;

using Godot;
using System;
using System.Collections.Generic;
using MySqlConnector;
using System.Threading.Tasks;

public partial class DatabaseManager : Node
{
    private const string Server = "localhost";
    private const string Database = "mydb";
    private const string User = "root";
    private const string Password = "root";
    
    private MySqlConnection connection;
    private string connectionString = $"Server={Server};Database={Database};Uid={User};Pwd={Password};";

    public override void _Ready()
    {
        GD.Print($"[DB] Konfiguracja: {connectionString}");
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            using (var testConnection = new MySqlConnection(connectionString))
            {
                await testConnection.OpenAsync();
                GD.Print("[DB] Połączenie testowe z bazą danych MySQL nawiązane pomyślnie.");
                return true;
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[DB ERROR] Nie udało się nawiązać połączenia z MySQL: {ex.Message}");
            return false;
        }
    }

    public async Task<List<PlantTypeData>> LoadPlantTypesAsync()
    {
        var plantTypes = new List<PlantTypeData>();

        
        using (var connection = new MySqlConnection(connectionString))
        {
            try
            {
                await connection.OpenAsync();
                
                const string query =
                    "SELECT ID, Nazwa, Tekstura2D, MaxWysokosc, MaxSzerokosc, GlebokoscKorzeni, PromienKorony, PreferencjeSlonca, Typ_ID FROM Roslina;";
                
                using (var command = new MySqlCommand(query, connection)) 
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        plantTypes.Add(new PlantTypeData
                        {
                            Id = reader.GetInt32("ID"),
                            Name = reader.GetString("Nazwa"),
                            TexturePath = reader.GetString("Tekstura2D"),
                            MaxHeight = reader.GetFloat("MaxWysokosc"),
                            MaxWidth = reader.GetFloat("MaxSzerokosc"),
                            RootDepth = reader.GetFloat("GlebokoscKorzeni"),
                            CanopyRadius = reader.GetFloat("PromienKorony"),
                            SunPreference = reader.GetFloat("PreferencjeSlonca"),
                            TypeId = reader.GetInt32("Typ_ID"),
                            BaseGrowthRate = 0.05f
                        });
                    }
                }
                GD.Print($"[DB] Załadowano {plantTypes.Count} typow roslin");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[DB ERROR] Nie udało się załadować typów roślin: {ex.Message}");
            }

            return plantTypes;
        }
    }
    
    public async Task<List<SoilData>> LoadSoilDataAsync()
    {
        var soilTypes = new List<SoilData>();

        using (var connection = new MySqlConnection(connectionString))
        {
            try
            {
                await connection.OpenAsync();
                
                const string query = "SELECT ID, Nazwa, RetencjaWody, PoziomOdzywczy FROM TypGleby;";

                
                using (var command = new MySqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        soilTypes.Add(new SoilData
                        {
                            Id = reader.GetInt32("ID"),
                            Name = reader.GetString("Nazwa"),
                            WaterRetention = reader.GetFloat("RetencjaWody"),
                            NutrientLevel = reader.GetFloat("PoziomOdzywczy"),
                        });
                    }
                }

                GD.Print($"[DB] Załadowano {soilTypes.Count} typów gleby");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[DB ERROR] Nie udało się zładować typów gleby: {ex.Message}");
            }
            return soilTypes;
        }
    }
}
