namespace EVA.Domain.Exceptions;

public sealed class NotFoundException(string entityName, object key)
    : Exception($"Registro '{entityName}' com identificador '{key}' não foi encontrado.");
