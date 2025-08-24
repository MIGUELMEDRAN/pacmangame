using System;
using Avalonia;

namespace PACMAN.Models;

/// <summary>
/// Representa un fantasma.
/// </summary>
public class Ghost
{
    /// <summary>
    /// Obtiene la coordenada X actual del fantasma.
    /// </summary>
    public double X { get; private set; }
    
    /// <summary>
    /// Obtiene la coordenada Y actual del fantasma.
    /// </summary>
    public double Y { get; private set; }
    
    /// <summary>
    /// Obtiene o estable el ancho del fantasma.
    /// </summary>
    public double Width { get; set; } = 30;
    
    /// <summary>
    /// Obtiene o establece la altura del fantasma.
    /// </summary>
    public double Height { get; set; } = 30;
    
    /// <summary>
    /// Obtiene o establece la velocidad de movimiento del fantasma.
    /// </summary>
    public double Speed { get; set; } = 5;
    
    /// <summary>
    /// Evento que se dispara cuando la posicion del fantasma cambia.
    /// </summary>
    public event Action<double, double> PositionChanged;

    /// <summary>
    /// Inicializa una nueva isntancia de la clase <see cref="Ghost"/> con una posicion incial.
    /// </summary>
    /// <param name="StartX"></param>
    /// <param name="StartY"></param>
    public Ghost(double StartX, double StartY)
    {
        X = StartX;
        Y = StartY;
    }

    /// <summary>
    /// Mueve el fantasma a una nueva posicion y dispara el evento <see cref="PositionChanged"/>.
    /// </summary>
    /// <param name="newX">Nueva coordenada X.</param>
    /// <param name="newY">Nueva coordenada Y.</param>
    public void Move(double newX, double newY)
    {
        X = newX;
        Y = newY;
        PositionChanged?.Invoke(X, Y);
    }

    /// <summary>
    /// Obtiene los limites rectangulares del fantasma.
    /// </summary>
    /// <returns>Un <see cref="Rect"/> que representa la posicion y el tamaño del fantasma.</returns>
    public Rect GetBounds()
    {
        return new Rect(X, Y, Width, Height);
    }
}