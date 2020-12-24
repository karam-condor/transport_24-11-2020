using System;
using System.Data.Entity.Core.Objects;
using System.Device.Location;

namespace transport
{
	public sealed class LocationGA
	{

		public LocationGA(double x, double y)
		{
			X = x;
			Y = y;
		}


		public LocationGA(double x, double y, int index)
		{
			X = x;
			Y = y;
			this.index = index;
		}


		// We could add other properties, like the name, the description
		// or anything similar that we consider useful.

		
		public double X { get; set; }
		public double Y { get; set; }
		public int index { get; set; }
		public int[] originalIndices { get; set; }		

		public static DistDur[,] distanceMatrix;


		public LocationGA(double X,double Y,int index,int[] originalInd)
        {
			this.X = X;
			this.Y = Y;
			this.index = index;
			this.originalIndices = originalInd;
        }


		public double GetDistance(LocationGA other)
		{
			if(Methods.useDistanceMatrix == false)
            {
				var diffX = X - other.X;
				var diffY = Y - other.Y;
				return Math.Sqrt(Math.Pow(diffX, 2) + Math.Pow(diffY, 2))/1000;
            }
            else
            {
				//Console.WriteLine($"pedido: {numped} , index: {originalIndex}, other pedido {other.numped} , other index: {other.originalIndex}, distance {distanceMatrix[originalIndex, other.originalIndex].distance}");
				return distanceMatrix[index, other.index].distance;
            }			
		}



		public double GetDistanceByKM(LocationGA other)
		{
			if(Methods.useDistanceMatrix == false)
            {
				var sGeo = new GeoCoordinate(X, Y);
				var eGeo = new GeoCoordinate(other.X, other.Y);
				return sGeo.GetDistanceTo(eGeo)/1000;
            }
            else
            {
				return Math.Round(distanceMatrix[index, other.index].distance,2);
			}
		}

		public static double GetTotalDistance(LocationGA startLocation, LocationGA[] locations)
		{
			if (startLocation == null)
				throw new ArgumentNullException("startLocation");

			if (locations == null)
				throw new ArgumentNullException("locations");

			if (locations.Length == 0)
				throw new ArgumentException("The locations array must have at least one element.", "locations");

			foreach (var location in locations)
				if (location == null)
					throw new ArgumentException("The locations array can't contain null values.");

			double result = startLocation.GetDistance(locations[0]);
			int countLess1 = locations.Length - 1;
			for (int i = 0; i < countLess1; i++)
			{
				var actual = locations[i];
				var next = locations[i + 1];

				var distance = actual.GetDistance(next);
				result += distance;
			}

			result += locations[locations.Length - 1].GetDistance(startLocation);

			return result;
		}


		public static double GetTotalDistanceKM(LocationGA startLocation, LocationGA[] locations)
		{
			if (startLocation == null)
				throw new ArgumentNullException("startLocation");

			if (locations == null)
				throw new ArgumentNullException("locations");

			if (locations.Length == 0)
				throw new ArgumentException("The locations array must have at least one element.", "locations");

			foreach (var location in locations)
				if (location == null)
					throw new ArgumentException("The locations array can't contain null values.");

			double result = startLocation.GetDistanceByKM(locations[0]);
			int countLess1 = locations.Length - 1;
			for (int i = 0; i < countLess1; i++)
			{
				var actual = locations[i];
				var next = locations[i + 1];

				var distance = actual.GetDistanceByKM(next);
				result += distance;
			}

			result += locations[locations.Length - 1].GetDistanceByKM(startLocation);

			return result;
		}


		public static void SwapLocations(LocationGA[] locations, int index1, int index2)
		{
			if (locations == null)
				throw new ArgumentNullException("locations");

			if (index1 < 0 || index1 >= locations.Length)
				throw new ArgumentOutOfRangeException("index1");

			if (index2 < 0 || index2 >= locations.Length)
				throw new ArgumentOutOfRangeException("index2");

			var location1 = locations[index1];
			var location2 = locations[index2];
			locations[index1] = location2;
			locations[index2] = location1;
		}

		// Moves an item in the list. That is, if we go from position 1 to 5, the items
		// that were previously 2, 3, 4 and 5 become 1, 2, 3 and 4.
		public static void MoveLocations(LocationGA[] locations, int fromIndex, int toIndex)
		{
			if (locations == null)
				throw new ArgumentNullException("locations");

			if (fromIndex < 0 || fromIndex >= locations.Length)
				throw new ArgumentOutOfRangeException("fromIndex");

			if (toIndex < 0 || toIndex >= locations.Length)
				throw new ArgumentOutOfRangeException("toIndex");

			var temp = locations[fromIndex];

			if (fromIndex < toIndex)
			{
				for (int i = fromIndex + 1; i <= toIndex; i++)
					locations[i - 1] = locations[i];
			}
			else
			{
				for (int i = fromIndex; i > toIndex; i--)
					locations[i] = locations[i - 1];
			}

			locations[toIndex] = temp;
		}

		public static void ReverseRange(LocationGA[] locations, int startIndex, int endIndex)
		{
			if (locations == null)
				throw new ArgumentNullException("locations");

			if (startIndex < 0 || startIndex >= locations.Length)
				throw new ArgumentOutOfRangeException("startIndex");

			if (endIndex < 0 || endIndex >= locations.Length)
				throw new ArgumentOutOfRangeException("endIndex");

			if (endIndex < startIndex)
			{
				int temp = endIndex;
				endIndex = startIndex;
				startIndex = temp;
			}

			while (startIndex < endIndex)
			{
				LocationGA temp = locations[endIndex];
				locations[endIndex] = locations[startIndex];
				locations[startIndex] = temp;
				startIndex++;
				endIndex--;
			}
		}

		public override string ToString()
		{
			return X + ", " + Y;
		}
	}
}
