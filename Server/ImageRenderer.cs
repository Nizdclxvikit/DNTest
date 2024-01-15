
namespace Server;

abstract class ImageRenderer
{
    public abstract byte[] RenderArea(double x, double y, double w, double h, int width, int height);

}