using System.Numerics;

namespace OverlayDisplayWhiteboard.Utility;

public static class PointUtility
{
	public static Vector2[] Smooth(Vector2[] data, int count,  int windowSize)
	{
		float[] kernel = Hanning(windowSize);
		return Convolve(data, kernel, count);
	}

	public static Vector2[] Convolve(Vector2[] data, float[] kernel, int count)
	{
		int dataSize = data.Length > count ? count : data.Length;
		int kernelSize = kernel.Length;
		int halfKernel = kernelSize / 2;
		Vector2[] smoothedData = new Vector2[dataSize];

		for (int i = 0; i < dataSize; i++)
		{
			float sumX = 0;
			float sumY = 0;
			float weightSumY = 0;
			float weightSumX = 0;

			for (int j = 0; j < kernelSize; j++)
			{
				int index = i + j - halfKernel;
				if (index >= 0 && index < dataSize)
				{
					sumX += data[index].X * kernel[j];
					weightSumX += kernel[j];

					sumY += data[index].Y * kernel[j];
					weightSumY += kernel[j];
					
				}
			}

			smoothedData[i].X = weightSumX > 0 ? sumX / weightSumX : 0;
			smoothedData[i].Y = weightSumY > 0 ? sumY / weightSumY : 0;
		}

		return smoothedData;
	}

	public static float[] Hanning(int windowSize)
	{
		return Enumerable
			.Range(0, windowSize)
			.Select(i => 0.5f * (1 - MathF.Cos((2 * MathF.PI * i) / (windowSize - 1f))))
			.ToArray();
	}
}