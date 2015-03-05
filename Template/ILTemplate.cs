using System;
using System.Collections.Generic;
using System.Reflection;

static class ILTemplate
{
	static readonly Dictionary<string, bool> nullCache = new Dictionary<string, bool>();

	static readonly Dictionary<string, string> assemblyNames = new Dictionary<string, string>();

	static readonly Dictionary<string, string> symbolNames = new Dictionary<string, string>();

	[ThreadStatic]
	static Dictionary<string, object> currentlyLoading;

	private static readonly object Locker = new object();

	public static void Attach()
	{
		var currentDomain = AppDomain.CurrentDomain;
		currentDomain.AssemblyResolve += (s, e) => ResolveAssembly(e.Name);
	}

	public static Assembly ResolveAssembly(string assemblyName)
	{
		if (currentlyLoading == null)
			currentlyLoading = new Dictionary<string, object>();

		var requestedAssemblyName = new AssemblyName(assemblyName);
		string assemblyKey;

		try
		{
			assemblyKey = requestedAssemblyName.Name + ";" + requestedAssemblyName.Version + ";";
		}
		catch (Exception)
		{
			assemblyKey = assemblyName;
		}

		assemblyKey = assemblyKey.ToLower();

		if (nullCache.ContainsKey(assemblyKey))
			return null;

		var added = false;

		try
		{
			if (currentlyLoading.ContainsKey(assemblyKey))
				return null;

			lock (Locker)
			{
				if (currentlyLoading.ContainsKey(assemblyKey))
					return null;

				currentlyLoading.Add(assemblyKey, null);
				added = true;

				if (nullCache.ContainsKey(assemblyKey))
					return null;

				var assembly = Common.ReadExistingAssembly(requestedAssemblyName);
				if (assembly != null)
				{
					return assembly;
				}

				Common.Log("Loading assembly '{0}' into the AppDomain", requestedAssemblyName);

				assembly = Common.ReadFromEmbeddedResources(assemblyNames, symbolNames, requestedAssemblyName);
				if (assembly == null)
				{
					nullCache.Add(assemblyKey, true);

					// Handles retargeted assemblies like PCL
					if (requestedAssemblyName.Flags == AssemblyNameFlags.Retargetable)
					{
						assembly = Assembly.Load(requestedAssemblyName);
					}
				}
				return assembly;
			}
		}
		finally
		{
			if (added)
				currentlyLoading.Remove(assemblyKey);
		}
	}
}