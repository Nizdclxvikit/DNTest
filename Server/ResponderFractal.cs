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
    class ResponderFractal : Responder
    {
        public ResponderFractal()
        {
        
        }

        public override Response GetResponse(byte[] input)
        {
            
            String str = Encoding.UTF8.GetString(input);
            
            string[] args = str.Split(";");

            if (args.Length == 4)
            {
                double x, y, w, h;
                bool success = true;

                success &= double.TryParse(args[0], out x);
                success &= double.TryParse(args[1], out y);
                success &= double.TryParse(args[2], out w);
                success &= double.TryParse(args[3], out h);
                
                if (success) 
                {
                    Response response = new Response(ResponderStatus.OK);
                    response.data = RenderFractal(x, y, w, h);

                    return response;
                }
                else return new Response(ResponderStatus.ERR_INPUT_FORMAT);
            }
            else
            {
                return new Response(ResponderStatus.ERR_INPUT_FORMAT);
            }

        }

        private byte[] RenderFractal(double x, double y, double w, double h)
        {
            const byte MAX_ITER = 52;

            // Format: width, height, then raw bytes for the image
            // 0 = inside, 1-255 = outside

            ushort width = 200, height = 200; // Image dimensions
            byte[] image = new byte[4 + width*(int)height]; // dims at the start
            image[0] = (byte)(width >> 8);
            image[1] = (byte)width;
            image[2] = (byte)(height >> 8);
            image[3] = (byte)height;

            double left   = x-w/2;
            double right  = x+w/2;
            double top    = y+h/2;
            double bottom = y-h/2;

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

                    image[4 + i + height*j] = n;
                }
            }

            return image;
        }
    }
}
