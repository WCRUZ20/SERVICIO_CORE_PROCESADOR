# Mejoras de Arquitectura - Worker SAP → HANA → API

## Resumen de Mejoras Implementadas

Se ha refactorizado la arquitectura del worker para seguir principios de Clean Architecture y separar claramente las responsabilidades de cada componente.

## Arquitectura Propuesta

```
┌─────────────────┐
│     Worker      │  ← Orquesta los dos procesos
└────────┬────────┘
         │
    ┌────┴────┐
    │         │
    ▼         ▼
┌─────────┐ ┌─────────┐
│ Proceso │ │ Proceso │
│   1     │ │   2     │
└─────────┘ └─────────┘
    │            │
    ▼            ▼
SAP (ODBC)   HANA (ODBC)
    │            │
    ▼            ▼
  HANA         API
```

## Componentes Creados

### 1. Interfaces (Application/Interfaces)

#### `ISapOdbcService` (SAP/)
- Interfaz para acceso a SAP mediante ODBC
- Métodos:
  - `GetStockTransfersAsync()` - Obtiene transferencias desde una fecha
  - `GetStockTransferByDocEntryAsync()` - Obtiene una transferencia específica

#### `IHanaRepository` (HANA/)
- Interfaz para repositorio de HANA
- Métodos:
  - `InsertStockTransfersAsync()` - Inserta transferencias en HANA
  - `GetPendingStockTransfersAsync()` - Obtiene transferencias pendientes
  - `MarkAsProcessedAsync()` - Marca como procesada
  - `ExistsAsync()` - Verifica existencia

#### `IApiClient` (API/)
- Interfaz para cliente de API externa
- Métodos:
  - `SendStockTransferAsync()` - Envía una transferencia
  - `SendStockTransfersAsync()` - Envía múltiples transferencias

### 2. Use Cases (Application/UseCases)

#### `ObtenerTransferenciasSapUseCase` (SAP/)
- **Responsabilidad**: Obtener transferencias desde SAP (ODBC) e insertarlas en HANA
- **Flujo**:
  1. Obtiene transferencias desde SAP mediante ODBC
  2. Filtra las que ya existen en HANA
  3. Inserta las nuevas en HANA
  4. Retorna resultado con estadísticas

#### `ProcesarTransferenciasHanaUseCase` (HANA/)
- **Responsabilidad**: Leer transferencias desde HANA y enviarlas a la API
- **Flujo**:
  1. Lee transferencias pendientes desde HANA
  2. Envía cada una a la API externa
  3. Marca como procesadas las exitosas
  4. Retorna resultado con estadísticas

### 3. Implementaciones (Infrastructure)

#### `SapOdbcService` (SAP/)
- Implementación de `ISapOdbcService`
- Usa `System.Data.Odbc` para conexión a SAP
- Mapea resultados de queries a objetos `StockTransferSAP`
- Maneja errores y logging

#### `HanaRepository` (HANA/)
- Implementación de `IHanaRepository`
- Usa `System.Data.Odbc` para conexión a HANA
- Serializa/deserializa objetos a JSON para almacenamiento
- Maneja transacciones y errores

#### `ApiClient` (API/)
- Implementación de `IApiClient`
- Usa `HttpClient` para comunicación con API externa
- Serializa objetos a JSON
- Maneja errores HTTP y retorna resultados detallados

### 4. Configuración (Domain/Configuration)

#### `WorkerSettings`
- Configuración del worker
- Intervalos de ejecución
- Flags de habilitación por proceso

#### `OdbcSettings`
- Configuración de conexiones ODBC
- Separada para SAP y HANA
- Soporta connection string completo o componentes individuales

### 5. Worker Refactorizado

El `Worker` ahora:
- ✅ Separa claramente los dos procesos
- ✅ Usa UseCases en lugar de lógica directa
- ✅ Respeta configuración de intervalos
- ✅ Maneja errores de forma independiente por proceso
- ✅ Logging estructurado y detallado

## Configuración

### appsettings.json

```json
{
  "AppWorkerSettings": {
    "Worker": {
      "isEnableFlag": 1,
      "loopInterval": 5000
    },
    "DocumentStockTransfer": {
      "isEnableFlag": 1,
      "fromDateToTake": "20251218"
    }
  },
  "OdbcSettings": {
    "SAP": {
      "Driver": "{SAP HANA ODBC Driver}",
      "Server": "your-sap-server:30015",
      "Database": "SAP_DB",
      "UserId": "your-user",
      "Password": "your-password"
    },
    "HANA": {
      "Driver": "{SAP HANA ODBC Driver}",
      "Server": "your-hana-server:30015",
      "Database": "HANA_DB",
      "UserId": "your-user",
      "Password": "your-password",
      "TableName": "STOCK_TRANSFERS_HIST"
    }
  },
  "ExternalServices": {
    "Drivin": {
      "BaseUrl": "https://api.drivin.com/"
    }
  }
}
```

## Script SQL

Ver `Scripts/CreateHanaTable.sql` para el script de creación de la tabla en HANA.

## Mejoras Principales

### ✅ Separación de Responsabilidades
- Cada componente tiene una responsabilidad única y clara
- Interfaces bien definidas permiten fácil testing y mocking

### ✅ Clean Architecture
- Domain: Entidades y configuración
- Application: UseCases e interfaces
- Infrastructure: Implementaciones concretas
- Worker: Orquestación

### ✅ Manejo de Errores
- Cada capa maneja sus propios errores
- Logging estructurado en cada nivel
- Resultados con información detallada

### ✅ Configuración Flexible
- Configuración por appsettings.json
- Soporte para connection strings completos o componentes
- Flags de habilitación por proceso

### ✅ Escalabilidad
- Fácil agregar nuevos procesos
- Fácil cambiar implementaciones (ej: de ODBC a otro método)
- UseCases reutilizables

### ✅ Mantenibilidad
- Código bien documentado
- Estructura clara y predecible
- Fácil de entender y modificar









