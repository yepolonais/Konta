using Konta.Domain.Categories;

namespace Konta.Application.Imports;

public sealed record OperationKey(OperationType Type, string Key);
