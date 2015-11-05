using System;
using System.Reflection;

namespace NMotiveTools
{
    public static class DependenciesDisplayer
    {
        public static void PrintDependencies(Assembly a)
        {
            AssemblyName aName = a.GetName();
            Console.WriteLine("Name={0}, Version={1}", aName.Name, aName.Version);
            //Console.WriteLine("Name={0}, Version={1}, Culture={2}, PublicKey token={3}", aName.Name, aName.Version, aName.CultureInfo.Name, (BitConverter.ToString(aName.GetPublicKeyToken())));

            foreach (AssemblyName an in a.GetReferencedAssemblies())
            {
                Assembly b = Assembly.Load(an);
                AssemblyName bName = b.GetName();
                Console.WriteLine("\tName={0}, Version={1}", bName.Name, bName.Version);
                //Console.WriteLine("\tName={0}, Version={1}, Culture={2}, PublicKey token={3}", bName.Name, bName.Version, bName.CultureInfo.Name, (BitConverter.ToString(bName.GetPublicKeyToken())));
            }
        }
    }
}
