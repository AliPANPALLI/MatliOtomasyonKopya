namespace ProjectTracking.Application.Exceptions;

public sealed class ProjeTakipValidationException(string message) : Exception(message);
public sealed class ProjeTakipNotFoundException(string message) : Exception(message);
public sealed class ProjeTakipConflictException(string message) : Exception(message);
public sealed class ProjeTakipForbiddenException(string message) : Exception(message);
