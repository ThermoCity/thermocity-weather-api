namespace ThermoCity.Weather.Application.Exceptions;

public sealed class InvalidInputException(string message) : Exception(message);
