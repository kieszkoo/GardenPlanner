using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Plant : Area2D
{
	public PlantTypeData TypeData { get; private set; }
	public float CurrentHeight { get; private set; }
	public float CurrentRadius { get; private set; }
	public int AgeMonths { get; private set; }
	
	private int DbId { get; set; }
	private const float PixelsPerMeter = 50.0f;

	private List<Plant> collidingNeighbors = new List<Plant>();
	
	private CollisionShape2D collisionShape;
	private Sprite2D sprite;
	private Label infoLabel;
	
	public override void _Ready()
	{
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		sprite = GetNode<Sprite2D>("Sprite2D");

		infoLabel = GetNodeOrNull <Label>("InfoLabel");

		if (collisionShape == null) GD.PrintErr("Brak CollisionShape2D w Plant");
		if (sprite == null) GD.PrintErr("Brak Sprite2D w Plant");
		
		ZIndex = 10;

		if (infoLabel != null) 
		{
			infoLabel.Visible = false;
			infoLabel.ZIndex = 200; 

			var style = new StyleBoxFlat();
			style.BgColor = new Color(0.05f, 0.05f, 0.05f, 0.85f); 
			
			style.CornerRadiusTopLeft = 5;
			style.CornerRadiusTopRight = 5;
			style.CornerRadiusBottomRight = 5;
			style.CornerRadiusBottomLeft = 5;
			
			style.ExpandMarginLeft = 8;
			style.ExpandMarginRight = 8;
			style.ExpandMarginTop = 4;
			style.ExpandMarginBottom = 4;
			
			infoLabel.AddThemeStyleboxOverride("normal", style);
		}

		AreaEntered += OnAreaEntered;
		AreaExited += OnAreaExited;

		MouseEntered += OnMouseEntered;
		MouseExited += OnMouseExited;
	}
	
	public override void _Process(double delta)
	{
		if (infoLabel != null && infoLabel.Visible)
		{
			var camera = GetViewport().GetCamera2D();
			if (camera != null)
			{
				infoLabel.Scale = Vector2.One / camera.Zoom;
			}
		}
	}

	public void Initialize(PlantTypeData data)
	{
		TypeData = data;
		AgeMonths = 0;
		CurrentHeight = 0.1f;
		CurrentRadius = data.CanopyRadius * 0.01f;

		if (ResourceLoader.Exists(data.TexturePath))
		{
			sprite.Texture = (Texture2D)ResourceLoader.Load(data.TexturePath);
			sprite.Scale = new Vector2(0.1f, 0.1f);
			sprite.ShowBehindParent = true;
		}
		else
		{
			GD.PrintErr($"Nie znaleziono tekstury dla rosliny: {data.Name} pod sciezka: {data.TexturePath}");
		}
		UpdateVisuals();
	}

	public override void _Draw()
	{
		if (CurrentRadius > 0)
		{
			float pixelRadius = CurrentRadius * PixelsPerMeter;

			Color fillColor;
			Color outlineColor;
			if (collidingNeighbors.Count > 0)
			{
				fillColor = new Color(0.9f, 0.2f, 0.2f, 0.3f);		//Kolizja - kolor czerwony
				outlineColor = Colors.DarkRed;
			}
			else
			{
				fillColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);		// Brak kolizji - kolor zielony
				outlineColor = Colors.DarkGreen;
			}
			DrawCircle(Vector2.Zero, pixelRadius, fillColor);
			DrawArc(Vector2.Zero, pixelRadius, 0, Mathf.Tau, 64, outlineColor, 2.0f);
		}
		
	}

	private void UpdateVisuals()
	{
		float pixelRadius = CurrentRadius * PixelsPerMeter;
		if (collisionShape.Shape is CircleShape2D circle)
		{
			circle.Radius = pixelRadius;
		}
		
		if (infoLabel != null && infoLabel.Visible)
		{
			UpdateLabelText();
		}
		// float scaleFactor = CurrentRadius / (TypeData.MaxWidth / 2.0f);
		// sprite.Scale = new Vector2(scaleFactor, scaleFactor);
		QueueRedraw();
	}

	private void OnMouseEntered()
	{
		if (infoLabel != null)
		{
			UpdateLabelText();
			infoLabel.Visible = true;
			
			_Process(0);

			if (sprite != null) sprite.Modulate = new Color(1.2f, 1.2f, 1.2f);
		}
	}

	private void OnMouseExited()
	{
		if (infoLabel != null)
		{
			infoLabel.Visible = false;

			if (sprite != null) sprite.Modulate = Colors.White;
		}
	}

	private void UpdateLabelText()
	{
		infoLabel.Text = $"{TypeData.Name}\n{AgeMonths} msc\n W {CurrentHeight:F1} m, Sz {2 * CurrentRadius:F1} m";
		
		float pixelRadius = CurrentRadius * PixelsPerMeter;
		infoLabel.Position = new Vector2(-20, -pixelRadius -30);
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
				QueueRedraw();
			}
		}
	}
	
	private void OnAreaExited(Area2D area)
	{
		if (area is Plant neighbor)
		{
			collidingNeighbors.Remove(neighbor);
			QueueRedraw();
		}
	}
	
	public void LoadState(int ageMonths, float currentHeight, float currentRadius)
	{
		AgeMonths = ageMonths;
		CurrentHeight = currentHeight;
		CurrentRadius = currentRadius;
		UpdateVisuals();
	}
}