using Accessibility;

namespace client;

public class ChunkedBitmap
{
    private Bitmap bitmap;
    private Graphics graphics;

    public readonly int width, height;

    public ChunkedBitmap (int _width, int _height)
    {
        width = _width;
        height = _height;
        bitmap = new Bitmap(width, height);
        
        graphics = Graphics.FromImage(bitmap);
    }

    public ChunkedBitmap (Bitmap _bitmap)
    {
        width = _bitmap.Width;
        height = _bitmap.Height;
        bitmap = _bitmap;
        
        graphics = Graphics.FromImage(bitmap);
    }

    public Bitmap GetBitmap()
    {
        return (Bitmap)bitmap.Clone();
    }

    public void PutChunk(int x, int y, Image chunk)
    {
        graphics.DrawImage(chunk, x, y);
    }
}