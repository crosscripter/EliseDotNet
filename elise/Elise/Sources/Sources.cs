using System;
using System.IO;
using System.Reflection;

namespace Elise.Sources
{
    public static class Sources
    {
        public static string Load(string resourceName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                using (var reader = new StreamReader(assembly.GetManifestResourceStream(resourceName)))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Resource '{resourceName}' could not be loaded. Error was: {e}");
            }
        }
    }
}
