# WebHook
# Webhook para Transacciones de PayPal

Este proyecto es un webhook desarrollado en ASP .NET 8.0 para recibir y procesar las notificaciones de transacciones realizadas a través de PayPal. Está diseñado para integrarse con tu sistema y manejar pagos de manera segura utilizando tecnologías modernas de autenticación y manejo de base de datos.

## Características

- **Recepción de Webhooks**: El webhook recibe notificaciones de PayPal sobre transacciones y las procesa automáticamente.
- **Autenticación**: Utiliza JWT Bearer para asegurar las conexiones.
- **Integración con Base de Datos**: Utiliza Entity Framework Core para manejar las transacciones y otros datos persistentes en una base de datos SQL Server.
- **Seguridad**: El uso de BCrypt para el hashing de contraseñas asegura la integridad de los datos sensibles.
- **Documentación API**: Generación automática de documentación OpenAPI para las rutas del API.

## Requisitos

- .NET 8.0
- SQL Server (para almacenamiento de transacciones)
- Cuenta de desarrollador de PayPal (para configurar las credenciales de PayPal)

## Instalación

1. **Clona el repositorio**:

   ```bash
   git clone https://github.com/sorihuen/WebhookAPi.git
   cd prueba

## Restaura los paquetes Nuget y ejecuta 
- dotnet restore
- dotnet run


   
