

using System;
using System.Device.Location;

namespace transport
{
	public class Pedido
	{
		public string numnota { get; private set; }
		public string codcli { get; private set; }
		public string numped { get; private set; }		
		public string cliente { get; private set; }		
		public string uf { get; private set; }
		public string cidade { get; private set; }
		public string bairro { get; private set; }
		public string endereco { get; private set; }
		public string complemento { get; private set; }
		public string cep { get; private set; }
		public string obs1 { get; private set; }
		public string obs2 { get; private set; }
		public string obs3 { get; private set; }
		public string rca { get; private set; }
		public double X { get; set; }
		public double Y { get; set; }
		public int tipo { get; private set; }//1 nota de entrega,2 nota de pendencia
		public int index { get; set; }		

		public string cor { get; set; }//0 using original adress, 1 using cpf in obs, 2 there is obs

		public static DistDur[,] distanceMatrix;


		public Pedido(double x, double y)
		{
			X = x;
			Y = y;
		}


		public Pedido(double x, double y, int index)
		{
			X = x;
			Y = y;
			this.index = index;
		}


		public Pedido(string numnota, string codcli, string numped, string cliente, string uf, string cidade, string bairro, string endereco, string cep, string obs1, string obs2, string obs3, string rca, double x, double y, int tipo, int originalIndex,string complemento,string cor)
		{
			this.numnota = numnota;
			this.codcli = codcli;
			this.numped = numped;			
			this.cliente = cliente;			
			this.uf = uf;
			this.cidade = cidade;
			this.bairro = bairro;
			this.endereco = endereco;
			this.cep = cep;
			this.obs1 = obs1;
			this.obs2 = obs2;
			this.obs3 = obs3;
			this.rca = rca;
			this.tipo = tipo;
			this.index = originalIndex;
			this.complemento = complemento;			
			this.cor = cor;
			X = x;
			Y = y;
		}
		


		


		public double GetDistance(Pedido other)
		{
			if (Methods.useDistanceMatrix == false)
			{
				var diffX = X - other.X;
				var diffY = Y - other.Y;
				return Math.Sqrt(Math.Pow(diffX, 2) + Math.Pow(diffY, 2)) / 1000;
			}
			else
			{
				//Console.WriteLine($"pedido: {numped} , index: {originalIndex}, other pedido {other.numped} , other index: {other.originalIndex}, distance {distanceMatrix[originalIndex, other.originalIndex].distance}");
				return distanceMatrix[index, other.index].distance;
			}
		}



		public double GetDistanceByKM(Pedido other)
		{
			if (Methods.useDistanceMatrix == false)
			{
				var sGeo = new GeoCoordinate(X, Y);
				var eGeo = new GeoCoordinate(other.X, other.Y);
				return sGeo.GetDistanceTo(eGeo) / 1000;
			}
			else
			{
				return Math.Round(distanceMatrix[index, other.index].distance, 2);
			}
		}

		public static double GetTotalDistance(Pedido startLocation, Pedido[] locations)
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


		public static double GetTotalDistanceKM(Pedido startLocation, Pedido[] locations)
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


		public static void SwapLocations(Pedido[] locations, int index1, int index2)
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
		public static void MoveLocations(Pedido[] locations, int fromIndex, int toIndex)
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

		public static void ReverseRange(Pedido[] locations, int startIndex, int endIndex)
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
				Pedido temp = locations[endIndex];
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
