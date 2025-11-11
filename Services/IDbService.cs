using System;

namespace COMP4952_Sockim.Services;

public interface IDbService
{
    private async Task<IDbOperationResult> DbOperation()
    {
        throw new NotImplementedException("Db context operation not implemented.");
    }
}
