using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection.Emit;

namespace Server
{
    class MandelRenderer : ImageRenderer
    {
        public override byte[] RenderArea(double x, double y, double w, double h, int width, int height)
        {
            return RenderFractal(x, y, w, h, width, height);
        }


        private byte[] RenderFractal(double x, double y, double w, double h, int width, int height)
        {
            const byte MAX_ITER = 52;

            // Format: just raw bytes for the image
            // 0 = inside, 1-255 = outside

            byte[] image = new byte[8 + width*(int)height]; // dims+coords at the start
            
           

            double left = x-w/2;
            double top  = y+h/2;

            double dx = w/width;
            double dy = h/height;

            // Loop every pixel
            for (int i=0; i<width; i++)
            {
                for (int j=0; j<height; j++)
                {
                    Complex c = new Complex(left + i*dx, top - j*dy); // This pixel coordinate
                    Complex z = 0;

                    byte n = 1;

                    while (z.Magnitude <= 2 && n < MAX_ITER)
                    {
                        z = z*z + c;
                        n++;
                    }
                    
                    if (n>=MAX_ITER) n = 0; // Didn't escape

                    image[i + width*j] = n;
                }
            }

            return image;
        }
    }
}
