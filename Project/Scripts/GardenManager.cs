using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class GardenManager : Node2D
{
	private PackedScene plantScene;
	private List<Plant> plantedPlants = new List<Plant>();
	private SoilData currentSoilType = InitialData.SoilTypes[0];

	private VBoxContainer plantListContainer;
	private Sprite2D ghostSprite;
	
	private PlantTypeData? selectedPlantType = null;

	private int totalYearsToSimulate = 10;
	private int simulationStepMonths = 1;
	private int currentSimulationMonth = 0;

	public override void _Ready()
	{
		GD.Print("Inicjalizacja Godot Garden Manager ...");
		
		plantScene = ResourceLoader.Load<PackedScene>("res://Scenes/Plant.tscn");

		if (plantScene == null)
		{
			GD.PrintErr("Błąd: Nie można załadować sceny");
			return;
		}
		plantListContainer = GetNode<VBoxContainer>("CanvasLayer/Panel/PlantList");

		if (plantListContainer != null)
		{
			GeneratePlantButtons();
		}
		else
		{
			GD.PrintErr("BŁĄD: Nie znaleziono Vbox Container (PlantList) w UI. Sprawdź ścieżkę");
		}

		CreateGhostSprite();
	}

	private void CreateGhostSprite()
	{
		ghostSprite = new Sprite2D();
		ghostSprite.Modulate = new Color(1, 1, 1, 0.5f);
		ghostSprite.Visible = false;
		ghostSprite.Scale = new Vector2(0.5f, 0.5f);
		ghostSprite.ZIndex = 100;
		AddChild(ghostSprite);
	}

	private void GeneratePlantButtons()
{
	// 1. Sprawdzenie kontenera UI
	if (plantListContainer == null)
	{
		GD.PrintErr("BŁĄD KRYTYCZNY: Zmienna 'plantListContainer' jest NULL.");
		GD.PrintErr("Rozwiązanie: Jeśli używasz [Export], przeciągnij węzeł VBoxContainer do pola w Inspektorze.");
		GD.PrintErr("Rozwiązanie: Jeśli używasz GetNode, sprawdź czy ścieżka jest poprawna.");
		return; // Przerywamy funkcję, żeby nie było crasha
	}

	// 2. Sprawdzenie danych
	if (InitialData.PlantTypes == null)
	{
		GD.PrintErr("BŁĄD KRYTYCZNY: Lista 'InitialData.PlantTypes' nie istnieje (jest null). Sprawdź plik DataModels.cs.");
		return;
	}

	GD.Print($"Generowanie przycisków... Znaleziono {InitialData.PlantTypes.Count} roślin.");

	// Czyścimy stare przyciski
	foreach (var child in plantListContainer.GetChildren())
	{
		child.QueueFree();
	}

	// Generujemy nowe
	foreach (var plantData in InitialData.PlantTypes)
	{
		var btn = new Button();
		btn.Text = plantData.Name;
		btn.CustomMinimumSize = new Vector2(0, 40);

		// Ważne: Tworzymy lokalną kopię zmiennej dla lambdy
		var pData = plantData; 
		btn.Pressed += () => SelectPlantToPlace(pData);

		plantListContainer.AddChild(btn);
	}
}

	private void SelectPlantToPlace(PlantTypeData plantType)
	{
		selectedPlantType = plantType;
		GD.Print($"Wybrano do sadzenia: {plantType.Name}");

		if (ResourceLoader.Exists(plantType.TexturePath))
		{
			ghostSprite.Texture = (Texture2D)ResourceLoader.Load(plantType.TexturePath);
			ghostSprite.Visible = true;
		}
	}

	private void CancelPlacement()
	{
		selectedPlantType = null;
		ghostSprite.Visible = false;
		GD.Print("Anulowano sadzenie");
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion)
		{
			if (selectedPlantType != null && ghostSprite.Visible)
			{
				ghostSprite.GlobalPosition = GetGlobalMousePosition();
			}
		}

		if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
		{
			if (mouseButton.ButtonIndex == MouseButton.Left)
			{
				if (selectedPlantType != null)
				{
					PlacePlant(selectedPlantType.Value, GetGlobalMousePosition());
				}
			}
			else if (mouseButton.ButtonIndex == MouseButton.Right)
			{
				CancelPlacement();
			}
		}
	}
	
	public void PlacePlant(PlantTypeData typeData, Vector2 position)
	{
		if (plantScene == null) return;

		var newPlant = plantScene.Instantiate<Plant>();
		AddChild(newPlant);

		newPlant.Position = position;
		newPlant.Initialize(typeData);

		if (newPlant.GetNode<CollisionShape2D>("CollisionShape2D").Shape == null)
		{
			GD.PrintErr("Błąd: Roslina nie ma ustawionego CircleShape2D");
			newPlant.GetNode<CollisionShape2D>("CollisionSahpe2D").Shape = new CircleShape2D();
		}
		
		plantedPlants.Add(newPlant);
		GD.Print($"Posadzono {typeData.Name} na pozycji {position}.");
	}
	

	public void RunSimulation()
	{
		if (plantedPlants.Count == 0)
		{
			GD.Print("Brak roślin do symulacji");
			return;
		}
		
		GD.Print($"--- Rozpoczecie symulacji ({totalYearsToSimulate} LAT) ---");
		int totalMonths = totalYearsToSimulate * 12;

		for (int month = 1; month <= totalMonths; month += simulationStepMonths)
		{
			SimulateStep(month);
		}
		GD.Print("--- Symulacja zakonczona");
	}

	private void SimulateStep(int month)
	{
		currentSimulationMonth = month;
		GD.Print($"Miesiac: {currentSimulationMonth}");

		foreach (var plantA in plantedPlants)
		{
			float sunLevel = CalculateSunLevel(plantA);
			float growthFactorSoil = GetGrowthFactor(plantA);
			plantA.SimulateMonth(growthFactorSoil, sunLevel);
		}
	}

	private float GetGrowthFactor(Plant plant)
	{
		var rule = InitialData.GrowthRules.FirstOrDefault(r =>
			r.PlantTypeId == plant.TypeData.Id && r.SoilTypeId == currentSoilType.Id);

		return rule.GrowthMultiplier > 0 ? rule.GrowthMultiplier : 1.0f;
	}

	private float CalculateSunLevel(Plant plantB)
	{
		float sunLevel = 1.0f;

		foreach (var plantA in plantedPlants)
		{
			if (plantA == plantB) continue;

			if (plantA.CurrentHeight > plantB.CurrentHeight)
			{
				float distance = plantA.Position.DistanceTo(plantB.Position);
				float combinedRadius = plantA.CurrentRadius + 0.5f;

				if (distance < combinedRadius)
				{
					float heightDifference = plantA.CurrentHeight - plantB.CurrentHeight;
					float shadowImpact = Mathf.Min(heightDifference / 0.5f, 0.8f);
					
					sunLevel -= shadowImpact;
				}
			}
		}
		
		sunLevel = Mathf.Max(0.2f, sunLevel);

		float preferenceImpact = Mathf.Abs(sunLevel * 10.0f - plantB.TypeData.SunPreference) / 10.0f;

		return sunLevel * (1.0f - preferenceImpact / 2.0f);
	}

	private void _on_simulation_button_pressed()
	{
		RunSimulation();
	}
}
