public class TransactionLogger
{
    private readonly string _logDirectory;

    public TransactionLogger(string logDirectory = "Logs")
    {
        _logDirectory = logDirectory;

        // Crear el directorio si no existe
        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }
    }

    public async Task LogTransaction(string message, bool isSuccess)
    {
        try
        {
            var timestamp = DateTime.Now;
            var fileName = $"transacciones_{timestamp:yyyyMMdd}.log";
            var logPath = Path.Combine(_logDirectory, fileName);

            var logEntry = $"{timestamp:yyyy-MM-dd HH:mm:ss.fff} | " +
                        $"Estado: {(isSuccess ? "ÉXITO" : "ERROR")} | " +
                        $"Mensaje: {message}{Environment.NewLine}";

            await File.AppendAllTextAsync(logPath, logEntry);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al escribir log: {ex.Message}");
        }
    }
}