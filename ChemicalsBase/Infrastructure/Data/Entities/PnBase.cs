using System.Reflection;

namespace ChemicalsBase.Infrastructure.Data.Entities;

public class PnBase : BaseEntity
{
    public async Task Create(ChemicalsDbContext dbContext)
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Version = 1;
        WorkflowState = Constants.Constants.WorkflowStates.Created;

        await dbContext.AddAsync(this);
        await dbContext.SaveChangesAsync();

        var res = MapVersion(this);
        if (res != null)
        {
            await dbContext.AddAsync(res);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task Update(ChemicalsDbContext dbContext)
    {
        await UpdateInternal(dbContext);
    }

    public async Task Delete(ChemicalsDbContext dbContext)
    {
        await UpdateInternal(dbContext, Constants.Constants.WorkflowStates.Removed);
    }

    private async Task UpdateInternal(ChemicalsDbContext dbContext, string state = null)
    {
        if (state != null)
        {
            WorkflowState = state;
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            Version += 1;
            UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            var res = MapVersion(this);
            if (res != null)
            {
                await dbContext.AddAsync(res);
                await dbContext.SaveChangesAsync();
            }
        }
    }

    private object MapVersion(object obj)
    {
        var type = obj.GetType().UnderlyingSystemType;
        var className = type.Name;
        var name = obj.GetType().FullName + "Version";
        var resultType = Assembly.GetExecutingAssembly().GetType(name);
        if (resultType == null)
            return null!;

        var returnObj = Activator.CreateInstance(resultType);

        var curreList = obj.GetType().GetProperties();
        foreach(var prop in curreList)
        {
            if (!prop.PropertyType.FullName!.Contains("ChemicalsBase.Infrastructure.Data.Entities"))
            {
                try
                {
                    var propName = prop.Name;
                    if (propName != "Id")
                    {
                        var propValue = prop.GetValue(obj);
                        var targetType = returnObj!.GetType();
                        var targetProp = targetType.GetProperty(propName);

                        targetProp!.SetValue(returnObj, propValue, null);
                    } else {
                        var propValue = prop.GetValue(obj);
                        var targetType = returnObj!.GetType();
                        var targetProp = targetType.GetProperty($"{className}Id");

                        targetProp!.SetValue(returnObj, propValue, null);
                    }
                }
                catch (Exception ex)
                {
                    // Console.WriteLine($"{ex.Message} - Property:{prop.Name} probably not found on Class {returnObj.GetType().Name}");
                }
            }
        }

        return returnObj!;
    }
}