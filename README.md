# GreenCoin Movil

Aplicación móvil para el sistema de reciclaje GreenCoin, desarrollada con .NET MAUI.

## 🚀 Configuración

### 1. Variables de Entorno

La aplicación utiliza variables de entorno para configurar la URL de la API. Esto permite cambiar fácilmente entre diferentes entornos sin recompilar el código.

#### Configuración Básica:

1. Copia el archivo `.env.example` a `.env`:
   ```bash
   cp .env.example .env
   ```

2. Edita el archivo `.env` con tu configuración:
   ```env
   API_BASE_URL=http://10.0.2.2:8080
   ```

#### URLs de API por Plataforma:

- **Android Emulator**: `http://10.0.2.2:8080`
- **iOS Simulator**: `http://localhost:8080`
- **Dispositivo físico**: `http://[IP_DE_TU_MAQUINA]:8080`

#### Configuración de Variables de Entorno:

**Windows (PowerShell):**
```powershell
$env:API_BASE_URL="http://10.0.2.2:8080"
```

**Windows (CMD):**
```cmd
set API_BASE_URL=http://10.0.2.2:8080
```

**macOS/Linux:**
```bash
export API_BASE_URL=http://10.0.2.2:8080
```

**Visual Studio (para desarrollo):**
- Ve a `Project Properties > Debug > Environment variables`
- Agrega: `API_BASE_URL=http://10.0.2.2:8080`

### 2. Backend

Asegúrate de que el backend esté corriendo en el puerto 8080. Los endpoints principales utilizados son:

- `POST /api/auth/login` - Autenticación
- `GET /api/reciclajes/mis-reciclajes` - Historial completo
- `GET /api/reciclajes/estado-validaciones` - Estado de validaciones
- `POST /api/reciclajes/registrar-con-foto` - Registrar reciclaje con foto

## 🛠️ Desarrollo

### Requisitos:
- .NET 9.0
- Visual Studio 2022 o Visual Studio Code
- Android SDK (para desarrollo Android)
- Xcode (para desarrollo iOS en macOS)

### Ejecutar:
```bash
dotnet build
dotnet run
```

## 📱 Características

- ✅ Autenticación de usuarios
- ✅ Registro de reciclajes con foto
- ✅ Historial completo de reciclajes
- ✅ Sistema de puntos y logros
- ✅ Interfaz adaptativa para móvil

## 🔧 Solución de Problemas

### Error de conexión a la API:
1. Verifica que el backend esté corriendo
2. Confirma la variable de entorno `API_BASE_URL`
3. Para Android Emulator, usa `10.0.2.2`
4. Para iOS Simulator, usa `localhost`

### Error "Forbidden" en el historial:
- Verifica que el token JWT sea válido
- Confirma que el usuario esté autenticado

### Error "Material desconocido":
- Verifica que el endpoint `/api/reciclajes/mis-reciclajes` esté funcionando
- Confirma que la estructura de respuesta del API coincida con `ReciclajeDTO`