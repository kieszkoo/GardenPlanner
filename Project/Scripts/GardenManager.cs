using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class GardenManager : Node2D
{
	private PackedScene plantScene;
	private List<Plant> plantedPlants = new List<Plant>();
	private SoilData currentSoilType = InitialData.SoilTypes[0];

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
		}

		TestPlanting();
	}

	private void TestPlanting()
	{
		GD.Print($"Aktualnie wybrana gleba: {currentSoilType.Name}");
		var oakType = InitialData.PlantTypes.Find(p => p.Id == 2);
		PlacePlant(oakType, new Vector2(100, 100));

		var roseType = InitialData.PlantTypes.Find(p => p.Id == 1);
		PlacePlant(roseType, new Vector2(150, 120));

		var fernType = InitialData.PlantTypes.Find(p => p.Id == 3);
		PlacePlant(fernType, new Vector2(500, 300));
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
		GD.PrintErr($"Posadzono {typeData.Name} na pozycji {position}.");
	}

	public void RunSimulation()
	{
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

	public override void _Input(InputEvent @event)
	{
	}

	private void _on_simulation_button_pressed()
	{
		RunSimulation();
	}
}
