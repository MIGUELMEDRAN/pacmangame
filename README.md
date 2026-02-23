# PacmanGame

Pac-Man style game built with .NET 8 and Avalonia UI.

## Requisitos

- .NET SDK 8.0+

## Build rápido

```bash
dotnet restore
dotnet build -c Release
```

## Verificación recomendada (solución definitiva para warnings reportados)

Se añadieron scripts para validar build y bloquear los warnings que rompían la calidad del proyecto:

- `CS8600`
- `CS8604`
- `AVLN3001`

### Windows (PowerShell)

```powershell
./scripts/verify-build.ps1 -Configuration Release
```

### Linux/macOS (bash)

```bash
./scripts/verify-build.sh Release
```

Si alguno de esos warnings vuelve a aparecer, el script termina con error para que se corrija antes de mergear.
