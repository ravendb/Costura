﻿using System;
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

		if (nullCache.ContainsKey(assemblyName))
			return null;

		try
		{
			if (currentlyLoading.ContainsKey(assemblyName))
				return null;

			currentlyLoading.Add(assemblyName, null);

			lock (Locker)
			{
				if (nullCache.ContainsKey(assemblyName))
					return null;

				var requestedAssemblyName = new AssemblyName(assemblyName);

				var assembly = Common.ReadExistingAssembly(requestedAssemblyName);
				if (assembly != null)
				{
					return assembly;
				}

				Common.Log("Loading assembly '{0}' into the AppDomain", requestedAssemblyName);

				assembly = Common.ReadFromEmbeddedResources(assemblyNames, symbolNames, requestedAssemblyName);
				if (assembly == null)
				{
					nullCache.Add(assemblyName, true);

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
			currentlyLoading.Remove(assemblyName);
		}
	}
}