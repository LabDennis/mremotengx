using System.Collections.Generic;
using System.Linq;
using mRemoteNG.Connection;

namespace mRemoteNGTests.TestHelpers
{
	public class ConnectionInfoAllConnectionPropertiesEqualityComparer : IEqualityComparer<ConnectionInfo>
	{
		public bool Equals(ConnectionInfo x, ConnectionInfo y)
		{
			if (x == null && y == null)
				return true;
			if ((x == null) != (y == null))
				return false;
			// A container and a connection serialize different property sets: compare the shared ones.
			var yProperties = y.GetSerializableProperties().GroupBy(p => p.Name).ToDictionary(g => g.Key, g => g.First());
			var different = x.GetSerializableProperties()
				.Where(p => yProperties.ContainsKey(p.Name))
				.Where(p => !ValuesEqual(p.GetValue(x), yProperties[p.Name].GetValue(y)))
				.Select(p => $"{p.Name}: {p.GetValue(x)} != {yProperties[p.Name].GetValue(y)}")
				.ToList();
			if (different.Count > 0)
				NUnit.Framework.TestContext.Out.WriteLine("Different properties: " + string.Join("; ", different));
			return different.Count == 0;
		}

		// Dictionaries such as PluginProperties are copied entry by entry, so compare their contents.
		private static bool ValuesEqual(object a, object b) =>
			a is IDictionary<string, string> da && b is IDictionary<string, string> db
				? da.Count == db.Count && da.All(entry => db.TryGetValue(entry.Key, out var value) && value == entry.Value)
				: Equals(a, b);

		public int GetHashCode(ConnectionInfo connectionInfo)
		{
			var allProperties = connectionInfo.GetSerializableProperties();

			unchecked // Overflow is fine, just wrap
			{
				return allProperties
					.Aggregate(17, 
						(current, prop) => current * 23 + (prop.GetValue(connectionInfo)?.GetHashCode() ?? 0));
			}
		}
	}
}
