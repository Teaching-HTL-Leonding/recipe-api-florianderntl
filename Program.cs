using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

int lastId = 0;
var recipes = new ConcurrentDictionary<int, Recipe>();

app.MapGet("/recipes", (string? title, string? ingredientName) =>
{
    var recipesResult = new List<Recipe>();

    string titleToLower = title == null ? "" : title.ToLower();
    var ingredient = ingredientName == null ? null : new Ingredient(ingredientName);

    foreach (var recipe in recipes.Values)
    {
        if ((recipe.Title.ToLower().Contains(titleToLower) || recipe.Description.ToLower().Contains(titleToLower)) &&
            (ingredient == null || recipe.Ingredients != null && recipe.Ingredients.Contains(ingredient)))
        {
            recipesResult.Add(recipe);
        }
    }

    return recipesResult;
});

app.MapPost("/recipes", (RecipeDto recipeDto) =>
{
    var id = Interlocked.Increment(ref lastId);
    var recipe = new Recipe(id, recipeDto);

    if (!recipes.TryAdd(id, recipe))
    {
        // should never happen
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }

    return Results.Created($"/recipe/{id}", recipe);
});

app.MapPut("/recipes/{id}", (int id, RecipeDto recipeDto) => 
{
    if (!recipes.TryGetValue(id, out Recipe? recipe))
    {
        return Results.NotFound();
    }

    var newRecipe = new Recipe(id, recipeDto);

    if (!recipes.TryUpdate(id, newRecipe, recipe))
    {
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }

    return Results.Ok(newRecipe);
});

app.MapDelete("/recipes/{id}", (int id) =>
{
    if (!recipes.Remove(id, out Recipe? recipe))
    {
        return Results.NotFound();
    }

    return Results.Ok(recipe);
});

app.Run();


class Recipe
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public List<Ingredient>? Ingredients { get; set; }
    public String Description { get; set; } = "";
    public string? ImgUrl { get; set; }

    public Recipe(int id, RecipeDto recipeDto)
    {
        this.Id = id;
        this.Title = recipeDto.Title;
        this.Ingredients = recipeDto.Ingredients;
        this.Description = recipeDto.Description;
        this.ImgUrl = recipeDto.ImgUrl;
    }
}

class Ingredient : IEquatable<Ingredient>
{
    public string Name { get; set; } = "";
    public string UnitOfMeasure { get; set; } = "";
    public int Quantity { get; set; }

    public Ingredient()
    {
        
    }

    public Ingredient(string name)
    {
        this.Name = name;
    }

    public bool Equals(Ingredient? other)
    {
        if (other == null)
        {
            return false;
        }
        
        return this.Name == other.Name;
    }

    // override object.GetHashCode
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

record RecipeDto(int? Id, string Title, List<Ingredient>? Ingredients, string Description, string? ImgUrl);
