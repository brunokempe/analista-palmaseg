using AnalistaPalmaseg.Core.Data;
using AnalistaPalmaseg.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AnalistaPalmaseg.Core.Services;

public class LeadService(IDbContextFactory<AppDbContext> contextFactory)
{
    public async Task<List<Lead>> GetTodosAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Leads.AsNoTracking().OrderByDescending(l => l.CriadoEm).ToListAsync();
    }

    public async Task<Lead> SalvarAsync(Lead lead)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        if (lead.Id == 0)
        {
            context.Leads.Add(lead);
        }
        else
        {
            var tracked = await context.Leads.FindAsync(lead.Id);
            if (tracked != null)
                context.Entry(tracked).CurrentValues.SetValues(lead);
            else
                context.Leads.Update(lead);
        }
        await context.SaveChangesAsync();
        return lead;
    }

    public async Task ExcluirAsync(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var lead = await context.Leads.FindAsync(id);
        if (lead != null)
        {
            context.Leads.Remove(lead);
            await context.SaveChangesAsync();
        }
    }
}
