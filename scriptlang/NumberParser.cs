using System.Linq;

namespace scriptlang
{
	public class NumberParser {

		/*
         * Returns the string with the first character removed if the first character is 
         * a '+' or '-', as long as that is not the only character in the string.
         */
		public static string SanitizeSign(string number)
		{
			if (number.Length > 1 && (number[0] == '-' || number[0] == '+'))
			{
				number = number.Substring(1);
			}
			return number;
		}

		public static bool IsNumeric(string symbol)
		{
			// To support scientific notation, split the number into the base and exponent parts, 
			// which will be seperated by an 'E' character.
			var parts = symbol.Split('e', 'E');

			// All valid numbers will either have one or two parts, the coefficient will always be the first part.
			string coefficient = SanitizeSign(parts[0]);

			// If we have two parts we need to check the exponent.
			if (parts.Length == 2)
			{
				var exponent = SanitizeSign(parts[1]);
				if (exponent.Any(ix => !char.IsDigit(ix)))
					return false;

			}
			else if (parts.Length != 1)
			{

				return false;
			}

			// If we are this far then we just need to check that the coefficient.
			return coefficient.All(ix => char.IsDigit(ix) || ix == '.');
		}
	}
}
