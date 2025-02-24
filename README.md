# 🌐 Webhook para Transacciones de PayPal
Este proyecto es un webhook desarrollado en ASP.NET 8.0 para recibir y procesar notificaciones de transacciones realizadas a través de PayPal. Está diseñado para integrarse fácilmente con tu sistema y manejar pagos de manera segura, utilizando tecnologías modernas de autenticación y gestión de bases de datos.

## ✨ Características
-✅ Recepción de Webhooks – Recibe notificaciones de PayPal sobre transacciones y las procesa automáticamente.
-🔐 Autenticación Segura – Utiliza JWT Bearer para garantizar conexiones seguras.
-💾 Gestión de Base de Datos – Usa Entity Framework Core para almacenar transacciones en SQL Server.
-🔑 Seguridad Reforzada – Implementa BCrypt para el hashing de contraseñas, asegurando la integridad de los datos.
-📄 Documentación API – Generación automática de documentación OpenAPI para facilitar la integración.

## ⚙️ Requisitos
Antes de ejecutar este proyecto, asegúrate de tener instalados los siguientes requisitos:

-✅ .NET 8.0
-✅ SQL Server (para almacenar transacciones)
-✅ Cuenta de desarrollador de PayPal (para configurar credenciales de PayPal)


## 🚀 Instalación y Ejecución

1. **Clona el repositorio**:

   ```bash
   git clone https://github.com/sorihuen/WebhookAPi.git
   cd prueba

2. **Restaura los paquetes Nuget y ejecuta**:

   ```bash
   dotnet restore

3. **Ejecuta la aplicación**:

   ```bash
   dotnet run


   
