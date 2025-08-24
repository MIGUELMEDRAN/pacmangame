using Avalonia.Controls;
using Avalonia.Media;
using Avalonia;

namespace PACMAN.Models;

/// <summary>
/// Representa el personaje principal del juego Pacman, incluye posicion, estado y comportamiento
/// </summary>
public class Pacman
{
    /// <summary>
    /// Posicion X de pacman
    /// </summary>
    public double X { get; set; } = 40;
    
    /// <summary>
    /// posicion Y de pacman
    /// </summary>
    public double Y { get; set; } = 140;
    
    /// <summary>
    /// Cantidad de movimeientos
    /// </summary>
    public double MoveStep { get; set; } = 10;
    
    /// <summary>
    /// Indica si la boca esta abierta
    /// </summary>
    public bool IsMouthOpen { get; set; } = true;
    
    /// <summary>
    /// Imagen de pacman con la boca abierta
    /// </summary>
    public Image OpenImage { get; set; }
    
    /// <summary>
    /// Imagen de pacman con la boca cerrada
    /// </summary>
    public Image CloseImage { get; set; }

    /// <summary>
    /// /Inicializa una nueva instancia de la clase <see cref="Pacman"/> con las imagenes
    /// </summary>
    /// <param name="openImage">Imagen de Pacman con la boca abierta</param>
    /// <param name="closeImage">Imagen de Pacman con la boca cerrada</param>
    public Pacman(Image openImage, Image closeImage)
    {
        OpenImage = openImage;
        CloseImage = closeImage;
    }

    /// <summary>
    /// Mueve a Pacman a una nueva posicion
    /// </summary>
    /// <param name="newX">Nueva Coordenada X</param>
    /// <param name="newY">Nueva Coordenada Y</param>
    public void Move(double newX, double newY)
    {
        X = newX;
        Y = newY;
        UpdatePosition();
    }

    /// <summary>
    /// Actualiza la posicion de las imagenes del Pacman
    /// </summary>
    public void UpdatePosition()
    {
        Canvas.SetLeft(OpenImage, X);
        Canvas.SetTop(OpenImage, Y);
        Canvas.SetLeft(CloseImage, X);
        Canvas.SetTop(CloseImage, Y);
    }

    /// <summary>
    /// Rota a Pacman al angulo especificado
    /// </summary>
    /// <param name="angle">Angulo de rotacion en grados</param>
    public void Rotate(double angle)
    {
        var transform = new RotateTransform
        {
            Angle = angle,
            CenterX = OpenImage.Bounds.Width / 2,
            CenterY = OpenImage.Bounds.Height / 2
        };
        
        OpenImage.RenderTransform = transform;
        CloseImage.RenderTransform = transform;
    }

    /// <summary>
    /// Obtiene los limites actuales de Pacman
    /// </summary>
    /// <returns>Un rectangulo que representa el area ocupada de Pacman</returns>
    public Rect GetBounds()
    {
        return new Rect(X, Y, OpenImage.Bounds.Width, OpenImage.Bounds.Height);
    }

    /// <summary>
    /// Alterna el estado de la boca de Pacman y actualiza la visibilidad
    /// </summary>
    public void ToggleMouth()
    {
        IsMouthOpen = !IsMouthOpen;
        OpenImage.IsVisible = IsMouthOpen;
        CloseImage.IsVisible = !IsMouthOpen;
    }
}