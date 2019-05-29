using System.Collections.Generic;

namespace scriptlang
{
	public class DeepMemberSetter
	{
		public static object Set(object root, string[] parts, object value)
		{
			var current = root;
			var parent = current;
			if (parts.Length == 0)
			{
				throw new RuntimeException($"This shouldn't get hit");
			}

			for (var p = 1; p < parts.Length; p++)
			{
				if (current is IDictionary<string, object> dict)
				{
					if (p == parts.Length - 1)
					{
						dict[parts[p]] = value;
						return value;
					}
					else
					{
						if (!dict.ContainsKey(parts[p]))
							throw new RuntimeException($"Member does not exist: {parts[p]} ({string.Join(".", parts)})");

						current = dict[parts[p]];
					}
				}
				else if (current == null)
				{
					throw new RuntimeException($"Member does not exist: {parts[p]} ({string.Join(".", parts)})");
				}
				else
				{

					throw new RuntimeException($"Unable to parse '{parts[0]}' symbol");
				}
			}

			return value;
		}
	}
}
