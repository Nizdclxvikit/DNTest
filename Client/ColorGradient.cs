namespace client;

public class ColorGradient
{
    private double[] positions;
    private Color[] colors;

    public ColorGradient(double[] dPositions, Color[] dColors)
    {
        // Positions should be sorted!
        positions = new double[dPositions.Length];
        for (int i=0; i<dPositions.Length; i++)
        {
            positions[i] = dPositions[i] % 1.0;
        }

        colors = new Color[dColors.Length];
        for (int i=0; i<dColors.Length; i++)
        {
            colors[i] = dColors[i];
        }
    }

    public Color SampleGradient(double position)
    {
        position = position % 1.0;

        int index;
        for (index = 0; index < positions.Length; index++)
        {
            if (positions[index] == position) return colors[index];
            if (positions[index] > position) break;
        }

        if (index == positions.Length) return colors[colors.Length-1];

        double fac = (position - positions[index-1]) / (positions[index] - positions[index-1]);

        return Lerp(colors[index-1], colors[index], fac);
    }

    private Color Lerp(Color A, Color B, double fac)
    {
        // fac from 0 to 1;

        return Color.FromArgb(
            (int)((1.0-fac)*A.R + fac * B.R),
            (int)((1.0-fac)*A.G + fac * B.G),
            (int)((1.0-fac)*A.B + fac * B.B));
    }
}