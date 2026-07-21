namespace AnalistaPalmaseg.Core.Data;

public class DatabaseInitializer(AppDbContext context)
{
    public void Initialize()
    {
        context.Database.EnsureCreated();
    }
}
