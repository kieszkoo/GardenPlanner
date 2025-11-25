using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Plant : Area2D
{
	public PlantTypeData TypeData { get; private set; }
	public float CurrentHeight { get; private set; }
	public float CurrentRadius { get; private set; }
	public int AgeMonths { get; private set; }

	private List<Plant> collidingNeighbors = new List<Plant>();
	
	private CollisionShape2D collisionShape;
	private Sprite2D sprite;
	
	public override void _Ready()
	{
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		sprite = GetNode<Sprite2D>("Sprite2D");

		AreaEntered += OnAreaEntered;
		AreaExited += OnAreaExited;
	}

	public void Initialize(PlantTypeData data)
	{
		TypeData = data;
		AgeMonths = 0;
		CurrentHeight = 0.1f;
		CurrentRadius = data.CanopyRadius * 0.1f;

		if (ResourceLoader.Exists(data.TexturePath))
		{
			sprite.Texture = (Texture2D)ResourceLoader.Load(data.TexturePath);
		}
		else
		{
			GD.PrintErr($"Nie znaleziono tekstury dla rosliny: {data.Name} pod sciezka: {data.TexturePath}");
		}
		
		if (collisionShape.Shape is CircleShape2D circle)
		{
			// Ustawienie początkowego kształtu
			circle.Radius = CurrentRadius;
		}

		UpdateVisuals();
	}

	private void UpdateVisuals()
	{
		if (collisionShape.Shape is CircleShape2D circle)
		{
			circle.Radius = CurrentRadius;
		}

		float scaleFactor = CurrentRadius / (TypeData.MaxWidth / 2.0f);
		sprite.Scale = new Vector2(scaleFactor, scaleFactor);
	}

	public void SimulateMonth(float growthFactorSoil, float sunLevel)
	{
		AgeMonths++;
		float collisionFactor = CalculateCollisionFactor();
		float baseGrowth = TypeData.BaseGrowthRate;
		float deltaSize = baseGrowth * growthFactorSoil * collisionFactor * sunLevel;

		CurrentHeight = Mathf.Min(CurrentHeight + deltaSize, TypeData.MaxHeight);

		float targetRadius = CurrentHeight / TypeData.MaxHeight * (TypeData.MaxWidth / 2.0f);
		
		CurrentRadius = Mathf.Min(CurrentRadius + deltaSize, targetRadius);
		UpdateVisuals();

		GD.Print($"Symulacja: {TypeData.Name} Wiek: {AgeMonths} mies., W: {CurrentHeight:F2}m, R: {CurrentRadius:F2}, Kolizja: {collisionFactor:F2}, Światło: {sunLevel:F2}");
	}


	private float CalculateCollisionFactor()
	{
		if (collidingNeighbors.Count == 0)
		{
			return 1.0f;
		}

		float maxImpact = 0.75f; //Maksymalne spowolnienie 75%
		float factor = 1.0f - maxImpact * (1.0f - Mathf.Pow(0.8f, collidingNeighbors.Count));
		return Mathf.Max(0.25f, factor);
	}

	private void OnAreaEntered(Area2D area)
	{
		if (area is Plant neighbor && neighbor != this)
		{
			if (!collidingNeighbors.Contains(neighbor))
			{
				collidingNeighbors.Add(neighbor);
			}
		}
	}
	
	private void OnAreaExited(Area2D area)
	{
		if (area is Plant neighbor)
		{
			collidingNeighbors.Remove(neighbor);
		}
	}
}
