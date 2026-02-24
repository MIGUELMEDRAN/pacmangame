# PacmanGame

Juego tipo **Pac-Man** construido en **.NET 8** con **Avalonia UI**.

Este documento describe de forma integral la arquitectura, estructura de carpetas, flujo de ejecución, componentes del código, recursos, scripts de validación y guía de mantenimiento.

---

## 1) Objetivo del proyecto

El proyecto implementa una experiencia arcade inspirada en Pac-Man con:

- Menú principal.
- Juego por niveles (3 niveles).
- Laberintos dinámicos por nivel.
- Puntos normales y power-ups.
- Fantasmas con comportamiento de persecución/evasión.
- Sistema de puntajes persistente en JSON.
- Efectos de audio para acciones clave.

---

## 2) Stack tecnológico

- **Lenguaje:** C#
- **Framework:** .NET 8
- **UI Desktop:** Avalonia 11
- **Audio:** LibVLCSharp + VideoLAN.LibVLC.Windows
- **Persistencia:** JSON (`System.Text.Json`)

Dependencias definidas en `PACMAN/PACMAN.csproj`.

---

## 3) Estructura del repositorio

```text
pacmangame/
├── pacmangame.sln
├── README.md
├── DiagramaDeClases.png
├── scripts/
│   ├── verify-build.sh
│   └── verify-build.ps1
└── PACMAN/
    ├── PACMAN.csproj
    ├── Program.cs
    ├── App.axaml
    ├── App.axaml.cs
    ├── app.manifest
    ├── Audio/
    │   └── AudioPlayer.cs
    ├── Assets/
    │   ├── Data/
    │   │   └── score.json
    │   ├── Images/
    │   │   ├── background.jpg
    │   │   ├── blinky.png
    │   │   ├── cherrys.jpg
    │   │   ├── inky.png
    │   │   ├── pacman1.png
    │   │   └── pacman2.png
    │   └── Sound/
    │       ├── chomp.mp3
    │       └── dead.wav
    ├── Models/
    │   ├── Dots.cs
    │   ├── Ghost.cs
    │   ├── Pacman.cs
    │   └── Score.cs
    ├── Services/
    │   └── ScoreService.cs
    ├── ViewModels/
    │   └── GameViewModel.cs
    └── Views/
        ├── MainWindow.axaml
        ├── MainWindow.axaml.cs
        ├── MainMenuView.axaml
        ├── MainMenuView.axaml.cs
        ├── GameView.axaml
        ├── GameView.axaml.cs
        ├── ScoreBoardWindow.axaml
        └── ScoreBoardWindow.axaml.cs
```

---

## 4) Arquitectura general

El proyecto sigue una organización por capas ligera:

- **Views (XAML + code-behind):** Composición visual y eventos de UI.
- **ViewModel (`GameViewModel`):** Lógica principal de juego, colisiones, niveles, IA y timers.
- **Models:** Entidades de dominio (`Pacman`, `Ghost`, `Dots`, `Score`).
- **Services:** Persistencia de puntajes (`ScoreService`).
- **Audio:** Reproducción de sonidos (`AudioPlayer`).

> Aunque no es MVVM estricto con binding completo, `GameViewModel` centraliza la lógica de dominio del gameplay y las vistas funcionan como host visual + interacción de usuario.

---

## 5) Flujo de ejecución de la app

1. `Program.Main(...)` configura Avalonia y arranca la app desktop.
2. `App.OnFrameworkInitializationCompleted()` instancia `MainWindow`.
3. `MainWindow` carga inicialmente `MainMenuView`.
4. Desde el menú:
   - **Iniciar partida:** cambia a `GameView` y reinicia score del jugador.
   - **Ver puntuaciones:** abre `ScoreBoardWindow` con Top 10.
   - **Salir:** cierra ventana.
5. `GameView` crea `AudioPlayer` y `GameViewModel` al entrar al árbol visual.
6. `GameViewModel` inicializa nivel, timers, entidades y eventos de puntuación.
7. Al terminar (victoria o game over), se muestra overlay y se puede reintentar o volver al menú.

---

## 6) Componentes documentados (código)

### 6.1 Entrada y bootstrap

- **`Program.cs`**
  - Punto de entrada (`Main`).
  - Configuración de Avalonia (`BuildAvaloniaApp`).

- **`App.axaml` / `App.axaml.cs`**
  - Declara tema Fluent.
  - Define `MainWindow` como ventana principal para desktop.

### 6.2 Ventana contenedora y navegación

- **`Views/MainWindow.axaml`**
  - Define estilos base de botones/textos.
  - Aloja un `ContentControl` (`MainContent`) para navegación entre vistas.

- **`Views/MainWindow.axaml.cs`**
  - Métodos de navegación:
    - `LoadGameView()`
    - `LoadMainMenuView()`

### 6.3 Menú principal y ranking

- **`Views/MainMenuView.axaml`**
  - Botones: iniciar partida, ver puntuaciones, salir.

- **`Views/MainMenuView.axaml.cs`**
  - Inicializa archivo de scores (`ScoreService.Initialize`).
  - Inicia juego y reinicia score del jugador (`ResetPlayerScore("Jugador")`).
  - Carga Top 10 y abre `ScoreBoardWindow`.
  - Muestra mensajes de error con ventana modal auxiliar.

- **`Views/ScoreBoardWindow.axaml(.cs)`**
  - Ventana visual para mostrar lista formateada de puntajes.

### 6.4 Vista de juego

- **`Views/GameView.axaml`**
  - HUD: vidas, score, nivel.
  - Botones: reiniciar y volver al menú.
  - `Canvas` de juego (600x600): Pac-Man, fantasmas y boss.
  - Overlay para estado final (victoria/game over).

- **`Views/GameView.axaml.cs`**
  - Crea y libera `AudioPlayer` y `GameViewModel` según ciclo de vida visual.
  - Reenvía teclado a `GameViewModel`.
  - Exposición de referencias visuales para fantasmas/boss.
  - Métodos de actualización de HUD y overlay.

### 6.5 Lógica central de gameplay

- **`ViewModels/GameViewModel.cs`**

  Responsabilidades principales:

  - Estado de partida:
    - score, vidas, nivel, power mode, boss, fin de juego.
  - Movimiento de Pac-Man:
    - dirección actual/deseada.
    - giro con alineación a celdas (`SnapToCell`, `TrySnapToLane`).
    - validación de colisiones con muros (`IsSpaceFree`).
  - Control de timers:
    - animación boca.
    - movimiento de Pac-Man.
    - movimiento de fantasmas.
    - duración de power-up.
  - Construcción del nivel:
    - laberinto por configuración.
    - puntos y power-ups.
    - spawn de fantasmas.
  - IA de fantasmas:
    - persecución usando BFS para elegir siguiente paso.
    - comportamiento evasivo en power mode (se alejan de Pac-Man).
  - Colisiones:
    - Pac-Man vs dots/power-ups.
    - Pac-Man vs fantasmas (pierde vida o come fantasma vulnerable).
  - Progresión:
    - avanza al siguiente nivel al limpiar tablero.
    - victoria al completar nivel 3.
    - game over al perder todas las vidas.

  Configuración destacada:

  - Tablero: 600x600.
  - Celda lógica: 10 px (movimiento / cuantización).
  - 3 niveles con:
    - colores de tema.
    - cantidad/spawn de fantasmas.
    - layout de muros específico.

### 6.6 Modelos de dominio

- **`Models/Pacman.cs`**
  - Posición, paso, alternancia de sprites (boca abierta/cerrada), rotación y bounds.

- **`Models/Ghost.cs`**
  - Posición, velocidad, estado (`IsVulnerable`, `IsActive`, `IsBoss`) y evento `PositionChanged`.

- **`Models/Dots.cs`**
  - Genera dots evitando muros.
  - Calcula nodos alcanzables por BFS desde spawn para evitar puntos aislados.
  - Crea power-ups por nivel en nodos alcanzables cercanos.
  - Detecta colisiones con Pac-Man y emite eventos:
    - `OnScore`
    - `OnPowerUpCollected`
    - `OnBoardCleared`

- **`Models/Score.cs`**
  - POCO con `PlayerName` y `HighScore`.

### 6.7 Servicios

- **`Services/ScoreService.cs`**
  - Inicializa archivo `Assets/Data/score.json`.
  - Carga y guarda lista de puntajes en JSON.
  - Ordena por score desc + nombre asc.
  - Actualiza mejor score del jugador actual.
  - Reinicia score de un jugador.

### 6.8 Audio

- **`Audio/AudioPlayer.cs`**
  - Inicializa LibVLC.
  - Reproduce sonidos de:
    - comida/power-up/level-up (`chomp.mp3`).
    - muerte (`dead.wav`).
  - Evita reproducir sonidos de juego tras game over.
  - Dispone correctamente recursos multimedia.

---

## 7) Recursos y empaquetado

### Recursos embebidos (AvaloniaResource)

Incluye imágenes y sonidos para carga en runtime desde XAML/rutas relativas.

### Recursos copiados a output

`score.json` y assets relevantes se copian al directorio de salida para ejecución y persistencia local.

Configurado en `PACMAN/PACMAN.csproj`.

---

## 8) Compilación y ejecución

### Requisitos

- .NET SDK 8.0+

### Comandos base

```bash
dotnet restore
dotnet build -c Release
```

### Ejecutar aplicación

```bash
dotnet run --project PACMAN/PACMAN.csproj
```

---

## 9) Verificación de calidad (scripts)

Se incluyen scripts para validar compilación y bloquear warnings considerados críticos en este proyecto:

- `CS8600`
- `CS8604`
- `AVLN3001`

### Linux/macOS

```bash
./scripts/verify-build.sh Release
```

### Windows PowerShell

```powershell
./scripts/verify-build.ps1 -Configuration Release
```

Si algún warning bloqueado aparece, el script finaliza con error.

---

## 10) Persistencia de puntajes

Archivo usado:

- `PACMAN/Assets/Data/score.json`

Comportamiento:

- Se crea automáticamente si no existe.
- Mantiene el mejor score del jugador `Jugador` durante la sesión de juego.
- El menú puede mostrar Top 10 ordenado.

---

## 11) Controles del juego

- Movimiento: `W`, `A`, `S`, `D` o flechas.
- Reiniciar partida: botón `Reiniciar partida`.
- Volver al menú: botón `Volver al menú`.

---

## 12) Consideraciones de diseño técnico

- Movimiento basado en grilla para facilitar navegación por pasillos.
- Snap de carril para mejorar respuesta al cambiar de dirección.
- BFS para IA de fantasmas y para cálculo de puntos alcanzables.
- Separación por capas para mantenimiento incremental.
- Uso de timers independientes para animación y lógica.

---

## 13) Posibles mejoras futuras

- Sistema multi-jugador real en ranking (captura de nombre).
- Más tipos de fantasmas con IA diferenciada.
- Sonidos y música por estado de juego.
- Ajuste de dificultad progresiva (velocidades/tiempos).
- Tests automatizados para servicios y lógica pura.
- Localización i18n para UI.

---

## 14) Solución de problemas

- **No hay audio:** verificar instalación/compatibilidad de runtime VLC y rutas de salida de assets.
- **No aparecen imágenes:** confirmar que los `AvaloniaResource` están incluidos y build limpio.
- **No persiste score:** revisar permisos de escritura en carpeta de salida (`Assets/Data/score.json`).

---

## 15) Licencia

Sin licencia explícita en este repositorio. Si se publicará de forma abierta, se recomienda añadir un archivo `LICENSE`.
